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
    public Task Authorized_Context_Entities_Should_Get_Context_Tokens() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.AddGuiAction(guiAction =>
                {
                    guiAction.Action = null;
                    guiAction.AuthorizationContext = ContextDto(AuthorizationContextUnauthorizedPresentation.Values.Disabled);
                });
                // Legacy gui action without a context must not get a context token
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
                // The dialog token itself is typed and carries no context grants
                GetTokenHeader(x.DialogToken!).GetProperty("typ").GetString().Should().Be(DialogTokenTypes.DialogToken);

                var contextGuiAction = x.GuiActions.Single(g => g.Action == "read");
                contextGuiAction.IsAuthorized.Should().BeTrue();
                contextGuiAction.ContextToken.Should().NotBeNullOrEmpty();

                var legacyGuiAction = x.GuiActions.Single(g => g.Action != "read");
                legacyGuiAction.IsAuthorized.Should().BeTrue();
                legacyGuiAction.ContextToken.Should().BeNull();

                x.Attachments.Single().ContextToken.Should().NotBeNullOrEmpty();

                var transmission = x.Transmissions.Single();
                transmission.ContextToken.Should().NotBeNullOrEmpty();
                // The "dp-excluded" rollback sentinel is persisted on the transmission but never echoed
                transmission.AuthorizationAttribute.Should().BeNull();
                transmission.Attachments.Single().ContextToken.Should().NotBeNullOrEmpty();
                transmission.NavigationalActions.Single().ContextToken.Should().NotBeNullOrEmpty();

                // The context token asserts exactly the entity, the grant and the permitted parties
                GetTokenHeader(contextGuiAction.ContextToken!).GetProperty("typ").GetString()
                    .Should().Be(DialogTokenTypes.DialogContextToken);

                var payload = GetTokenPayload(contextGuiAction.ContextToken!);
                payload.GetProperty(DialogTokenClaimTypes.EntityId).GetGuid().Should().Be(contextGuiAction.Id);
                payload.GetProperty(DialogTokenClaimTypes.EntityType).GetString()
                    .Should().Be(DialogContextTokenEntityTypes.GuiAction);
                payload.GetProperty(DialogTokenClaimTypes.Actions).GetString().Should().Be("read");
                payload.GetProperty(DialogTokenClaimTypes.EffectiveResource).GetString().Should().Be(ContextResource);
                payload.GetProperty(DialogTokenClaimTypes.PermittedParties).EnumerateArray()
                    .Select(p => p.GetString()).Should().BeEquivalentTo([OtherParty]);
                payload.GetProperty(DialogTokenClaimTypes.DialogId).GetGuid().Should().Be(x.Id);
            });

    [Fact]
    public Task Unauthorized_Context_Entities_Should_Not_Get_Context_Tokens() =>
        FlowBuilder.For(Application, ConfigureMainReadOnlyAuthorization)
            .CreateSimpleDialog((x, _) =>
            {
                x.AddGuiAction(guiAction =>
                {
                    guiAction.Action = null;
                    guiAction.AuthorizationContext = ContextDto(AuthorizationContextUnauthorizedPresentation.Values.Disabled);
                });
                x.AddTransmission(transmission =>
                {
                    transmission.AuthorizationAttribute = null;
                    transmission.AuthorizationContext = ContextDto(AuthorizationContextUnauthorizedPresentation.Values.Disabled, action: null);
                });
            })
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                var guiAction = x.GuiActions.Single();
                guiAction.IsAuthorized.Should().BeFalse();
                guiAction.ContextToken.Should().BeNull();

                var transmission = x.Transmissions.Single();
                transmission.IsAuthorized.Should().BeFalse();
                transmission.ContextToken.Should().BeNull();
            });

    private static JsonElement GetTokenHeader(string token) => DecodeTokenPart(token, 0);

    private static JsonElement GetTokenPayload(string token) => DecodeTokenPart(token, 1);

    private static JsonElement DecodeTokenPart(string token, int index) =>
        JsonSerializer.Deserialize<JsonElement>(Base64Url.Decode(token.Split('.')[index]));
}
