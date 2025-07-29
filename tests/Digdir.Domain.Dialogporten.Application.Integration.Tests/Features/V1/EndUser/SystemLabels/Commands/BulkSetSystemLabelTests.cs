using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Commands.BulkSetSystemLabels;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Parties;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;
using SearchDialogDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search.DialogDto;

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
                AddLabels = [SystemLabel.Values.Bin]
            })
            .SendCommand(_ => GetDialog(dialogId1))
            .AssertResult<DialogDto>(x => x.EndUserContext.SystemLabels.FirstOrDefault().Should().Be(SystemLabel.Values.Bin))
            .SendCommand(_ => GetDialog(dialogId2))
            .ExecuteAndAssert<DialogDto>(x => x.EndUserContext.SystemLabels.FirstOrDefault().Should().Be(SystemLabel.Values.Bin));
    }

    [Fact]
    public async Task BulkSet_Updates_System_Labels_With_Revisions()
    {
        var enduserId = NorwegianPersonIdentifier.PrefixWithSeparator + "22834498646";
        Guid? dialogId1 = NewUuidV7();
        Guid? revision1 = null;

        Guid? dialogId2 = NewUuidV7();
        Guid? revision2 = null;

        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x => (x.Dto.Party, x.Dto.Id) = (enduserId, dialogId1))
            .CreateSimpleDialog(x => (x.Dto.Party, x.Dto.Id) = (enduserId, dialogId2))
            .SearchEndUserDialogs(x => x.Party = [enduserId])
            .AssertResult<PaginatedList<SearchDialogDto>>(x =>
            {
                var dialog1 = x.Items.Single(d => d.Id == dialogId1);
                var dialog2 = x.Items.Single(d => d.Id == dialogId2);
                revision1 = dialog1.EndUserContext.Revision;
                revision2 = dialog2.EndUserContext.Revision;
            })
            .SendCommand(_ => new BulkSetSystemLabelCommand
            {
                Dto = new BulkSetSystemLabelDto
                {
                    Dialogs =
                    [
                        new DialogRevisionDto { DialogId = dialogId1.Value, EndUserContextRevision = revision1!.Value },
                        new DialogRevisionDto { DialogId = dialogId2.Value, EndUserContextRevision = revision2!.Value }
                    ],
                    AddLabels = [SystemLabel.Values.Bin]
                }
            })
            .SendCommand(_ => GetDialog(dialogId1))
            .AssertResult<DialogDto>(x => x.EndUserContext.SystemLabels.FirstOrDefault().Should().Be(SystemLabel.Values.Bin))
            .SendCommand(_ => GetDialog(dialogId2))
            .ExecuteAndAssert<DialogDto>(x => x.EndUserContext.SystemLabels.FirstOrDefault().Should().Be(SystemLabel.Values.Bin));
    }

    [Fact]
    public Task BulkSet_Returns_NotFound_For_Invalid_Id() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .BulkSetSystemLabelEndUser((x, ctx) => x.Dto = new()
            {
                Dialogs =
                [
                    new() { DialogId = ctx.GetDialogId() },
                    new() { DialogId = NewUuidV7() }
                ],
                AddLabels = [SystemLabel.Values.Bin]
            })
            .ExecuteAndAssert<EntityNotFound<DialogEntity>>(x =>
                x.Message.Should().NotBeEmpty());

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
                        EndUserContextRevision = Guid.NewGuid()
                    }
                ],
                AddLabels = [SystemLabel.Values.Bin]
            })
            .ExecuteAndAssert<ConcurrencyError>();

    [Fact]
    public Task Bulk_Remove_Bin_Label_Should_Reset_To_Default_SystemLabel() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.SystemLabel = SystemLabel.Values.Bin)
            .BulkSetSystemLabelEndUser((x, ctx) => x.Dto = new()
            {
                Dialogs = [new() { DialogId = ctx.GetDialogId() }],
                RemoveLabels = [SystemLabel.Values.Bin]
            })
            .SendCommand(ctx => GetDialog(ctx.GetDialogId()))
            .ExecuteAndAssert<DialogDto>(x =>
                x.EndUserContext.SystemLabels.FirstOrDefault().Should().Be(SystemLabel.Values.Default));

    private static GetDialogQuery GetDialog(Guid? id) => new() { DialogId = id!.Value };
}
