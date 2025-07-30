using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.EndUserContext.Commands.SetSystemLabels;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using FluentAssertions;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.SystemLabels.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class SetSystemLabelTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public Task Set_Updates_System_Label() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SendCommand((_, ctx) => new SetSystemLabelCommand
            {
                EndUserId = ctx.GetParty(),
                DialogId = ctx.GetDialogId(),
                AddLabels = [SystemLabel.Values.Bin]
            })
            .SendCommand((_, ctx) => GetDialog(ctx.GetDialogId()))
            .ExecuteAndAssert<DialogDto>(x =>
                x.EndUserContext.SystemLabels.FirstOrDefault().Should().Be(SystemLabel.Values.Bin));

    [Fact]
    public Task Set_Returns_ConcurrencyError_On_Revision_Mismatch() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SendCommand((_, ctx) => new SetSystemLabelCommand
            {
                EndUserId = ctx.GetParty(),
                DialogId = ctx.GetDialogId(),
                IfMatchEndUserContextRevision = NewUuidV7(),
                AddLabels = [SystemLabel.Values.Bin]
            })
            .ExecuteAndAssert<ConcurrencyError>();

    [Fact]
    public async Task Set_Succeeds_On_Revision_Match()
    {
        Guid? dialogId = NewUuidV7();
        string? party = null;
        Guid? revision = null;

        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.Id = dialogId)
            .GetServiceOwnerDialog()
            .AssertResult<DialogDto>(x =>
            {
                party = x.Party;
                revision = x.EndUserContext.Revision;
            })
            .SendCommand(_ => new SetSystemLabelCommand
            {
                EndUserId = party!,
                DialogId = dialogId.Value,
                IfMatchEndUserContextRevision = revision!.Value,
                AddLabels = [SystemLabel.Values.Bin]
            })
            .SendCommand(_ => GetDialog(dialogId))
            .ExecuteAndAssert<DialogDto>(x =>
                x.EndUserContext.SystemLabels.FirstOrDefault().Should().Be(SystemLabel.Values.Bin));
    }

    private static GetDialogQuery GetDialog(Guid? id) => new() { DialogId = id!.Value };
}
