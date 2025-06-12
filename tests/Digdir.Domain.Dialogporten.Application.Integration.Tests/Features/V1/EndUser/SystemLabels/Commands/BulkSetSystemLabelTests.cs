using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.SystemLabels;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.DialogSystemLabels.Commands.BulkSet;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;

using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.SystemLabels.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class BulkSetSystemLabelTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public async Task BulkSet_Updates_System_Labels()
    {
        Guid? dialogId1 = NewUuidV7();
        Guid? dialogId2 = NewUuidV7();

        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.Id = dialogId1)
            .CreateSimpleDialog(x => x.Dto.Id = dialogId2)
            .BulkSetSystemLabelEndUser((x, _) => x.Dto = new()
            {
                Dialogs =
                [
                    new() { DialogId = dialogId1.Value },
                    new() { DialogId = dialogId2.Value }
                ],
                SystemLabels = [SystemLabel.Values.Bin]
            })
            .SendCommand(GetDialog(dialogId1))
            .AssertResult<DialogDto>(x => x.SystemLabel.Should().Be(SystemLabel.Values.Bin))
            .SendCommand(GetDialog(dialogId2))
            .ExecuteAndAssert<DialogDto>(x => x.SystemLabel.Should().Be(SystemLabel.Values.Bin));
    }

    [Fact]
    public async Task BulkSet_Updates_System_Labels_With_Revisions()
    {
        Guid? dialogId1 = NewUuidV7();
        Guid? revision1 = null;

        Guid? dialogId2 = NewUuidV7();
        Guid? revision2 = null;

        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.Id = dialogId1)
            .CreateSimpleDialog(x => x.Dto.Id = dialogId2)
            .SendCommand(GetDialog(dialogId1))
            .AssertResult<DialogDto>(x => revision1 = x.EnduserContextRevision)
            .SendCommand(GetDialog(dialogId2))
            .ExecuteAndAssert<DialogDto>(x => revision2 = x.EnduserContextRevision);

        await FlowBuilder.For(Application)
            .SendCommand(new BulkSetSystemLabelCommand
            {
                Dto = new BulkSetSystemLabelDto
                {
                    Dialogs =
                    [
                        new DialogRevisionDto { DialogId = dialogId1.Value, EnduserContextRevision = revision1!.Value },
                        new DialogRevisionDto { DialogId = dialogId2.Value, EnduserContextRevision = revision2!.Value }
                    ],
                    SystemLabels = [SystemLabel.Values.Bin]
                }
            })
            .AssertResult<BulkSetSystemLabelSuccess>()
            .SendCommand(GetDialog(dialogId1))
            .AssertResult<DialogDto>(x => x.SystemLabel.Should().Be(SystemLabel.Values.Bin))
            .SendCommand(GetDialog(dialogId2))
            .ExecuteAndAssert<DialogDto>(x => x.SystemLabel.Should().Be(SystemLabel.Values.Bin));
    }

    [Fact]
    public Task BulkSet_Returns_Forbidden_For_Invalid_Id() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .BulkSetSystemLabelEndUser((x, ctx) => x.Dto = new()
            {
                Dialogs =
                [
                    new() { DialogId = ctx.GetDialogId() },
                    new() { DialogId = NewUuidV7() }
                ],
                SystemLabels = [SystemLabel.Values.Bin]
            })
            .ExecuteAndAssert<Forbidden>(x =>
                x.Reasons.Should().NotBeEmpty());

    [Fact]
    public Task BulkSet_Returns_ConcurrencyError_On_Revision_Mismatch() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .BulkSetSystemLabelEndUser((x, ctx) => x.Dto = new()
            {
                Dialogs =
                [
                    new DialogRevisionDto
                    {
                        DialogId = ctx.GetDialogId(),
                        EnduserContextRevision = Guid.NewGuid()
                    }
                ],
                SystemLabels = [SystemLabel.Values.Bin]
            })
            .ExecuteAndAssert<ConcurrencyError>();

    private static GetDialogQuery GetDialog(Guid? id) => new() { DialogId = id!.Value };
}
