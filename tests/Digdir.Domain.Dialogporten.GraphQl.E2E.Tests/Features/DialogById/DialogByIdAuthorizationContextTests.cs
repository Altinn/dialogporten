using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using StrawberryShake;
using Constants = Digdir.Domain.Dialogporten.Application.Common.Authorization.Constants;

namespace Digdir.Domain.Dialogporten.GraphQl.E2E.Tests.Features.DialogById;

/// <summary>
/// GraphQL and REST decorate authorization results through separate code paths, so the GraphQL surface needs its
/// own coverage. The expectations here deliberately mirror
/// <c>WebAPI.E2E.Tests/…/Get/GetDialogAuthorizationContextTests</c> on an identically shaped dialog, so the two
/// pipelines cannot drift without one of the suites failing.
/// </summary>
[Collection(nameof(GraphQlTestCollectionFixture))]
public class DialogByIdAuthorizationContextTests(GraphQlE2EFixture fixture) : E2ETestBase<GraphQlE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Expose_IsAuthorized_On_Every_Context_Carrying_Surface()
    {
        // Arrange: one granted and one denied entity on each of the six surfaces.
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(dialog =>
        {
            dialog.Attachments =
            [
                CreateAttachment("granted-attachment", E2EConstants.AvailableExternalResource),
                CreateAttachment("denied-attachment", E2EConstants.UnavailableExternalResource)
            ];
            dialog.GuiActions =
            [
                CreateGuiAction("granted-gui-action", E2EConstants.AvailableExternalResource),
                CreateGuiAction("denied-gui-action", E2EConstants.UnavailableExternalResource,
                    DialogsEntitiesActions_DialogGuiActionPriority.Secondary)
            ];
            dialog.ApiActions =
            [
                CreateApiAction("granted-api-action", E2EConstants.AvailableExternalResource),
                CreateApiAction("denied-api-action", E2EConstants.UnavailableExternalResource)
            ];
            dialog.Transmissions =
            [
                CreateTransmission("granted", E2EConstants.AvailableExternalResource),
                CreateTransmission("denied", E2EConstants.UnavailableExternalResource)
            ];
        });

        // Act
        var result = await Fixture.GraphQlClient.GetDialogById.ExecuteAsync(
            dialogId, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.Should().BeEmpty();
        result.Data.Should().NotBeNull();
        var dialog = result.Data.DialogById.Dialog;
        dialog.Should().NotBeNull();

        // Dialog attachments
        var grantedAttachment = dialog.Attachments.Single(a => a.Name == "granted-attachment");
        grantedAttachment.IsAuthorized.Should().BeTrue();

        var deniedAttachment = dialog.Attachments.Single(a => a.Name == "denied-attachment");
        deniedAttachment.IsAuthorized.Should().BeFalse();

        // Gui actions
        var grantedGuiAction = dialog.GuiActions.Single(a => a.Url.ToString().EndsWith("granted-gui-action", StringComparison.Ordinal));
        grantedGuiAction.IsAuthorized.Should().BeTrue();
        grantedGuiAction.Action.Should().Be(Constants.ReadAction, "the action is surfaced from the context");

        var deniedGuiAction = dialog.GuiActions.Single(a => a.Url.ToString() == Constants.UnauthorizedUri.ToString());
        deniedGuiAction.IsAuthorized.Should().BeFalse();

        // Api actions
        var grantedApiAction = dialog.ApiActions.Single(a => a.Name == "granted-api-action");
        grantedApiAction.IsAuthorized.Should().BeTrue();

        var deniedApiAction = dialog.ApiActions.Single(a => a.Name == "denied-api-action");
        deniedApiAction.IsAuthorized.Should().BeFalse();

        // Transmissions, and their attachments and navigational actions
        var grantedTransmission = dialog.Transmissions.Single(t => t.ExternalReference == "granted");
        grantedTransmission.IsAuthorized.Should().BeTrue();
        grantedTransmission.Attachments.Single().IsAuthorized.Should().BeTrue();
        grantedTransmission.NavigationalActions.Single().IsAuthorized.Should().BeTrue();

        var deniedTransmission = dialog.Transmissions.Single(t => t.ExternalReference == "denied");
        deniedTransmission.IsAuthorized.Should().BeFalse();

        // Parent-first narrowing holds on the GraphQL surface too: the children's own contexts refer the
        // available resource, yet they are denied because their parent is denied.
        deniedTransmission.Attachments.Single().IsAuthorized.Should().BeFalse();
        deniedTransmission.NavigationalActions.Single().IsAuthorized.Should().BeFalse();
    }

    [E2EFact]
    public async Task Dialog_Without_Authorization_Contexts_Should_Still_Issue_A_Dialog_Token()
    {
        // Arrange: the complex dialog uses legacy authorization attributes throughout, no contexts.
        var dialogId = await Fixture.ServiceownerApi.CreateComplexDialogAsync();

        // Act
        var result = await Fixture.GraphQlClient.GetDialogById.ExecuteAsync(
            dialogId, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.Should().BeEmpty();
        result.Data.Should().NotBeNull();
        var dialog = result.Data.DialogById.Dialog;
        dialog.Should().NotBeNull();

        dialog.DialogToken.Should().NotBeNullOrEmpty("legacy grants are still expressed through the dialog token");
    }

    private static V1ServiceOwnerDialogsCommandsCreate_Attachment CreateAttachment(string name, string serviceResource) =>
        new()
        {
            Name = name,
            DisplayName = [DialogTestData.CreateLocalization(name)],
            Urls =
            [
                new V1ServiceOwnerDialogsCommandsCreate_AttachmentUrl
                {
                    Url = new Uri($"https://digdir.apps.tt02.altinn.no/attachment/{name}"),
                    ConsumerType = Attachments_AttachmentUrlConsumerType.Gui
                }
            ],
            AuthorizationContext = ChildContext(serviceResource)
        };

    private static V1ServiceOwnerDialogsCommandsCreate_GuiAction CreateGuiAction(
        string name,
        string serviceResource,
        DialogsEntitiesActions_DialogGuiActionPriority priority =
            DialogsEntitiesActions_DialogGuiActionPriority.Primary) =>
        new()
        {
            Url = new Uri($"https://digdir.apps.tt02.altinn.no/{name}"),
            Priority = priority,
            Title = [DialogTestData.CreateLocalization(name)],
            AuthorizationContext = Context(serviceResource)
        };

    private static V1ServiceOwnerDialogsCommandsCreate_ApiAction CreateApiAction(string name, string serviceResource) =>
        new()
        {
            Name = name,
            Endpoints =
            [
                new V1ServiceOwnerDialogsCommandsCreate_ApiActionEndpoint
                {
                    Url = new Uri($"https://digdir.apps.tt02.altinn.no/api/{name}"),
                    HttpMethod = Http_HttpVerb.GET
                }
            ],
            AuthorizationContext = Context(serviceResource)
        };

    private static V1ServiceOwnerDialogsCommandsCreate_Transmission CreateTransmission(string externalReference, string serviceResource) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            Type = DialogsEntitiesTransmissions_DialogTransmissionType.Information,
            Sender = new V1ServiceOwnerCommonActors_Actor { ActorType = Actors_ActorType.ServiceOwner },
            ExternalReference = externalReference,
            AuthorizationContext = Context(serviceResource),
            Content = new V1ServiceOwnerDialogsCommandsCreate_TransmissionContent
            {
                Title = DialogTestData.CreateContentValue("Tittel", "nb"),
                Summary = DialogTestData.CreateContentValue("Sammendrag", "nb")
            },
            Attachments =
            [
                new V1ServiceOwnerDialogsCommandsCreate_TransmissionAttachment
                {
                    Id = Guid.CreateVersion7(),
                    DisplayName = [DialogTestData.CreateLocalization($"{externalReference}-attachment")],
                    Urls =
                    [
                        new V1ServiceOwnerDialogsCommandsCreate_TransmissionAttachmentUrl
                        {
                            Url = new Uri($"https://digdir.apps.tt02.altinn.no/attachment/{externalReference}"),
                            ConsumerType = Attachments_AttachmentUrlConsumerType.Gui
                        }
                    ],
                    // Always the available resource, so a denied parent is the only thing that can deny this.
                    AuthorizationContext = ChildContext(E2EConstants.AvailableExternalResource)
                }
            ],
            NavigationalActions =
            [
                new V1ServiceOwnerDialogsCommandsCreate_TransmissionNavigationalAction
                {
                    Title = [DialogTestData.CreateLocalization($"{externalReference}-nav-action")],
                    Url = new Uri($"https://digdir.apps.tt02.altinn.no/nav/{externalReference}"),
                    AuthorizationContext = ChildContext(E2EConstants.AvailableExternalResource)
                }
            ]
        };

    private static V1CommonAuthorizationContexts_AuthorizationContext Context(string serviceResource) =>
        new()
        {
            Action = Constants.ReadAction,
            ServiceResource = serviceResource,
            IncludeDialogParty = true,
            UnauthorizedPresentation = DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation.Disabled
        };

    private static V1CommonAuthorizationContexts_AuthorizationContext ChildContext(string serviceResource) =>
        new()
        {
            ServiceResource = serviceResource,
            IncludeDialogParty = true,
            UnauthorizedPresentation = DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation.Disabled
        };
}
