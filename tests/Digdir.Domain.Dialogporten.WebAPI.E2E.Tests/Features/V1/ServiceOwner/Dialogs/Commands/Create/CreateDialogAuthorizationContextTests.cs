using System.Net;
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
    /// The context action is optional everywhere and defaults to "read", but an explicit action must take
    /// effect on child contexts too — both when the policy grants it (the dialog token lists the entity)
    /// and when no policy rule matches it (denied, rather than silently evaluated as "read").
    /// </summary>
    [E2EFact]
    public async Task Explicit_Action_On_A_Child_Context_Should_Take_Effect()
    {
        // Arrange
        var grantedAttachmentId = Guid.CreateVersion7();
        var deniedAttachmentId = Guid.CreateVersion7();
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(dialog =>
        {
            dialog.Attachments =
            [
                new V1ServiceOwnerDialogsCommandsCreate_Attachment
                {
                    Id = grantedAttachmentId,
                    DisplayName = [DialogTestData.CreateLocalization("Vedlegg med write")],
                    Urls = [new V1ServiceOwnerDialogsCommandsCreate_AttachmentUrl
                    {
                        Url = new Uri("https://digdir.apps.tt02.altinn.no/attachment-write"),
                        ConsumerType = Attachments_AttachmentUrlConsumerType.Gui
                    }],
                    AuthorizationContext = new V1CommonAuthorizationContexts_AuthorizationContext
                    {
                        Action = "write",
                        ServiceResource = E2EConstants.AvailableExternalResource,
                        IncludeDialogParty = true,
                        UnauthorizedPresentation = DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation.Disabled
                    }
                },
                new V1ServiceOwnerDialogsCommandsCreate_Attachment
                {
                    Id = deniedAttachmentId,
                    DisplayName = [DialogTestData.CreateLocalization("Vedlegg med ukjent action")],
                    Urls = [new V1ServiceOwnerDialogsCommandsCreate_AttachmentUrl
                    {
                        Url = new Uri("https://digdir.apps.tt02.altinn.no/attachment-unknown-action"),
                        ConsumerType = Attachments_AttachmentUrlConsumerType.Gui
                    }],
                    AuthorizationContext = new V1CommonAuthorizationContexts_AuthorizationContext
                    {
                        // No policy defines this action, so the check must deny — proving the explicit
                        // action reached the PDP instead of being replaced by the "read" default.
                        Action = "e2eactionwithoutpolicyrule",
                        ServiceResource = E2EConstants.AvailableExternalResource,
                        IncludeDialogParty = true,
                        UnauthorizedPresentation = DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation.Disabled
                    }
                }
            ];
        });

        // Act
        var dialogResponse = await Fixture.EndUserApi.GetDialog(dialogId);

        // Assert
        dialogResponse.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = dialogResponse.Content ?? throw new InvalidOperationException("Dialog content was null.");

        var granted = content.Attachments.Single(a => a.Id == grantedAttachmentId);
        granted.IsAuthorized.Should().BeTrue("the end user holds write on the external resource");

        var denied = content.Attachments.Single(a => a.Id == deniedAttachmentId);
        denied.IsAuthorized.Should().BeFalse(
            "an action no policy rule matches must deny — the explicit action must not fall back to read");

        var token = await DialogportenTokenVerifier.VerifyAsync(
            Fixture.WebApiUri,
            content.DialogToken ?? throw new InvalidOperationException("Dialog token was null."),
            TestContext.Current.CancellationToken);
        token.GetStringList(DialogTokenClaimTypes.AuthorizedEntities).Should().Equal([grantedAttachmentId.ToString()],
            "only the entity whose explicit action the PDP permitted is listed");
    }

    /// <summary>
    /// A service owner supplied token reference replaces the entity id in the dialog token, so the service owner
    /// can recognize the grant by a reference of its own choosing, and it is echoed back on the read surface.
    /// </summary>
    [E2EFact]
    public async Task TokenRef_Should_Replace_The_Entity_Id_In_The_Dialog_Token()
    {
        // Arrange
        var attachmentId = Guid.CreateVersion7();
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(dialog =>
        {
            dialog.Attachments =
            [
                new V1ServiceOwnerDialogsCommandsCreate_Attachment
                {
                    Id = attachmentId,
                    DisplayName = [DialogTestData.CreateLocalization("Vedlegg med tokenRef")],
                    Urls = [new V1ServiceOwnerDialogsCommandsCreate_AttachmentUrl
                    {
                        Url = new Uri("https://digdir.apps.tt02.altinn.no/attachment-token-ref"),
                        ConsumerType = Attachments_AttachmentUrlConsumerType.Gui
                    }],
                    AuthorizationContext = new V1CommonAuthorizationContexts_AuthorizationContext
                    {
                        ServiceResource = E2EConstants.AvailableExternalResource,
                        IncludeDialogParty = true,
                        TokenRef = "e2e-token-ref",
                        UnauthorizedPresentation = DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation.Disabled
                    }
                }
            ];
        });

        // Act
        var serviceOwnerResponse = await Fixture.ServiceownerApi.GetDialog(dialogId);
        var endUserResponse = await Fixture.EndUserApi.GetDialog(dialogId);

        // Assert
        serviceOwnerResponse.ShouldHaveStatusCode(HttpStatusCode.OK);
        serviceOwnerResponse.Content.Should().NotBeNull();
        serviceOwnerResponse.Content.Attachments.Single().AuthorizationContext!.TokenRef.Should().Be("e2e-token-ref");

        endUserResponse.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = endUserResponse.Content ?? throw new InvalidOperationException("Dialog content was null.");
        content.Attachments.Single(a => a.Id == attachmentId).IsAuthorized.Should().BeTrue();

        var token = await DialogportenTokenVerifier.VerifyAsync(
            Fixture.WebApiUri,
            content.DialogToken ?? throw new InvalidOperationException("Dialog token was null."),
            TestContext.Current.CancellationToken);
        token.GetStringList(DialogTokenClaimTypes.AuthorizedEntities).Should().Equal(["e2e-token-ref"],
            "the token reference stands in for the attachment id");
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

}
