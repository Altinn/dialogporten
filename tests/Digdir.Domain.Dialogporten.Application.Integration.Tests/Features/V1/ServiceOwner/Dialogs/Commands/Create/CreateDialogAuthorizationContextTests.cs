using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.AuthorizationContexts;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;
using AwesomeAssertions;
using DialogDtoSO = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get.DialogDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Commands.Create;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class CreateDialogAuthorizationContextTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    private const string OtherParty = "urn:altinn:organization:identifier-no:991825827";

    private static AuthorizationContextDto CreateContext(Action<AuthorizationContextDto>? modify = null)
    {
        var context = new AuthorizationContextDto
        {
            ServiceResource = "urn:altinn:resource:some-other-service",
            AdditionalResourceAttribute = "urn:altinn:task:Task_1",
            Parties = [OtherParty],
            IncludeDialogParty = true,
            Action = "read",
            UnauthorizedPresentation = AuthorizationContextUnauthorizedPresentation.Values.Redacted
        };
        modify?.Invoke(context);
        return context;
    }

    private static AuthorizationContextDto CreateChildContext(Action<AuthorizationContextDto>? modify = null)
    {
        var context = new AuthorizationContextDto
        {
            AdditionalResourceAttribute = "urn:altinn:subresource:secret",
            Parties = [OtherParty],
            UnauthorizedPresentation = AuthorizationContextUnauthorizedPresentation.Values.Disabled
        };
        modify?.Invoke(context);
        return context;
    }

    [Fact]
    public Task Create_With_AuthorizationContext_On_All_Carriers_Should_RoundTrip() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.AddGuiAction(guiAction =>
                {
                    guiAction.Action = null;
                    guiAction.AuthorizationContext = CreateContext();
                });
                x.AddApiAction(apiAction =>
                {
                    apiAction.Action = null;
                    apiAction.AuthorizationContext = CreateContext(c => c.Action = "write");
                });
                x.AddAttachment(attachment => attachment.AuthorizationContext = CreateChildContext());
                x.AddTransmission(transmission =>
                {
                    transmission.AuthorizationAttribute = null;
                    transmission.AuthorizationContext = CreateContext(c => c.Action = null);
                    transmission.AddAttachment(attachment => attachment.AuthorizationContext = CreateChildContext());
                    transmission.AddNavigationalAction(navigationalAction => navigationalAction.AuthorizationContext = CreateChildContext());
                });
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDtoSO>(x =>
            {
                var guiAction = x.GuiActions.Single();
                guiAction.Action.Should().BeNull();
                guiAction.AuthorizationAttribute.Should().BeNull();
                guiAction.AuthorizationContext.Should().NotBeNull();
                guiAction.AuthorizationContext!.ServiceResource.Should().Be("urn:altinn:resource:some-other-service");
                guiAction.AuthorizationContext.AdditionalResourceAttribute.Should().Be("urn:altinn:task:Task_1");
                guiAction.AuthorizationContext.Parties.Should().BeEquivalentTo([OtherParty]);
                guiAction.AuthorizationContext.IncludeDialogParty.Should().BeTrue();
                guiAction.AuthorizationContext.Action.Should().Be("read");
                guiAction.AuthorizationContext.UnauthorizedPresentation.Should().Be(AuthorizationContextUnauthorizedPresentation.Values.Redacted);

                var apiAction = x.ApiActions.Single();
                apiAction.Action.Should().BeNull();
                apiAction.AuthorizationContext.Should().NotBeNull();
                apiAction.AuthorizationContext!.Action.Should().Be("write");

                var attachment = x.Attachments.Single();
                attachment.AuthorizationContext.Should().NotBeNull();
                attachment.AuthorizationContext!.AdditionalResourceAttribute.Should().Be("urn:altinn:subresource:secret");
                attachment.AuthorizationContext.UnauthorizedPresentation.Should().Be(AuthorizationContextUnauthorizedPresentation.Values.Disabled);

                var transmission = x.Transmissions.Single();
                transmission.AuthorizationAttribute.Should().BeNull();
                transmission.AuthorizationContext.Should().NotBeNull();
                transmission.AuthorizationContext!.Action.Should().BeNull();
                transmission.Attachments.Should().ContainSingle(a => a.AuthorizationContext != null);
                transmission.NavigationalActions.Should().ContainSingle(a => a.AuthorizationContext != null);
            });

    [Fact]
    public Task Create_Should_Fail_When_Transmission_Combines_AuthorizationAttribute_And_Context() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(transmission =>
                {
                    transmission.AuthorizationAttribute = "element1";
                    transmission.AuthorizationContext = CreateContext(c => c.Action = null);
                }))
            .ExecuteAndAssert<ValidationError>(x =>
                x.Errors.Should().Contain(e => e.ErrorMessage.Contains("cannot be combined")));

    [Fact]
    public Task Create_Should_Fail_When_ApiAction_Combines_Action_And_Context() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddApiAction(apiAction => apiAction.AuthorizationContext = CreateContext()))
            .ExecuteAndAssert<ValidationError>(x =>
                x.Errors.Should().Contain(e => e.ErrorMessage.Contains("cannot be combined")));

    [Fact]
    public Task Create_Should_Fail_When_GuiAction_Combines_AuthorizationAttribute_And_Context() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddGuiAction(guiAction =>
                {
                    guiAction.Action = null;
                    guiAction.AuthorizationAttribute = "element1";
                    guiAction.AuthorizationContext = CreateContext();
                }))
            .ExecuteAndAssert<ValidationError>(x =>
                x.Errors.Should().Contain(e => e.ErrorMessage.Contains("cannot be combined")));

    [Fact]
    public Task Create_Should_Succeed_When_ApiAction_Context_Is_Missing_Action() =>
        // The context action is optional everywhere and defaults to "read"
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddApiAction(apiAction =>
                {
                    apiAction.Action = null;
                    apiAction.AuthorizationContext = CreateContext(c => c.Action = null);
                }))
            .ExecuteAndAssert<CreateDialogSuccess>(x => x.DialogId.Should().NotBeEmpty());

    [Fact]
    public Task Create_Should_Fail_When_GuiAction_Has_Neither_Action_Nor_Context() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddGuiAction(guiAction => guiAction.Action = null))
            .ExecuteAndAssert<ValidationError>(x =>
                x.Errors.Should().Contain(e => e.PropertyName.Contains("Action")));

    [Fact]
    public Task Create_Should_Fail_When_UnauthorizedPresentation_Is_Not_Supplied() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(transmission =>
                {
                    transmission.AuthorizationAttribute = null;
                    transmission.AuthorizationContext = CreateContext(c =>
                    {
                        c.Action = null;
                        // Simulates an omitted "unauthorizedPresentation" in the JSON payload
                        c.UnauthorizedPresentation = default;
                    });
                }))
            .ExecuteAndAssert<ValidationError>(x =>
                x.Errors.Should().Contain(e => e.ErrorMessage.Contains("required")));

    [Fact]
    public Task Create_Should_Fail_When_Context_Has_No_Parties_And_No_DialogParty() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(transmission =>
                {
                    transmission.AuthorizationAttribute = null;
                    transmission.AuthorizationContext = CreateContext(c =>
                    {
                        c.Action = null;
                        c.Parties = [];
                        c.IncludeDialogParty = false;
                    });
                }))
            .ExecuteAndAssert<ValidationError>(x =>
                x.Errors.Should().Contain(e => e.ErrorMessage.Contains("at least one party")));

    [Fact]
    public Task Create_Should_Succeed_When_Context_Has_No_Parties_But_Includes_DialogParty() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(transmission =>
                {
                    transmission.AuthorizationAttribute = null;
                    transmission.AuthorizationContext = CreateContext(c =>
                    {
                        c.Action = null;
                        c.Parties = [];
                        c.IncludeDialogParty = true;
                    });
                }))
            .ExecuteAndAssert<CreateDialogSuccess>();

    [Fact]
    public Task Create_Should_Fail_When_Context_Party_Is_Invalid() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(transmission =>
                {
                    transmission.AuthorizationAttribute = null;
                    transmission.AuthorizationContext = CreateContext(c =>
                    {
                        c.Action = null;
                        c.Parties = ["not-a-party"];
                    });
                }))
            .ExecuteAndAssert<ValidationError>();

    [Fact]
    public Task Create_Should_Fail_When_Context_Has_Too_Many_Parties() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(transmission =>
                {
                    transmission.AuthorizationAttribute = null;
                    transmission.AuthorizationContext = CreateContext(c =>
                    {
                        c.Action = null;
                        c.Parties = Enumerable.Range(0, AuthorizationContext.MaxNumberOfParties + 1)
                            .Select(i => $"urn:altinn:organization:identifier-no:{910000000 + i}")
                            .ToList();
                    });
                }))
            .ExecuteAndAssert<ValidationError>(x =>
                x.Errors.Should().Contain(e => e.ErrorMessage.Contains("cannot contain more than")));

    [Fact]
    public Task Create_Should_Fail_When_AdditionalResourceAttribute_Contains_Resource_Reference() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(transmission =>
                {
                    transmission.AuthorizationAttribute = null;
                    transmission.AuthorizationContext = CreateContext(c =>
                    {
                        c.Action = null;
                        c.AdditionalResourceAttribute = "urn:altinn:resource:sneaky-resource";
                    });
                }))
            .ExecuteAndAssert<ValidationError>(x =>
                x.Errors.Should().Contain(e => e.ErrorMessage.Contains("ServiceResource")));

    [Fact]
    public Task Update_Should_Replace_Context_On_GuiAction() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddGuiAction(guiAction =>
                {
                    guiAction.Action = null;
                    guiAction.AuthorizationContext = CreateContext();
                }))
            .UpdateDialog(x =>
            {
                var guiAction = x.Dto.GuiActions.Single();
                guiAction.AuthorizationContext.Should().NotBeNull();
                guiAction.AuthorizationContext!.Parties = [OtherParty, "urn:altinn:organization:identifier-no:310778737"];
                guiAction.AuthorizationContext.UnauthorizedPresentation = AuthorizationContextUnauthorizedPresentation.Values.Disabled;
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDtoSO>(x =>
            {
                var guiAction = x.GuiActions.Single();
                guiAction.AuthorizationContext.Should().NotBeNull();
                guiAction.AuthorizationContext!.Parties.Should().HaveCount(2);
                guiAction.AuthorizationContext.UnauthorizedPresentation.Should().Be(AuthorizationContextUnauthorizedPresentation.Values.Disabled);
            });

    [Fact]
    public Task Update_Should_Remove_Context_And_Allow_Legacy_Fields_Again() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddGuiAction(guiAction =>
                {
                    guiAction.Action = null;
                    guiAction.AuthorizationContext = CreateContext();
                }))
            .UpdateDialog(x =>
            {
                var guiAction = x.Dto.GuiActions.Single();
                guiAction.AuthorizationContext = null;
                guiAction.Action = "read";
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDtoSO>(x =>
            {
                var guiAction = x.GuiActions.Single();
                guiAction.AuthorizationContext.Should().BeNull();
                guiAction.Action.Should().Be("read");
            });
}
