using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.ServiceOwnerContext.Commands.Update;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.ServiceOwnerContext.ServiceOwnerLabels.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.DialogServiceOwnerContexts.Entities;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;
using ServiceOwnerLabelDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.ServiceOwnerContext.Commands.Update.ServiceOwnerLabelDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.ServiceOwnerContext.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class UpdateDialogServiceOwnerLabelTests : ApplicationCollectionFixture
{
    public UpdateDialogServiceOwnerLabelTests(DialogApplication application) : base(application) { }

    [Fact]
    public Task Cannot_Call_Update_ServiceOwnerLabels_Without_DialogId_Or_Dto() =>
        FlowBuilder.For(Application)
            .SendCommand(new UpdateDialogServiceOwnerContextCommand())
            .ExecuteAndAssert<ValidationError>(x =>
            {
                x.ShouldHaveErrorWithText(nameof(UpdateDialogServiceOwnerContextCommand.DialogId));
                x.ShouldHaveErrorWithText(nameof(UpdateDialogServiceOwnerContextCommand.Dto));
            });

    [Fact]
    public async Task Calling_UpdateDialogServiceOwnerContext_With_Invalid_DialogId_Returns_NotFound()
    {
        var invalidDialogId = NewUuidV7();
        await FlowBuilder.For(Application)
            .SendCommand(new UpdateDialogServiceOwnerContextCommand
            {
                DialogId = invalidDialogId,
                Dto = new()
            })
            .ExecuteAndAssert<EntityNotFound<DialogEntity>>(x =>
                x.Message.Should().Contain(invalidDialogId.ToString()));
    }

    [Fact]
    public Task Can_Remove_ServiceOwnerLabels() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
                x.Dto.ServiceOwnerContext!.ServiceOwnerLabels = [new() { Value = "Scadrial" }])
            .UpdateServiceOwnerContext(x => x.Dto = new()
            {
                ServiceOwnerLabels = []
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.ServiceOwnerContext
                    .ServiceOwnerLabels
                    .Count
                    .Should()
                    .Be(0));

    [Fact]
    public Task Can_Add_ServiceOwnerLabel_To_Existing_Dialog() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .UpdateServiceOwnerContext(x => x.Dto = new()
            {
                ServiceOwnerLabels = CreateLabels("Scadrial", "Roshar")
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.ServiceOwnerContext
                    .ServiceOwnerLabels
                    .Count
                    .Should()
                    .Be(2));

    [Fact]
    public Task Cannot_Update_ServiceOwnerLabels_With_Duplicates() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .UpdateServiceOwnerContext(x =>
            {
                const string label = "SCADRIAL";
                x.Dto = new()
                {
                    ServiceOwnerLabels = CreateLabels(label, label.ToLowerInvariant())
                };
            })
            .ExecuteAndAssert<ValidationError>(x =>
                x.ShouldHaveErrorWithText("duplicate"));

    [Fact]
    public Task Cannot_Update_ServiceOwnerLabels_With_Invalid_Length() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .UpdateServiceOwnerContext(x =>
            {
                x.Dto = new()
                {
                    ServiceOwnerLabels = CreateLabels(
                        null,
                        new string('a', Constants.MinSearchStringLength - 1),
                        new string('a', Constants.DefaultMaxStringLength + 1))
                };
            })
            .ExecuteAndAssert<ValidationError>(x =>
            {
                x.ShouldHaveErrorWithText("not be empty");
                x.ShouldHaveErrorWithText("at least");
                x.ShouldHaveErrorWithText("or fewer");
            });

    [Fact]
    public async Task Update_ServiceOwnerLabels_Should_Update_ServiceOwnerContext_Revision()
    {
        Guid? originalRevision = null;
        await FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SendCommand((_, ctx) => new GetServiceOwnerLabelsQuery { DialogId = ctx.GetDialogId() })
            .AssertResult<ServiceOwnerLabelResultDto>(x => originalRevision = x.Revision)
            .SendCommand((_, ctx) => new UpdateDialogServiceOwnerContextCommand
            {
                DialogId = ctx.GetDialogId(),
                Dto = new()
                {
                    ServiceOwnerLabels = [new() { Value = "scadrial" }]
                }
            })
            .SendCommand((_, ctx) => new GetServiceOwnerLabelsQuery { DialogId = ctx.GetDialogId() })
            .ExecuteAndAssert<ServiceOwnerLabelResultDto>(x =>
                x.Revision.Should().NotBe(originalRevision!.Value));
    }

    [Fact]
    public Task Cannot_Update_With_More_Than_Max_Allowed_ServiceOwner_Labels() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .UpdateServiceOwnerContext(x =>
            {
                x.Dto = new()
                {
                    ServiceOwnerLabels = Enumerable.Range(0, DialogServiceOwnerLabel.MaxNumberOfLabels + 1)
                        .Select(i => new ServiceOwnerLabelDto { Value = $"label{i}" })
                        .ToList()
                };
            })
            .ExecuteAndAssert<ValidationError>(x =>
            {
                x.ShouldHaveErrorWithText("Maximum");
                x.ShouldHaveErrorWithText($"{DialogServiceOwnerLabel.MaxNumberOfLabels}");
            });

    private static List<ServiceOwnerLabelDto> CreateLabels(params string?[] values) =>
        values.Select(value => new ServiceOwnerLabelDto { Value = value! }).ToList();
}
