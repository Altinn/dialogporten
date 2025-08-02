using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Commands.SetSystemLabel;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using FluentAssertions;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.SystemLabels.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class SetSystemLabelTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public Task Set_Updates_System_Label() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SetSystemLabelsEndUser(x =>
                x.AddLabels = [SystemLabel.Values.Bin])
            .SendCommand((x, ctx) => GetDialog(ctx.GetDialogId()))
            .ExecuteAndAssert<DialogDto>(x =>
                x.EndUserContext.SystemLabels.FirstOrDefault().Should().Be(SystemLabel.Values.Bin));

    [Fact]
    public Task Set_Returns_ConcurrencyError_On_Revision_Mismatch() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SetSystemLabelsEndUser(x =>
                {
                    x.AddLabels = [SystemLabel.Values.Bin];
                    x.IfMatchEndUserContextRevision = Guid.NewGuid();
                })
            .ExecuteAndAssert<ConcurrencyError>();

    [Fact]
    public async Task Set_Succeeds_On_Revision_Match()
    {
        Guid? dialogId = NewUuidV7();
        Guid? revision = null;

        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.Id = dialogId)
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x => revision = x.EndUserContext.Revision);

        await FlowBuilder.For(Application)
            .SendCommand(_ => new SetSystemLabelCommand
            {
                DialogId = dialogId.Value,
                IfMatchEndUserContextRevision = revision!.Value,
                AddLabels = [SystemLabel.Values.Bin]
            })
            .SendCommand(_ => GetDialog(dialogId))
            .ExecuteAndAssert<DialogDto>(x =>
                x.EndUserContext.SystemLabels.FirstOrDefault().Should().Be(SystemLabel.Values.Bin));
    }

    [Fact]
    public async Task Can_Set_And_Remove_MarkedAsUnopened_Label()
    {
        var dialogId = NewUuidV7();
        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.Id = dialogId)
            .SetSystemLabelsEndUser(x =>
                x.AddLabels = [SystemLabel.Values.MarkedAsUnopened])
            .ExecuteAndAssert<SetSystemLabelSuccess>();

        var dialogSystemLabels = await Application
            .GetDbEntities<DialogEndUserContextSystemLabel>();

        dialogSystemLabels.Should().ContainSingle(x => x.SystemLabelId == SystemLabel.Values.MarkedAsUnopened);
        dialogSystemLabels.Should().ContainSingle(x => x.SystemLabelId == SystemLabel.Values.Default);

        await FlowBuilder.For(Application)
            .SendCommand(_ => new SetSystemLabelCommand
            {
                RemoveLabels = [SystemLabel.Values.MarkedAsUnopened],
                DialogId = dialogId
            })
            .ExecuteAndAssert<SetSystemLabelSuccess>();

        dialogSystemLabels = await Application
            .GetDbEntities<DialogEndUserContextSystemLabel>();

        dialogSystemLabels.Should().NotContain(x => x.SystemLabelId == SystemLabel.Values.MarkedAsUnopened);
        dialogSystemLabels.Should().ContainSingle(x => x.SystemLabelId == SystemLabel.Values.Default);
    }

    [Fact]
    public Task Cannot_Set_Sent_System_Label() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SetSystemLabelsEndUser(x =>
                x.AddLabels = [SystemLabel.Values.Sent])
            .ExecuteAndAssert<ValidationError>(x =>
                x.ShouldHaveErrorWithText(
                    ValidationErrorStrings.SentLabelNotAllowed));

    [Fact]
    public Task Cannot_Remove_Existing_Sent_System_Label() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
                x.AddTransmission(x =>
                    x.Type = DialogTransmissionType.Values.Submission))
            .SetSystemLabelsEndUser(x =>
                x.RemoveLabels = [SystemLabel.Values.Sent])
            .ExecuteAndAssert<ValidationError>(x =>
                x.ShouldHaveErrorWithText(
                    ValidationErrorStrings.SentLabelNotAllowed));

    private static GetDialogQuery GetDialog(Guid? id) => new() { DialogId = id!.Value };
}
