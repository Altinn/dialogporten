using System.Text.Json;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.SearchTransmissions;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;
using CreateContextDto = Digdir.Domain.Dialogporten.Application.Features.V1.Common.AuthorizationContexts.AuthorizationContextDto;
using CreateChildContextDto = Digdir.Domain.Dialogporten.Application.Features.V1.Common.AuthorizationContexts.AuthorizationContextDto;
using GetTransmissionDtoEU = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.GetTransmission.TransmissionDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.Dialogs.Queries.Get;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class GetDialogAuthorizationContextTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    private const string OtherParty = "urn:altinn:organization:identifier-no:991825827";
    private const string ContextResource = "urn:altinn:resource:context-service";

    private static readonly AuthorizationCheck ContextReadCheck =
        new("read", AuthorizationResourceSpec.FromContext(ContextResource, null), [OtherParty]);

    private static CreateContextDto ContextDto(AuthorizationContextUnauthorizedPresentation.Values ifUnauthorized, string? action = "read") =>
        new()
        {
            ServiceResource = ContextResource,
            Parties = [OtherParty],
            Action = action,
            UnauthorizedPresentation = ifUnauthorized
        };

    private static CreateChildContextDto ChildContextDto(AuthorizationContextUnauthorizedPresentation.Values ifUnauthorized) =>
        new()
        {
            ServiceResource = ContextResource,
            Parties = [OtherParty],
            UnauthorizedPresentation = ifUnauthorized
        };

    private static void ConfigureMainReadOnlyAuthorization(IServiceCollection services) =>
        services.ConfigureDialogDetailsAuthorizationResult(new DialogDetailsAuthorizationResult
        {
            AuthorizedChecks = [TestAuthorizedChecks.Authorized(Constants.ReadAction)]
        });

    private static void ConfigureMainReadAndContextAuthorization(IServiceCollection services) =>
        services.ConfigureDialogDetailsAuthorizationResult(new DialogDetailsAuthorizationResult
        {
            AuthorizedChecks =
            [
                TestAuthorizedChecks.Authorized(Constants.ReadAction),
                TestAuthorizedChecks.Authorized(ContextReadCheck)
            ]
        });

    [Fact]
    public Task Context_Entities_Should_Be_Authorized_With_Default_Authorization() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.AddGuiAction(guiAction =>
                {
                    guiAction.Action = null;
                    guiAction.Url = new Uri("https://localhost/gui");
                    guiAction.AuthorizationContext = ContextDto(AuthorizationContextUnauthorizedPresentation.Values.Excluded);
                });
                x.AddAttachment(attachment => attachment.AuthorizationContext = ChildContextDto(AuthorizationContextUnauthorizedPresentation.Values.Excluded));
            })
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                var guiAction = x.GuiActions.Single();
                guiAction.IsAuthorized.Should().BeTrue();
                guiAction.Url.Should().Be(new Uri("https://localhost/gui"));
                // The end user API coalesces the action from the context
                guiAction.Action.Should().Be("read");

                var attachment = x.Attachments.Single();
                attachment.IsAuthorized.Should().BeTrue();
                attachment.Urls.Should().AllSatisfy(url => url.Url.Should().NotBe(Constants.UnauthorizedUri));
            });

    [Fact]
    public Task Context_GuiAction_And_ApiAction_Should_Surface_Read_As_Effective_Action_When_Context_Action_Is_Omitted() =>
        // authorizationContext.action is documented as optional, defaulting to "read" - the mapper must
        // surface that effective value rather than the literal (omitted) input, since both REST and GraphQL
        // declare the action field as required.
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.AddGuiAction(guiAction =>
                {
                    guiAction.Action = null;
                    guiAction.AuthorizationContext = ContextDto(AuthorizationContextUnauthorizedPresentation.Values.Excluded, action: null);
                });
                x.AddApiAction(apiAction =>
                {
                    apiAction.Action = null;
                    apiAction.AuthorizationContext = ContextDto(AuthorizationContextUnauthorizedPresentation.Values.Excluded, action: null);
                });
            })
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                x.GuiActions.Single().Action.Should().Be(Constants.ReadAction);
                x.ApiActions.Single().Action.Should().Be(Constants.ReadAction);
            });

    [Fact]
    public Task Unauthorized_Context_GuiAction_With_Disable_Should_Be_Masked() =>
        FlowBuilder.For(Application, ConfigureMainReadOnlyAuthorization)
            .CreateSimpleDialog((x, _) =>
                x.AddGuiAction(guiAction =>
                {
                    guiAction.Action = null;
                    guiAction.AuthorizationContext = ContextDto(AuthorizationContextUnauthorizedPresentation.Values.Disabled);
                }))
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                var guiAction = x.GuiActions.Single();
                guiAction.IsAuthorized.Should().BeFalse();
                guiAction.Url.Should().Be(Constants.UnauthorizedUri);

                // Disabled is not exclusion: nothing left the list, so the property is absent entirely
                x.ExcludedGuiActions.Should().BeNull();
            });

    [Fact]
    public Task Unauthorized_Context_GuiAction_With_Excluded_Should_Leave_Its_List_For_The_Excluded_List() =>
        FlowBuilder.For(Application, ConfigureMainReadOnlyAuthorization)
            .CreateSimpleDialog((x, _) =>
                x.AddGuiAction(guiAction =>
                {
                    guiAction.Action = null;
                    guiAction.AuthorizationContext = ContextDto(AuthorizationContextUnauthorizedPresentation.Values.Excluded);
                }))
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                x.GuiActions.Should().BeEmpty();
                var excluded = x.ExcludedGuiActions.Should().ContainSingle().Subject;
                excluded.Id.Should().NotBeEmpty();
                excluded.CreatedAt.Should().NotBe(default);
            });

    [Fact]
    public Task Authorized_Context_GuiAction_Should_Keep_Url_When_Context_Check_Is_Granted() =>
        FlowBuilder.For(Application, ConfigureMainReadAndContextAuthorization)
            .CreateSimpleDialog((x, _) =>
                x.AddGuiAction(guiAction =>
                {
                    guiAction.Action = null;
                    guiAction.Url = new Uri("https://localhost/gui");
                    guiAction.AuthorizationContext = ContextDto(AuthorizationContextUnauthorizedPresentation.Values.Excluded);
                }))
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                var guiAction = x.GuiActions.Single();
                guiAction.IsAuthorized.Should().BeTrue();
                guiAction.Url.Should().Be(new Uri("https://localhost/gui"));
            });

    [Fact]
    public Task Unauthorized_Context_Dialog_Attachment_With_Disable_Should_Mask_Urls() =>
        FlowBuilder.For(Application, ConfigureMainReadOnlyAuthorization)
            .CreateSimpleDialog((x, _) =>
                x.AddAttachment(attachment => attachment.AuthorizationContext = ChildContextDto(AuthorizationContextUnauthorizedPresentation.Values.Disabled)))
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                var attachment = x.Attachments.Single();
                attachment.IsAuthorized.Should().BeFalse();
                attachment.Urls.Should().NotBeEmpty()
                    .And.AllSatisfy(url => url.Url.Should().Be(Constants.UnauthorizedUri));
            });

    [Fact]
    public Task Unauthorized_Context_Dialog_Attachment_With_Excluded_Should_Leave_Its_List_For_The_Excluded_List() =>
        FlowBuilder.For(Application, ConfigureMainReadOnlyAuthorization)
            .CreateSimpleDialog((x, _) =>
                x.AddAttachment(attachment => attachment.AuthorizationContext = ChildContextDto(AuthorizationContextUnauthorizedPresentation.Values.Excluded)))
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                x.Attachments.Should().BeEmpty();
                x.ExcludedAttachments.Should().ContainSingle().Subject.Id.Should().NotBeEmpty();
            });

    [Fact]
    public async Task Unauthorized_Context_Transmission_With_Excluded_Should_Leave_Its_List_In_GetDialog()
    {
        var transmissionId = NewUuidV7();

        await FlowBuilder.For(Application, ConfigureMainReadOnlyAuthorization)
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(transmission =>
                {
                    transmission.Id = transmissionId;
                    transmission.AuthorizationAttribute = null;
                    transmission.AuthorizationContext = ContextDto(AuthorizationContextUnauthorizedPresentation.Values.Excluded, action: null);
                    transmission.AddAttachment();
                    transmission.AddNavigationalAction();
                }))
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                // The transmission and everything under it is gone; only the stub says it existed
                x.Transmissions.Should().BeEmpty();
                var excluded = x.ExcludedTransmissions.Should().ContainSingle().Subject;
                excluded.Id.Should().Be(transmissionId);
                excluded.CreatedAt.Should().NotBe(default);
            });
    }

    [Fact]
    public async Task Unauthorized_Context_Transmission_With_Excluded_Should_Be_Forbidden_In_GetTransmission()
    {
        var transmissionId = NewUuidV7();

        await FlowBuilder.For(Application, ConfigureMainReadOnlyAuthorization)
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(transmission =>
                {
                    transmission.Id = transmissionId;
                    transmission.AuthorizationAttribute = null;
                    transmission.AuthorizationContext = ContextDto(AuthorizationContextUnauthorizedPresentation.Values.Excluded, action: null);
                    transmission.AddAttachment();
                }))
            .GetEndUserTransmission(transmissionId)
            // 403, not 404: the dialog's own excludedTransmissions already says this transmission exists
            .ExecuteAndAssert<Forbidden>();
    }

    [Fact]
    public Task Unauthorized_Context_Transmission_With_Excluded_Should_Be_Dropped_From_Search() =>
        FlowBuilder.For(Application, ConfigureMainReadOnlyAuthorization)
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(transmission =>
                {
                    transmission.AuthorizationAttribute = null;
                    transmission.AuthorizationContext = ContextDto(AuthorizationContextUnauthorizedPresentation.Values.Excluded, action: null);
                    transmission.AddAttachment();
                }))
            .SendCommand((_, ctx) => new SearchTransmissionQuery
            {
                DialogId = ctx.GetDialogId(),
            })
            // A bare JSON array has nowhere to report exclusions; the dialog GET is the authoritative timeline
            .ExecuteAndAssert<List<TransmissionDto>>(x => x.Should().BeEmpty());

    [Fact]
    public async Task Child_Context_Grant_Should_Not_Widen_Access_When_Parent_Transmission_Is_Denied()
    {
        // The transmission is unauthorized (legacy attribute not granted), while the attachment's own
        // context check IS granted. Parent-first narrowing must still mask the attachment.
        var transmissionId = NewUuidV7();

        await FlowBuilder.For(Application, ConfigureMainReadAndContextAuthorization)
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(transmission =>
                {
                    transmission.Id = transmissionId;
                    transmission.AuthorizationAttribute = "urn:altinn:resource:restricted";
                    transmission.AddAttachment(attachment =>
                        attachment.AuthorizationContext = ChildContextDto(AuthorizationContextUnauthorizedPresentation.Values.Disabled));
                }))
            .GetEndUserTransmission(transmissionId)
            .ExecuteAndAssert<GetTransmissionDtoEU>(x =>
            {
                x.IsAuthorized.Should().BeFalse();
                x.Attachments.Should().NotBeEmpty();
                x.Attachments.Should().AllSatisfy(a =>
                {
                    a.IsAuthorized.Should().BeFalse();
                    a.Urls.Should().AllSatisfy(url => url.Url.Should().Be(Constants.UnauthorizedUri));
                });
            });
    }

    [Fact]
    public Task Unauthorized_Child_Context_Within_Authorized_Transmission_Should_Only_Affect_The_Child() =>
        FlowBuilder.For(Application, ConfigureMainReadOnlyAuthorization)
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(transmission =>
                {
                    transmission.AuthorizationAttribute = null;
                    transmission.AddAttachment(attachment =>
                        attachment.AuthorizationContext = ChildContextDto(AuthorizationContextUnauthorizedPresentation.Values.Disabled));
                    transmission.AddNavigationalAction(navigationalAction =>
                        navigationalAction.AuthorizationContext = ChildContextDto(AuthorizationContextUnauthorizedPresentation.Values.Excluded));
                }))
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                var transmission = x.Transmissions.Single();
                transmission.IsAuthorized.Should().BeTrue();

                var attachment = transmission.Attachments.Single();
                attachment.IsAuthorized.Should().BeFalse();
                attachment.Urls.Should().AllSatisfy(url => url.Url.Should().Be(Constants.UnauthorizedUri));

                // The excluded navigational action leaves the list, and is reported beside it
                transmission.NavigationalActions.Should().BeEmpty();
                transmission.ExcludedNavigationalActions.Should().ContainSingle()
                    .Subject.Id.Should().NotBeEmpty();

                // ... which is per collection: the attachment above was disabled, not excluded
                transmission.ExcludedAttachments.Should().BeNull();
            });

    [Fact]
    public Task Dialog_Token_Should_List_Authorized_Context_Entities() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.AddGuiAction(guiAction =>
                {
                    guiAction.Action = null;
                    guiAction.AuthorizationContext = ContextDto(AuthorizationContextUnauthorizedPresentation.Values.Disabled);
                });
                // Legacy gui action without a context is governed by the action claim, not the entity list
                x.AddGuiAction(guiAction => guiAction.Priority = DialogGuiActionPriority.Values.Secondary);
                x.AddAttachment(attachment =>
                    attachment.AuthorizationContext = ChildContextDto(AuthorizationContextUnauthorizedPresentation.Values.Disabled));
                x.AddTransmission(transmission =>
                {
                    transmission.AuthorizationAttribute = null;
                    transmission.AuthorizationContext = ContextDto(AuthorizationContextUnauthorizedPresentation.Values.Disabled, action: null);
                    transmission.AddAttachment(attachment =>
                        attachment.AuthorizationContext = ChildContextDto(AuthorizationContextUnauthorizedPresentation.Values.Disabled));
                    transmission.AddNavigationalAction(navigationalAction =>
                        navigationalAction.AuthorizationContext = ChildContextDto(AuthorizationContextUnauthorizedPresentation.Values.Disabled));
                });
            })
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                GetTokenHeader(x.DialogToken!).GetProperty("typ").GetString().Should().Be(DialogTokenTypes.DialogToken);

                var contextGuiAction = x.GuiActions.Single(g => g.Action == "read");
                contextGuiAction.IsAuthorized.Should().BeTrue();
                var legacyGuiAction = x.GuiActions.Single(g => g.Action != "read");
                legacyGuiAction.IsAuthorized.Should().BeTrue();
                var transmission = x.Transmissions.Single();
                // The "dp-excluded" rollback sentinel is persisted on the transmission but never echoed
                transmission.AuthorizationAttribute.Should().BeNull();

                // Every authorized context-carrying entity, in document order, by id; nothing else
                var payload = GetTokenPayload(x.DialogToken!);
                var authorizedEntities = payload.GetProperty(DialogTokenClaimTypes.AuthorizedEntities).EnumerateArray()
                    .Select(e => e.GetString())
                    .ToList();
                authorizedEntities.Should().HaveCount(5);
                authorizedEntities.Take(4).Should().Equal(
                    contextGuiAction.Id.ToString(),
                    x.Attachments.Single().Id.ToString(),
                    transmission.Id.ToString(),
                    transmission.Attachments.Single().Id.ToString());
                // Navigational actions expose no id on the wire, so theirs is only recognizable via "tokenRef"
                Guid.TryParse(authorizedEntities[4], out _).Should().BeTrue();
                payload.GetProperty(DialogTokenClaimTypes.DialogId).GetGuid().Should().Be(x.Id);
            });

    [Fact]
    public Task Dialog_Token_Should_List_TokenRef_Instead_Of_Entity_Id_When_Supplied() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.AddGuiAction(guiAction =>
                {
                    guiAction.Action = null;
                    guiAction.AuthorizationContext = ContextDto(AuthorizationContextUnauthorizedPresentation.Values.Disabled);
                    guiAction.AuthorizationContext.TokenRef = "my-own-reference";
                });
                x.AddAttachment(attachment =>
                    attachment.AuthorizationContext = ChildContextDto(AuthorizationContextUnauthorizedPresentation.Values.Disabled));
                x.AddTransmission(transmission =>
                {
                    transmission.AuthorizationAttribute = null;
                    transmission.AddNavigationalAction(navigationalAction =>
                    {
                        navigationalAction.AuthorizationContext = ChildContextDto(AuthorizationContextUnauthorizedPresentation.Values.Disabled);
                        navigationalAction.AuthorizationContext.TokenRef = "nav-action-reference";
                    });
                });
            })
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                var payload = GetTokenPayload(x.DialogToken!);
                payload.GetProperty(DialogTokenClaimTypes.AuthorizedEntities).EnumerateArray()
                    .Select(e => e.GetString())
                    .Should().Equal("my-own-reference", x.Attachments.Single().Id.ToString(), "nav-action-reference");
            });

    [Fact]
    public Task Dialog_Token_Should_Not_List_Unauthorized_Context_Entities() =>
        FlowBuilder.For(Application, ConfigureMainReadAndContextAuthorization)
            .CreateSimpleDialog((x, _) =>
            {
                // Denied: the context asks for "sign", which is not among the authorized checks
                x.AddGuiAction(guiAction =>
                {
                    guiAction.Action = null;
                    guiAction.AuthorizationContext = ContextDto(AuthorizationContextUnauthorizedPresentation.Values.Disabled, action: "sign");
                });
                // Denied transmission whose children would be authorized on their own: parent-first narrowing
                // must keep them out of the list too, or the token would grant access past the denied parent.
                x.AddTransmission(transmission =>
                {
                    transmission.AuthorizationAttribute = null;
                    transmission.AuthorizationContext = ContextDto(AuthorizationContextUnauthorizedPresentation.Values.Disabled, action: "sign");
                    transmission.AddAttachment(attachment =>
                        attachment.AuthorizationContext = ChildContextDto(AuthorizationContextUnauthorizedPresentation.Values.Disabled));
                });
                // Authorized: read on the context resource is among the authorized checks
                x.AddAttachment(attachment =>
                    attachment.AuthorizationContext = ChildContextDto(AuthorizationContextUnauthorizedPresentation.Values.Disabled));
            })
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                x.GuiActions.Single().IsAuthorized.Should().BeFalse();
                x.Transmissions.Single().IsAuthorized.Should().BeFalse();
                x.Transmissions.Single().Attachments.Single().IsAuthorized.Should().BeFalse();
                x.Attachments.Single().IsAuthorized.Should().BeTrue();

                GetTokenPayload(x.DialogToken!).GetProperty(DialogTokenClaimTypes.AuthorizedEntities).EnumerateArray()
                    .Select(e => e.GetString())
                    .Should().Equal(x.Attachments.Single().Id.ToString());
            });

    [Fact]
    public Task Dialog_Token_Should_Omit_Authorized_Entities_Claim_When_No_Context_Entity_Is_Authorized() =>
        FlowBuilder.For(Application, ConfigureMainReadOnlyAuthorization)
            .CreateSimpleDialog((x, _) =>
            {
                x.AddGuiAction(guiAction =>
                {
                    guiAction.Action = null;
                    guiAction.AuthorizationContext = ContextDto(AuthorizationContextUnauthorizedPresentation.Values.Disabled);
                });
                x.AddGuiAction(guiAction => guiAction.Priority = DialogGuiActionPriority.Values.Secondary);
            })
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                x.GuiActions.Single(g => g.Action == "read").IsAuthorized.Should().BeFalse();

                var payload = GetTokenPayload(x.DialogToken!);
                payload.TryGetProperty(DialogTokenClaimTypes.AuthorizedEntities, out _).Should().BeFalse(
                    "a token without context grants keeps the pre-existing claim set");
                payload.GetProperty(DialogTokenClaimTypes.Actions).GetString().Should().Be("read",
                    "only the main-resource read grant is authorized here");
            });

    private static JsonElement GetTokenHeader(string token) => DecodeTokenPart(token, 0);

    private static JsonElement GetTokenPayload(string token) => DecodeTokenPart(token, 1);

    private static JsonElement DecodeTokenPart(string token, int index) =>
        JsonSerializer.Deserialize<JsonElement>(Base64Url.Decode(token.Split('.')[index]));
}
