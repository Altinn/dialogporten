using System.Net;
using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Extensions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using Constants = Digdir.Domain.Dialogporten.Application.Common.Authorization.Constants;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.Dialogs.Queries.Get;

/// <summary>
/// The headline capability of authorization contexts: a grant obtained via a party other than the dialog's own
/// party. The default end user represents <see cref="E2EConstants.DefaultEndUserOrgUrn"/> but the dialogs below
/// are owned by the person party, so any access granted here can only have come from the context's party list.
/// This is what the dialog token cannot express, and therefore what context tokens exist for.
/// </summary>
[Collection(nameof(WebApiTestCollectionFixture))]
public class GetDialogMultiPartyAuthorizationContextTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    /// <summary>
    /// Baseline. Everything below asserts "authorized via the org party"; if the end user did not actually
    /// represent that org, those assertions would fail for a reason that has nothing to do with authorization
    /// contexts. This test isolates that premise so a delegation change in the test environment is diagnosable.
    /// </summary>
    [E2EFact(SkipOnEnvironments = ["yt01"])]
    public async Task Default_End_User_Should_Represent_The_Org_Party_Used_By_These_Tests()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(
            dialog => dialog.Party = E2EConstants.DefaultEndUserOrgUrn);

        // Act
        var response = await Fixture.EndUserApi.GetDialog(dialogId);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
    }

    [E2EFact(SkipOnEnvironments = ["yt01"])]
    public async Task Should_Grant_Access_Via_A_Party_Other_Than_The_Dialog_Party()
    {
        // Arrange: the dialog party is the person, and neither context includes it. The only way the granted
        // attachment can be authorized is through the org party in its own party list.
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(dialog =>
        {
            dialog.Party = E2EConstants.DefaultParty;
            dialog.Attachments =
            [
                CreateAttachment("granted-via-org-party", [E2EConstants.DefaultEndUserOrgUrn]),
                CreateAttachment("denied-unrepresented-party", [E2EConstants.UnrepresentedOrgUrn])
            ];
        });

        // Act
        var response = await Fixture.EndUserApi.GetDialog(dialogId);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = response.Content ?? throw new InvalidOperationException("Dialog content was null.");

        var granted = content.Attachments!.Single(a => a.Name == "granted-via-org-party");
        granted.IsAuthorized.Should().BeTrue(
            "the org party in the authorization context grants access even though it is not the dialog party");
        granted.Urls.Should().NotBeEmpty()
            .And.AllSatisfy(url => url.Url.ToString().Should().NotBe(Constants.UnauthorizedUri.ToString()));

        var denied = content.Attachments!.Single(a => a.Name == "denied-unrepresented-party");
        denied.IsAuthorized.Should().BeFalse(
            "the end user does not represent that party, so no grant can be obtained through it");
        denied.Urls.Should().NotBeEmpty()
            .And.AllSatisfy(url => url.Url.ToString().Should().Be(Constants.UnauthorizedUri.ToString()));
    }

    /// <summary>
    /// A context's party list is a disjunction: one permitted party is enough. Asserted here with the
    /// unrepresented party listed first, so a bug that only inspects the first party would fail.
    /// </summary>
    [E2EFact(SkipOnEnvironments = ["yt01"])]
    public async Task Should_Grant_Access_When_Any_Single_Context_Party_Is_Permitted()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(dialog =>
        {
            dialog.Party = E2EConstants.DefaultParty;
            dialog.Attachments =
            [
                CreateAttachment(
                    "granted-via-second-party",
                    [E2EConstants.UnrepresentedOrgUrn, E2EConstants.DefaultEndUserOrgUrn])
            ];
        });

        // Act
        var response = await Fixture.EndUserApi.GetDialog(dialogId);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = response.Content ?? throw new InvalidOperationException("Dialog content was null.");

        content.Attachments!.Single().IsAuthorized.Should().BeTrue(
            "a context is authorized when at least one of its parties is permitted");
    }

    /// <summary>
    /// The security-critical assertion for context tokens: the permitted-parties claim must name only the party
    /// the PDP actually permitted, and must never be inflated to the dialog party. Verified the way a receiving
    /// service must verify it — Ed25519 signature against the published JWKS, then the claims.
    /// </summary>
    [E2EFact(SkipOnEnvironments = ["yt01"])]
    public async Task Should_Issue_Verifiable_Context_Token_Asserting_Only_The_Permitted_Party()
    {
        // Arrange
        var attachmentId = Guid.CreateVersion7();
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(dialog =>
        {
            dialog.Party = E2EConstants.DefaultParty;
            dialog.Attachments =
            [
                CreateAttachment("granted-via-org-party", [E2EConstants.DefaultEndUserOrgUrn],
                    modify: attachment => attachment.Id = attachmentId)
            ];
        });

        // Act
        var response = await Fixture.EndUserApi.GetDialog(dialogId);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = response.Content ?? throw new InvalidOperationException("Dialog content was null.");

        var attachment = content.Attachments!.Single();
        attachment.IsAuthorized.Should().BeTrue();
        attachment.ContextToken.Should().NotBeNullOrEmpty(
            "an authorized context-carrying entity must be issued a context token");

        var token = await DialogportenTokenVerifier.VerifyAsync(
            Fixture.WebApiUri,
            attachment.ContextToken!,
            TestContext.Current.CancellationToken);

        token.TokenType.Should().Be(DialogTokenTypes.DialogContextToken,
            "the typ header is what lets a receiving service tell the two token types apart");

        // Bound to this entity, action and dialog, so it cannot be replayed against anything else.
        token.GetString(DialogTokenClaimTypes.EntityId).Should().Be(attachmentId.ToString());
        token.GetString(DialogTokenClaimTypes.EntityType).Should().Be(DialogContextTokenEntityTypes.Attachment);
        token.GetString(DialogTokenClaimTypes.Actions).Should().Be(Constants.ReadAction,
            "attachments are always evaluated with the read action");
        token.GetString(DialogTokenClaimTypes.DialogId).Should().Be(dialogId.ToString());

        // The context does not override the resource, so no effective-resource claim is emitted.
        token.HasClaim(DialogTokenClaimTypes.EffectiveResource).Should().BeFalse(
            "the effective resource claim is omitted when the grant applies to the dialog's own resource");

        // The crux: pp is the PDP's answer, p is the dialog party. They must not be conflated.
        token.GetStringList(DialogTokenClaimTypes.PermittedParties)
            .Should().Equal([E2EConstants.DefaultEndUserOrgUrn],
                "the permitted-parties claim must name only the party the PDP actually permitted");
        token.GetString(DialogTokenClaimTypes.DialogParty).Should().Be(E2EConstants.DefaultParty,
            "the dialog party claim still describes the dialog, not the grant");
    }

    /// <summary>
    /// The dialog token is frozen at legacy semantics. A dialog carrying both a legacy gui action and a
    /// context-based one must encode only the legacy grant in the dialog token's action claim; the context
    /// grant lives exclusively in that entity's context token.
    /// </summary>
    [E2EFact(SkipOnEnvironments = ["yt01"])]
    public async Task Dialog_Token_Should_Not_Encode_Context_Derived_Grants()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(dialog =>
        {
            dialog.Party = E2EConstants.DefaultParty;
            dialog.GuiActions =
            [
                // Legacy: no context, authorized against the dialog's own resource for the dialog party.
                new V1ServiceOwnerDialogsCommandsCreate_GuiAction
                {
                    Action = Constants.ReadAction,
                    Url = new Uri("https://digdir.apps.tt02.altinn.no/legacy-gui-action"),
                    Priority = DialogsEntitiesActions_DialogGuiActionPriority.Primary,
                    Title = [DialogTestData.CreateLocalization("Legacy")]
                },
                // Context-based, granted via the org party only.
                new V1ServiceOwnerDialogsCommandsCreate_GuiAction
                {
                    Url = new Uri("https://digdir.apps.tt02.altinn.no/context-gui-action"),
                    Priority = DialogsEntitiesActions_DialogGuiActionPriority.Secondary,
                    Title = [DialogTestData.CreateLocalization("Context")],
                    AuthorizationContext = new V1CommonAuthorizationContexts_AuthorizationContext
                    {
                        Action = Constants.ReadAction,
                        Parties = [E2EConstants.DefaultEndUserOrgUrn],
                        IncludeDialogParty = false,
                        UnauthorizedPresentation = DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation.Disabled
                    }
                }
            ];
        });

        // Act
        var response = await Fixture.EndUserApi.GetDialog(dialogId);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = response.Content ?? throw new InvalidOperationException("Dialog content was null.");

        var legacyAction = content.GuiActions!.Single(a => a.Url.ToString().EndsWith("legacy-gui-action", StringComparison.Ordinal));
        var contextAction = content.GuiActions!.Single(a => a.Url.ToString().EndsWith("context-gui-action", StringComparison.Ordinal));

        legacyAction.IsAuthorized.Should().BeTrue();
        legacyAction.ContextToken.Should().BeNull("an entity without an authorization context gets no context token");

        contextAction.IsAuthorized.Should().BeTrue();
        contextAction.ContextToken.Should().NotBeNullOrEmpty();

        var dialogToken = await DialogportenTokenVerifier.VerifyAsync(
            Fixture.WebApiUri,
            content.DialogToken ?? throw new InvalidOperationException("Dialog token was null."),
            TestContext.Current.CancellationToken);

        dialogToken.TokenType.Should().Be(DialogTokenTypes.DialogToken);
        dialogToken.HasClaim(DialogTokenClaimTypes.EntityId).Should().BeFalse();
        dialogToken.HasClaim(DialogTokenClaimTypes.PermittedParties).Should().BeFalse();

        // The legacy read grant is present exactly once; nothing was contributed by the context entity. Both
        // actions happen to be "read", so the claim would be indistinguishable if contexts were folded in —
        // hence the assertion is on the entry list, not on containment.
        var dialogTokenActions = dialogToken.GetString(DialogTokenClaimTypes.Actions).Split(';');
        dialogTokenActions.Should().Equal([Constants.ReadAction],
            "the dialog token's action claim carries legacy grants only");
    }

    private static V1ServiceOwnerDialogsCommandsCreate_Attachment CreateAttachment(
        string name,
        string[] parties,
        Action<V1ServiceOwnerDialogsCommandsCreate_Attachment>? modify = null)
    {
        var attachment = new V1ServiceOwnerDialogsCommandsCreate_Attachment
        {
            Name = name,
            DisplayName = [DialogTestData.CreateLocalization(name)],
            Urls =
            [
                new V1ServiceOwnerDialogsCommandsCreate_AttachmentUrl
                {
                    Url = new Uri($"https://digdir.apps.tt02.altinn.no/some-attachment/{name}"),
                    ConsumerType = Attachments_AttachmentUrlConsumerType.Gui
                }
            ],
            AuthorizationContext = new V1CommonAuthorizationContexts_ChildAuthorizationContext
            {
                // No ServiceResource: the effective resource is the dialog's own, which the baseline test
                // proves the end user can read as the org party.
                Parties = [.. parties],
                IncludeDialogParty = false,
                UnauthorizedPresentation = DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation.Disabled
            }
        };

        modify?.Invoke(attachment);
        return attachment;
    }
}
