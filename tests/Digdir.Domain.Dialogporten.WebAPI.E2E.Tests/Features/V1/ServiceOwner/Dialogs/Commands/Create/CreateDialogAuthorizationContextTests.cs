using System.Net;
using System.Net.Http.Headers;
using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Extensions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using Constants = Digdir.Domain.Dialogporten.Application.Common.Authorization.Constants;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.ServiceOwner.Dialogs.Commands.Create;

/// <summary>
/// Write-side guards on authorization contexts, against the real resource registry and the real PDP.
/// </summary>
[Collection(nameof(WebApiTestCollectionFixture))]
public class CreateDialogAuthorizationContextTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    /// <summary>
    /// A service owner may only reference resources it owns in an authorization context. The typed client cannot
    /// express this as a compile error, so the check must hold at the API boundary — otherwise a service owner
    /// could have Dialogporten evaluate access against somebody else's resource.
    /// </summary>
    [E2EFact]
    public async Task Should_Reject_Context_Referring_A_Resource_The_Service_Owner_Does_Not_Own()
    {
        // Arrange
        var dialog = DialogTestData.CreateSimpleDialog(d =>
            d.GuiActions =
            [
                new V1ServiceOwnerDialogsCommandsCreate_GuiAction
                {
                    Url = new Uri("https://digdir.apps.tt02.altinn.no/unowned-resource"),
                    Priority = DialogsEntitiesActions_DialogGuiActionPriority.Primary,
                    Title = [DialogTestData.CreateLocalization("Ikke eid ressurs")],
                    AuthorizationContext = new V1CommonAuthorizationContexts_AuthorizationContext
                    {
                        Action = Constants.ReadAction,
                        ServiceResource = "urn:altinn:resource:notavailable",
                        IncludeDialogParty = true,
                        UnauthorizedPresentation = DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation.Disabled
                    }
                }
            ]);

        // Act
        var response = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsCommandsCreateDialog(
            dialog, TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Attachments and navigational actions are only ever fetched, never acted upon, so their XACML action is
    /// fixed to "read". The contract enforces this structurally — the child context shape has no action field —
    /// but a hand-rolled client can still put one on the wire. This asserts it cannot take effect, by reading
    /// back the action the PDP was actually asked about from the issued context token.
    /// </summary>
    [E2EFact]
    public async Task Action_On_A_Child_Context_Should_Never_Take_Effect()
    {
        // Arrange: raw JSON, because the generated client deliberately has no action on child contexts.
        var dialogId = Guid.CreateVersion7();
        var attachmentId = Guid.CreateVersion7();
        var payload = $$"""
            {
              "id": "{{dialogId}}",
              "serviceResource": "{{E2EConstants.DefaultServiceResource}}",
              "party": "{{E2EConstants.DefaultParty}}",
              "status": "NotApplicable",
              "serviceOwnerContext": { "serviceOwnerLabels": [ { "value": "{{E2EConstants.EphemeralDialogUrn}}" } ] },
              "content": {
                "title": { "mediaType": "text/plain", "value": [ { "languageCode": "nb", "value": "Tittel" } ] },
                "summary": { "mediaType": "text/plain", "value": [ { "languageCode": "nb", "value": "Sammendrag" } ] }
              },
              "attachments": [
                {
                  "id": "{{attachmentId}}",
                  "displayName": [ { "languageCode": "nb", "value": "Vedlegg" } ],
                  "urls": [ { "url": "https://digdir.apps.tt02.altinn.no/attachment", "consumerType": "Gui" } ],
                  "authorizationContext": {
                    "action": "write",
                    "serviceResource": "{{E2EConstants.AvailableExternalResource}}",
                    "includeDialogParty": true,
                    "unauthorizedPresentation": "Disabled"
                  }
                }
              ]
            }
            """;

        using var serviceOwnerClient = await CreateRawServiceOwnerClient();

        // Act
        using var createResponse = await serviceOwnerClient.PostAsync(
            "api/v1/serviceowner/dialogs",
            new StringContent(payload, System.Text.Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);

        // Assert: the unknown member is ignored rather than rejected, so the dialog is created…
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created,
            await createResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        var dialogResponse = await Fixture.EndUserApi.GetDialog(dialogId);
        dialogResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        var attachment = (dialogResponse.Content ?? throw new InvalidOperationException("Dialog content was null."))
            .Attachments!.Single();

        attachment.IsAuthorized.Should().BeTrue(
            "the attachment is authorized with read, which the end user holds — not with the write it asked for");
        attachment.ContextToken.Should().NotBeNullOrEmpty();

        // …and the action the PDP was actually asked about is read, not write.
        var token = await DialogportenTokenVerifier.VerifyAsync(
            Fixture.WebApiUri, attachment.ContextToken!, TestContext.Current.CancellationToken);

        token.GetString(DialogTokenClaimTypes.Actions).Should().Be(Constants.ReadAction,
            "an action supplied on a child context must not be able to escalate the evaluated action");
    }

    /// <summary>
    /// The two mechanisms are mutually exclusive per entity, so a migrating service owner cannot end up with an
    /// entity whose authorization is described twice and ambiguously.
    /// </summary>
    [E2EFact]
    public async Task Should_Reject_An_Entity_Combining_AuthorizationAttribute_And_AuthorizationContext()
    {
        // Arrange
#pragma warning disable CS0618 // Deliberately exercising the deprecated field
        var dialog = DialogTestData.CreateSimpleDialog(d =>
            d.Transmissions =
            [
                new V1ServiceOwnerDialogsCommandsCreate_Transmission
                {
                    Type = DialogsEntitiesTransmissions_DialogTransmissionType.Information,
                    Sender = new V1ServiceOwnerCommonActors_Actor { ActorType = Actors_ActorType.ServiceOwner },
                    AuthorizationAttribute = E2EConstants.UnavailableSubresource,
                    AuthorizationContext = new V1CommonAuthorizationContexts_AuthorizationContext
                    {
                        ServiceResource = E2EConstants.AvailableExternalResource,
                        IncludeDialogParty = true,
                        UnauthorizedPresentation = DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation.Disabled
                    },
                    Content = new V1ServiceOwnerDialogsCommandsCreate_TransmissionContent
                    {
                        Title = DialogTestData.CreateContentValue("Tittel", "nb"),
                        Summary = DialogTestData.CreateContentValue("Sammendrag", "nb")
                    }
                }
            ]);
#pragma warning restore CS0618

        // Act
        var response = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsCommandsCreateDialog(
            dialog, TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// An HttpClient carrying a real service owner token, for the few cases that must put JSON on the wire that
    /// the generated client cannot express.
    /// </summary>
    private async Task<HttpClient> CreateRawServiceOwnerClient()
    {
        var token = await TestTokenGenerator.GenerateTokenAsync(
            TokenKind.ServiceOwner, Fixture.Settings, TestContext.Current.CancellationToken);

        var httpClient = new HttpClient { BaseAddress = Fixture.WebApiUri };
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return httpClient;
    }
}
