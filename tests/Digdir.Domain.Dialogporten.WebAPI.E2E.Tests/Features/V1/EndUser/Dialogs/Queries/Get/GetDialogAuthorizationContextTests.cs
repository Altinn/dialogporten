using System.Net;
using System.Text.Json;
using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common;
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
                CreateAttachment("granted-attachment", new V1CommonAuthorizationContexts_AuthorizationContext
                {
                    ServiceResource = E2EConstants.AvailableExternalResource,
                    Parties = [ForeignOrgParty],
                    IncludeDialogParty = true,
                    UnauthorizedPresentation = DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation.Disabled
                }),
                CreateAttachment("denied-disabled-attachment", new V1CommonAuthorizationContexts_AuthorizationContext
                {
                    ServiceResource = E2EConstants.UnavailableExternalResource,
                    Parties = [ForeignOrgParty],
                    IncludeDialogParty = true,
                    UnauthorizedPresentation = DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation.Disabled
                }),
                CreateAttachment("denied-redacted-attachment", new V1CommonAuthorizationContexts_AuthorizationContext
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
                            AuthorizationContext = new V1CommonAuthorizationContexts_AuthorizationContext
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

    /// <summary>
    /// Parent-first narrowing for every child surface at once, including the security-relevant consequence: a
    /// child whose parent is denied must not be issued a context token either, or the holder could use it against
    /// the child's endpoint despite having no access to the transmission it belongs to.
    /// </summary>
    [E2EFact]
    public async Task Denied_Transmission_Should_Deny_All_Children_And_Issue_No_Context_Tokens()
    {
        // Arrange: the transmission refers an unavailable resource; both of its children refer an available one.
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(dialog =>
        {
            dialog.Transmissions =
            [
                CreateTransmission(
                    E2EConstants.UnavailableExternalResource,
                    DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation.Disabled,
                    transmission =>
                    {
                        transmission.Attachments =
                        [
                            CreateTransmissionAttachment("child-attachment", PermissiveChildContext())
                        ];
                        transmission.NavigationalActions =
                        [
                            CreateNavigationalAction("child-nav-action", PermissiveChildContext())
                        ];
                    })
            ];
        });

        // Act
        var response = await Fixture.EndUserApi.GetDialog(dialogId);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = response.Content ?? throw new InvalidOperationException("Dialog content was null.");

        var transmission = content.Transmissions!.Single();
        transmission.IsAuthorized.Should().BeFalse();
        transmission.ContextToken.Should().BeNull("an unauthorized entity is never issued a context token");

        var attachment = transmission.Attachments!.Single();
        attachment.IsAuthorized.Should().BeFalse("a child context can never widen access beyond its parent");
        attachment.ContextToken.Should().BeNull(
            "issuing a token here would let the holder bypass the denied parent transmission");
        attachment.Urls.Should().NotBeEmpty()
            .And.AllSatisfy(url => url.Url.ToString().Should().Be(Constants.UnauthorizedUri.ToString()));

        var navigationalAction = transmission.NavigationalActions!.Single();
        navigationalAction.IsAuthorized.Should().BeFalse("a child context can never widen access beyond its parent");
        navigationalAction.ContextToken.Should().BeNull(
            "issuing a token here would let the holder bypass the denied parent transmission");
        navigationalAction.Url.ToString().Should().Be(Constants.UnauthorizedUri.ToString());
    }

    /// <summary>
    /// Api actions carry authorization contexts too, and are the only surface whose URLs live on child endpoint
    /// objects rather than the entity itself — so masking has its own code path worth exercising.
    /// </summary>
    [E2EFact]
    public async Task Api_Action_Context_Should_Mask_All_Endpoints_When_Denied_And_Keep_Them_When_Granted()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(dialog =>
        {
            dialog.ApiActions =
            [
                CreateApiAction("granted-api-action", E2EConstants.AvailableExternalResource),
                CreateApiAction("denied-api-action", E2EConstants.UnavailableExternalResource)
            ];
        });

        // Act
        var response = await Fixture.EndUserApi.GetDialog(dialogId);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = response.Content ?? throw new InvalidOperationException("Dialog content was null.");

        var granted = content.ApiActions!.Single(a => a.Name == "granted-api-action");
        granted.IsAuthorized.Should().BeTrue();
        granted.ContextToken.Should().NotBeNullOrEmpty();
        granted.Endpoints.Should().NotBeEmpty()
            .And.AllSatisfy(e => e.Url.ToString().Should().NotBe(Constants.UnauthorizedUri.ToString()));

        var denied = content.ApiActions!.Single(a => a.Name == "denied-api-action");
        denied.IsAuthorized.Should().BeFalse();
        denied.ContextToken.Should().BeNull();
        denied.Endpoints.Should().NotBeEmpty()
            .And.AllSatisfy(e => e.Url.ToString().Should().Be(Constants.UnauthorizedUri.ToString()),
                "every endpoint of a denied api action must be masked, including deprecated ones");
    }

    /// <summary>
    /// The difference between the two unauthorized presentations, on a transmission. Disabled keeps the
    /// transmission legible — title and summary survive — and masks only what would grant access. Redacted
    /// (covered by <see cref="Should_Evaluate_Multi_Party_AuthorizationContext_On_Dialog_Attachments"/> for
    /// attachments) strips content entirely.
    /// </summary>
    [E2EFact]
    public async Task Denied_Transmission_With_Disabled_Presentation_Should_Keep_Content_But_Mask_Access()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(dialog =>
        {
            dialog.Transmissions =
            [
                CreateTransmission(
                    E2EConstants.UnavailableExternalResource,
                    DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation.Disabled),
                CreateTransmission(
                    E2EConstants.UnavailableExternalResource,
                    DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation.Redacted)
            ];
        });

        // Act
        var response = await Fixture.EndUserApi.GetDialog(dialogId);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = response.Content ?? throw new InvalidOperationException("Dialog content was null.");

        content.Transmissions.Should().HaveCount(2);
        content.Transmissions!.Should().AllSatisfy(t =>
        {
            t.IsAuthorized.Should().BeFalse();
            t.ContextToken.Should().BeNull();
        });

        var disabled = content.Transmissions!.Single(t => t.Content is not null);
        disabled.Content!.Title.Should().NotBeNull("disabled keeps the transmission legible");
        disabled.Sender.Should().NotBeNull();

        var redacted = content.Transmissions!.Single(t => t.Content is null);
        redacted.Sender.Should().BeNull("redacted strips the transmission to a tombstone");
        redacted.ExtendedType.Should().BeNull();
    }

    /// <summary>
    /// The standalone transmission endpoints must expose the same authorization outcome and the same context
    /// token as get-dialog. Without this, a consumer fetching a transmission directly would be told it is
    /// authorized but given no token to act on it.
    /// </summary>
    [E2EFact]
    public async Task Standalone_Transmission_Endpoints_Should_Expose_The_Same_Context_Tokens_As_Get_Dialog()
    {
        // Arrange
        var transmissionId = Guid.CreateVersion7();
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(dialog =>
        {
            dialog.Transmissions =
            [
                CreateTransmission(
                    E2EConstants.AvailableExternalResource,
                    DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation.Disabled,
                    transmission =>
                    {
                        transmission.Id = transmissionId;
                        transmission.Attachments =
                        [
                            CreateTransmissionAttachment("parity-attachment", PermissiveChildContext())
                        ];
                        transmission.NavigationalActions =
                        [
                            CreateNavigationalAction("parity-nav-action", PermissiveChildContext())
                        ];
                    })
            ];
        });

        // Act
        var dialogResponse = await Fixture.EndUserApi.GetDialog(dialogId);
        var transmissionResponse = await Fixture.EndUserApi.GetTransmission(dialogId, transmissionId);
        var searchResponse = await Fixture.EndUserApi.SearchTransmissions(dialogId);

        // Assert
        dialogResponse.ShouldHaveStatusCode(HttpStatusCode.OK);
        transmissionResponse.ShouldHaveStatusCode(HttpStatusCode.OK);
        searchResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        var fromDialog = (dialogResponse.Content ?? throw new InvalidOperationException("Dialog content was null."))
            .Transmissions!.Single();
        var fromGet = transmissionResponse.Content ?? throw new InvalidOperationException("Transmission was null.");
        var fromSearch = (searchResponse.Content ?? throw new InvalidOperationException("Search result was null."))
            .Single();

        // Every surface must be authorized and carry a token. Tokens are per-request (they embed jti/iat), so
        // assert presence and equivalent claims rather than string equality across responses.
        foreach (var (label, isAuthorized, contextToken) in new[]
                 {
                     ("get-dialog transmission", fromDialog.IsAuthorized, fromDialog.ContextToken),
                     ("get-transmission", fromGet.IsAuthorized, fromGet.ContextToken),
                     ("search-transmissions", fromSearch.IsAuthorized, fromSearch.ContextToken),
                     ("get-dialog attachment", fromDialog.Attachments!.Single().IsAuthorized, fromDialog.Attachments!.Single().ContextToken),
                     ("get-transmission attachment", fromGet.Attachments!.Single().IsAuthorized, fromGet.Attachments!.Single().ContextToken),
                     ("search attachment", fromSearch.Attachments!.Single().IsAuthorized, fromSearch.Attachments!.Single().ContextToken),
                     ("get-dialog nav action", fromDialog.NavigationalActions!.Single().IsAuthorized, fromDialog.NavigationalActions!.Single().ContextToken),
                     ("get-transmission nav action", fromGet.NavigationalActions!.Single().IsAuthorized, fromGet.NavigationalActions!.Single().ContextToken),
                     ("search nav action", fromSearch.NavigationalActions!.Single().IsAuthorized, fromSearch.NavigationalActions!.Single().ContextToken)
                 })
        {
            isAuthorized.Should().BeTrue($"{label} should be authorized");
            contextToken.Should().NotBeNullOrEmpty($"{label} should carry a context token");
        }

        // The transmission's token asserts the same grant on all three surfaces.
        var expected = await VerifyContextToken(fromDialog.ContextToken!);
        foreach (var token in new[] { fromGet.ContextToken!, fromSearch.ContextToken! })
        {
            var actual = await VerifyContextToken(token);
            actual.GetString(DialogTokenClaimTypes.EntityId).Should().Be(expected.GetString(DialogTokenClaimTypes.EntityId));
            actual.GetString(DialogTokenClaimTypes.EntityType).Should().Be(DialogContextTokenEntityTypes.Transmission);
            actual.GetString(DialogTokenClaimTypes.Actions).Should().Be(expected.GetString(DialogTokenClaimTypes.Actions));
            actual.GetStringOrNull(DialogTokenClaimTypes.EffectiveResource)
                .Should().Be(expected.GetStringOrNull(DialogTokenClaimTypes.EffectiveResource));
            actual.GetStringList(DialogTokenClaimTypes.PermittedParties)
                .Should().Equal(expected.GetStringList(DialogTokenClaimTypes.PermittedParties));
        }
    }

    /// <summary>
    /// Pins the whole wire shape of a context-heavy dialog: every context-carrying surface, once granted and once
    /// denied under each unauthorized presentation. Redaction removes fields rather than emptying them, so a
    /// snapshot is the only assertion that catches an accidental leak of a field nobody thought to check.
    /// Tokens are scrubbed by <see cref="JsonSnapshotVerifier"/>; their claims are asserted in the tests above.
    /// </summary>
    [E2EFact(SkipOnEnvironments = ["yt01"])]
    public async Task Get_Dialog_With_Authorization_Contexts_Verify_Snapshot()
    {
        // Arrange
        // Every child collection comes back ordered by CreatedAt then Id. Everything below is created in a
        // single request, so CreatedAt ties across siblings and Id decides — and UUIDv7s minted in the same
        // millisecond differ only in their random tail. Hand each collection pre-sorted ids so the snapshot
        // order matches the declaration order instead of being a coin flip.
        //
        // Exception: navigational actions. Their create contract has no Id property (the database mints one
        // with gen_random_uuid()), so the two below cannot be pre-sorted and may swap order in the snapshot
        // when CreatedAt ties. Non-snapshot tests elsewhere in this file are unaffected — they select
        // navigational actions by name/property rather than by position.
        var attachmentIds = OrderedVersion7Ids(3);
        var guiActionIds = OrderedVersion7Ids(2);
        var apiActionIds = OrderedVersion7Ids(2);
        var transmissionIds = OrderedVersion7Ids(3);
        var childAttachmentIds = OrderedVersion7Ids(2);

        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(dialog =>
        {
            dialog.Attachments =
            [
                CreateAttachment("granted-attachment", ChildContext(E2EConstants.AvailableExternalResource,
                    DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation.Disabled),
                    attachmentIds[0]),
                CreateAttachment("denied-disabled-attachment", ChildContext(E2EConstants.UnavailableExternalResource,
                    DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation.Disabled),
                    attachmentIds[1]),
                CreateAttachment("denied-redacted-attachment", ChildContext(E2EConstants.UnavailableExternalResource,
                    DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation.Redacted),
                    attachmentIds[2])
            ];

            dialog.GuiActions =
            [
                CreateGuiAction("granted-gui-action", E2EConstants.AvailableExternalResource,
                    id: guiActionIds[0]),
                CreateGuiAction("denied-gui-action", E2EConstants.UnavailableExternalResource,
                    DialogsEntitiesActions_DialogGuiActionPriority.Secondary, guiActionIds[1])
            ];

            dialog.ApiActions =
            [
                CreateApiAction("granted-api-action", E2EConstants.AvailableExternalResource, apiActionIds[0]),
                CreateApiAction("denied-api-action", E2EConstants.UnavailableExternalResource, apiActionIds[1])
            ];

            dialog.Transmissions =
            [
                CreateTransmission(
                    E2EConstants.AvailableExternalResource,
                    DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation.Disabled,
                    transmission =>
                    {
                        transmission.ExternalReference = "granted-transmission";
                        transmission.Attachments =
                        [
                            CreateTransmissionAttachment("granted-child-attachment",
                                ChildContext(E2EConstants.AvailableExternalResource,
                                    DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation.Disabled),
                                childAttachmentIds[0]),
                            CreateTransmissionAttachment("denied-child-attachment",
                                ChildContext(E2EConstants.UnavailableExternalResource,
                                    DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation.Disabled),
                                childAttachmentIds[1])
                        ];
                        transmission.NavigationalActions =
                        [
                            CreateNavigationalAction("granted-child-nav-action",
                                ChildContext(E2EConstants.AvailableExternalResource,
                                    DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation.Disabled)),
                            CreateNavigationalAction("denied-child-nav-action",
                                ChildContext(E2EConstants.UnavailableExternalResource,
                                    DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation.Redacted))
                        ];
                    },
                    transmissionIds[0]),
                CreateTransmission(
                    E2EConstants.UnavailableExternalResource,
                    DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation.Disabled,
                    transmission => transmission.ExternalReference = "denied-disabled-transmission",
                    transmissionIds[1]),
                CreateTransmission(
                    E2EConstants.UnavailableExternalResource,
                    DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation.Redacted,
                    transmission => transmission.ExternalReference = "denied-redacted-transmission",
                    transmissionIds[2])
            ];
        });

        // Act
        var response = await Fixture.EndUserApi.GetDialog(
            dialogId, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        response.Content.Should().NotBeNull();
        await JsonSnapshotVerifier.VerifyJsonSnapshot(JsonSerializer.Serialize(response.Content));
    }

    private async Task<VerifiedToken> VerifyContextToken(string contextToken)
    {
        var token = await DialogportenTokenVerifier.VerifyAsync(
            Fixture.WebApiUri, contextToken, TestContext.Current.CancellationToken);

        token.TokenType.Should().Be(DialogTokenTypes.DialogContextToken);
        return token;
    }

    // A context that would grant access on its own, used to prove a child cannot widen its parent's access.
    private static V1CommonAuthorizationContexts_AuthorizationContext PermissiveChildContext() =>
        ChildContext(
            E2EConstants.AvailableExternalResource,
            DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation.Disabled);

    private static V1CommonAuthorizationContexts_AuthorizationContext ChildContext(
        string serviceResource,
        DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation unauthorizedPresentation) =>
        new()
        {
            ServiceResource = serviceResource,
            IncludeDialogParty = true,
            UnauthorizedPresentation = unauthorizedPresentation
        };

    private static V1ServiceOwnerDialogsCommandsCreate_GuiAction CreateGuiAction(
        string name,
        string serviceResource,
        DialogsEntitiesActions_DialogGuiActionPriority priority =
            DialogsEntitiesActions_DialogGuiActionPriority.Primary,
        Guid? id = null) =>
        new()
        {
            Id = id ?? Guid.CreateVersion7(),
            Url = new Uri($"https://digdir.apps.tt02.altinn.no/{name}"),
            Priority = priority,
            Title = [DialogTestData.CreateLocalization(name)],
            AuthorizationContext = new V1CommonAuthorizationContexts_AuthorizationContext
            {
                Action = Constants.ReadAction,
                ServiceResource = serviceResource,
                IncludeDialogParty = true,
                UnauthorizedPresentation = DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation.Disabled
            }
        };

    private static V1ServiceOwnerDialogsCommandsCreate_Transmission CreateTransmission(
        string serviceResource,
        DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation unauthorizedPresentation,
        Action<V1ServiceOwnerDialogsCommandsCreate_Transmission>? modify = null,
        Guid? id = null)
    {
        var transmission = new V1ServiceOwnerDialogsCommandsCreate_Transmission
        {
            Id = id ?? Guid.CreateVersion7(),
            Type = DialogsEntitiesTransmissions_DialogTransmissionType.Information,
            Sender = new V1ServiceOwnerCommonActors_Actor { ActorType = Actors_ActorType.ServiceOwner },
            AuthorizationContext = new V1CommonAuthorizationContexts_AuthorizationContext
            {
                ServiceResource = serviceResource,
                IncludeDialogParty = true,
                UnauthorizedPresentation = unauthorizedPresentation
            },
            Content = new V1ServiceOwnerDialogsCommandsCreate_TransmissionContent
            {
                Title = DialogTestData.CreateContentValue("Tittel", "nb"),
                Summary = DialogTestData.CreateContentValue("Sammendrag", "nb")
            }
        };

        modify?.Invoke(transmission);
        return transmission;
    }

    /// <summary>
    /// UUIDv7 ids in ascending order, for collections whose response order is decided by Id.
    /// </summary>
    private static Guid[] OrderedVersion7Ids(int count) =>
        Enumerable.Range(0, count)
            .Select(_ => Guid.CreateVersion7())
            .Order()
            .ToArray();

    private static V1ServiceOwnerDialogsCommandsCreate_TransmissionAttachment CreateTransmissionAttachment(
        string name,
        V1CommonAuthorizationContexts_AuthorizationContext authorizationContext,
        Guid? id = null) =>
        new()
        {
            Id = id ?? Guid.CreateVersion7(),
            DisplayName = [DialogTestData.CreateLocalization(name)],
            Urls =
            [
                new V1ServiceOwnerDialogsCommandsCreate_TransmissionAttachmentUrl
                {
                    Url = new Uri($"https://digdir.apps.tt02.altinn.no/some-attachment/{name}"),
                    ConsumerType = Attachments_AttachmentUrlConsumerType.Gui
                }
            ],
            AuthorizationContext = authorizationContext
        };

    private static V1ServiceOwnerDialogsCommandsCreate_TransmissionNavigationalAction CreateNavigationalAction(
        string name,
        V1CommonAuthorizationContexts_AuthorizationContext authorizationContext) =>
        new()
        {
            Title = [DialogTestData.CreateLocalization(name)],
            Url = new Uri($"https://digdir.apps.tt02.altinn.no/some-nav-action/{name}"),
            AuthorizationContext = authorizationContext
        };

    private static V1ServiceOwnerDialogsCommandsCreate_ApiAction CreateApiAction(
        string name,
        string serviceResource,
        Guid? id = null) =>
        new()
        {
            Id = id ?? Guid.CreateVersion7(),
            Name = name,
            Endpoints =
            [
                new V1ServiceOwnerDialogsCommandsCreate_ApiActionEndpoint
                {
                    Url = new Uri($"https://digdir.apps.tt02.altinn.no/api/{name}"),
                    HttpMethod = Http_HttpVerb.GET
                },
                new V1ServiceOwnerDialogsCommandsCreate_ApiActionEndpoint
                {
                    Url = new Uri($"https://digdir.apps.tt02.altinn.no/api/{name}/deprecated"),
                    HttpMethod = Http_HttpVerb.GET,
                    Deprecated = true
                }
            ],
            AuthorizationContext = new V1CommonAuthorizationContexts_AuthorizationContext
            {
                Action = Constants.ReadAction,
                ServiceResource = serviceResource,
                IncludeDialogParty = true,
                UnauthorizedPresentation = DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation.Disabled
            }
        };

    private static V1ServiceOwnerDialogsCommandsCreate_Attachment CreateAttachment(
        string name,
        V1CommonAuthorizationContexts_AuthorizationContext authorizationContext,
        Guid? id = null) =>
        new()
        {
            Id = id ?? Guid.CreateVersion7(),
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
