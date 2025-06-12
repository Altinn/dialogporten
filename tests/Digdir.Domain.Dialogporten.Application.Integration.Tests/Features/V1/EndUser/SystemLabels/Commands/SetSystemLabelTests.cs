using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.DialogSystemLabels.Commands.Set;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
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
            .SendCommand((_, ctx) => new SetSystemLabelCommand
            {
                DialogId = ctx.GetDialogId(),
                SystemLabels = [SystemLabel.Values.Bin]
            })
            .SendCommand((x, ctx) => GetDialog(ctx.GetDialogId()))
            .ExecuteAndAssert<DialogDto>(x => x.SystemLabel.Should().Be(SystemLabel.Values.Bin));

    [Fact]
    public Task Set_Returns_ConcurrencyError_On_Revision_Mismatch() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SendCommand((_, ctx) => new SetSystemLabelCommand
            {
                DialogId = ctx.GetDialogId(),
                IfMatchEnduserContextRevision = Guid.NewGuid(),
                SystemLabels = [SystemLabel.Values.Bin]
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
            .ExecuteAndAssert<DialogDto>(x => revision = x.EnduserContextRevision);

        await FlowBuilder.For(Application)
            .SendCommand(new SetSystemLabelCommand
            {
                DialogId = dialogId.Value,
                IfMatchEnduserContextRevision = revision!.Value,
                SystemLabels = [SystemLabel.Values.Bin]
            })
            .SendCommand(GetDialog(dialogId))
            .ExecuteAndAssert<DialogDto>(x =>
                x.SystemLabel.Should().Be(SystemLabel.Values.Bin));
    }

    private static GetDialogQuery GetDialog(Guid? id) => new() { DialogId = id!.Value };
}
