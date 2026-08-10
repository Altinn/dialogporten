using System.Net;
using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Extensions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using Constants = Digdir.Domain.Dialogporten.Application.Common.Authorization.Constants;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.Dialogs.Queries.Get;

[Collection(nameof(WebApiTestCollectionFixture))]
public class GetDialogAuthorizationContextTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    // A foreign party the default test end user does not represent; used to exercise
    // multi-party (OR) semantics against the real PDP.
    private const string ForeignOrgParty = "urn:altinn:organization:identifier-no:991825827";

    [E2EFact]
    public async Task Should_Evaluate_Multi_Party_AuthorizationContext_On_Dialog_Attachments()
    {
        // Arrange: three dialog attachments with authorization contexts:
        // 1. Available external resource, evaluated for a foreign party OR the dialog party
        //    => authorized via the dialog party (multi-party OR semantics).
        // 2. Unavailable external resource with unauthorizedPresentation = disabled => masked URLs.
        // 3. Unavailable external resource with unauthorizedPresentation = redacted => stripped to a tombstone.
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(dialog =>
        {
            dialog.Attachments =
            [
                CreateAttachment("granted-attachment", new V1CommonAuthorizationContexts_ChildAuthorizationContext
                {
                    ServiceResource = E2EConstants.AvailableExternalResource,
                    Parties = [ForeignOrgParty],
                    IncludeDialogParty = true,
                    UnauthorizedPresentation = DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation.Disabled
                }),
                CreateAttachment("denied-disabled-attachment", new V1CommonAuthorizationContexts_ChildAuthorizationContext
                {
                    ServiceResource = E2EConstants.UnavailableExternalResource,
                    Parties = [ForeignOrgParty],
                    IncludeDialogParty = true,
                    UnauthorizedPresentation = DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation.Disabled
                }),
                CreateAttachment("denied-redacted-attachment", new V1CommonAuthorizationContexts_ChildAuthorizationContext
                {
                    ServiceResource = E2EConstants.UnavailableExternalResource,
                    Parties = [ForeignOrgParty],
                    IncludeDialogParty = true,
                    UnauthorizedPresentation = DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation.Redacted
                })
            ];
        });

        // Act
        var response = await Fixture.EndUserApi.GetDialog(dialogId);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = response.Content ?? throw new InvalidOperationException("Dialog content was null.");

        content.Attachments.Should().HaveCount(3);

        var grantedAttachment = content.Attachments.Single(a => a.Name == "granted-attachment");
        grantedAttachment.IsAuthorized.Should().BeTrue();
        grantedAttachment.Urls.Should().NotBeEmpty()
            .And.AllSatisfy(url => url.Url.ToString().Should().NotBe(Constants.UnauthorizedUri.ToString()));

        var deniedAttachment = content.Attachments.Single(a => a.Name == "denied-disabled-attachment");
        deniedAttachment.IsAuthorized.Should().BeFalse();
        deniedAttachment.Urls.Should().NotBeEmpty()
            .And.AllSatisfy(url => url.Url.ToString().Should().Be(Constants.UnauthorizedUri.ToString()));

        // The redacted attachment is stripped to a tombstone: existence only
        var redactedAttachment = content.Attachments.Single(a => !a.IsAuthorized && a.Name == null);
        redactedAttachment.DisplayName.Should().BeNullOrEmpty();
        redactedAttachment.Urls.Should().BeNullOrEmpty();
    }

    [E2EFact]
    public async Task Should_Not_Widen_Access_For_Transmission_Children_When_Parent_Is_Denied()
    {
        // Arrange: the transmission itself refers an unavailable external resource, while its
        // navigational action's context refers an available one. Parent-first narrowing must
        // still mask the navigational action.
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(dialog =>
        {
            dialog.Transmissions =
            [
                new V1ServiceOwnerDialogsCommandsCreate_Transmission
                {
                    Type = DialogsEntitiesTransmissions_DialogTransmissionType.Information,
                    Sender = new V1ServiceOwnerCommonActors_Actor { ActorType = Actors_ActorType.ServiceOwner },
                    AuthorizationContext = new V1CommonAuthorizationContexts_AuthorizationContext
                    {
                        ServiceResource = E2EConstants.UnavailableExternalResource,
                        IncludeDialogParty = true,
                        UnauthorizedPresentation = DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation.Disabled
                    },
                    Content = new V1ServiceOwnerDialogsCommandsCreate_TransmissionContent
                    {
                        Title = new V1CommonContent_ContentValue
                        {
                            MediaType = "text/plain",
                            Value = [new V1CommonLocalizations_Localization { LanguageCode = "nb", Value = "Tittel" }]
                        },
                        Summary = new V1CommonContent_ContentValue
                        {
                            MediaType = "text/plain",
                            Value = [new V1CommonLocalizations_Localization { LanguageCode = "nb", Value = "Sammendrag" }]
                        }
                    },
                    NavigationalActions =
                    [
                        new V1ServiceOwnerDialogsCommandsCreate_TransmissionNavigationalAction
                        {
                            Title = [new V1CommonLocalizations_Localization { LanguageCode = "nb", Value = "Gå til sak" }],
                            Url = new Uri("https://digdir.apps.tt02.altinn.no/some-nav-action"),
                            AuthorizationContext = new V1CommonAuthorizationContexts_ChildAuthorizationContext
                            {
                                ServiceResource = E2EConstants.AvailableExternalResource,
                                IncludeDialogParty = true,
                                UnauthorizedPresentation = DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation.Disabled
                            }
                        }
                    ]
                }
            ];
        });

        // Act
        var response = await Fixture.EndUserApi.GetDialog(dialogId);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = response.Content ?? throw new InvalidOperationException("Dialog content was null.");

        var transmission = content.Transmissions!.Single();
        transmission.IsAuthorized.Should().BeFalse();

        var navigationalAction = transmission.NavigationalActions!.Single();
        navigationalAction.IsAuthorized.Should().BeFalse("a child context can never widen access beyond its parent");
        navigationalAction.Url.ToString().Should().Be(Constants.UnauthorizedUri.ToString());
    }

    private static V1ServiceOwnerDialogsCommandsCreate_Attachment CreateAttachment(
        string name,
        V1CommonAuthorizationContexts_ChildAuthorizationContext authorizationContext) =>
        new()
        {
            Name = name,
            DisplayName = [new V1CommonLocalizations_Localization { LanguageCode = "nb", Value = name }],
            Urls =
            [
                new V1ServiceOwnerDialogsCommandsCreate_AttachmentUrl
                {
                    Url = new Uri($"https://digdir.apps.tt02.altinn.no/some-attachment/{name}"),
                    ConsumerType = Attachments_AttachmentUrlConsumerType.Gui
                }
            ],
            AuthorizationContext = authorizationContext
        };
}
