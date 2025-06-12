using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.SystemLabels;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.DialogSystemLabels.Commands.BulkSet;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using FluentAssertions;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.SystemLabels.Commands;

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
            .BulkSetSystemLabelServiceOwner((x, ctx) =>
            {
                x.EnduserId = ctx.GetParty();
                x.Dto = new()
                {
                    Dialogs =
                    [
                        new() { DialogId = dialogId1.Value },
                        new() { DialogId = dialogId2.Value }
                    ],
                    SystemLabels = [SystemLabel.Values.Bin]
                };
            })
            .SendCommand(GetDialog(dialogId1))
            .AssertResult<DialogDto>(x =>
                x.SystemLabel.Should().Be(SystemLabel.Values.Bin))
            .SendCommand(GetDialog(dialogId2))
            .ExecuteAndAssert<DialogDto>(x =>
                x.SystemLabel.Should().Be(SystemLabel.Values.Bin));
    }

    [Fact]
    public Task BulkSet_Returns_Forbidden_For_Invalid_Id() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .BulkSetSystemLabelServiceOwner((x, ctx) =>
            {
                x.EnduserId = ctx.GetParty();
                x.Dto = new()
                {
                    Dialogs =
                    [
                        new() { DialogId = ctx.GetDialogId() },
                        new() { DialogId = NewUuidV7() }
                    ],
                    SystemLabels = [SystemLabel.Values.Bin]
                };
            })
            .ExecuteAndAssert<Forbidden>(x =>
                x.Reasons.Should().NotBeEmpty());

    [Fact]
    public Task BulkSet_Returns_ConcurrencyError_On_Revision_Mismatch() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .BulkSetSystemLabelServiceOwner((x, ctx) =>
            {
                x.EnduserId = ctx.GetParty();
                x.Dto = new()
                {
                    Dialogs =
                    [
                        new DialogRevisionDto
                        {
                            DialogId = ctx.GetDialogId(),
                            EnduserContextRevision = NewUuidV7()
                        }
                    ],
                    SystemLabels = [SystemLabel.Values.Bin]
                };
            })
            .ExecuteAndAssert<ConcurrencyError>();

    [Fact]
    public async Task BulkSet_Updates_System_Labels_With_Revisions()
    {
        string? enduserId = null;
        Guid? dialogId1 = NewUuidV7();
        Guid? revision1 = null;

        Guid? dialogId2 = NewUuidV7();
        Guid? revision2 = null;

        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.Id = dialogId1)
            .CreateSimpleDialog(x => x.Dto.Id = dialogId2)
            .SendCommand(GetDialog(dialogId1))
            .AssertResult<DialogDto>(x =>
            {
                enduserId = x.Party;
                revision1 = x.EnduserContextRevision;
            })
            .SendCommand(GetDialog(dialogId2))
            .ExecuteAndAssert<DialogDto>(x => revision2 = x.EnduserContextRevision);

        await FlowBuilder.For(Application)
            .SendCommand(new BulkSetSystemLabelCommand
            {
                EnduserId = enduserId!,
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
            .SendCommand(GetDialog(dialogId1))
            .AssertResult<DialogDto>(x => x.SystemLabel.Should().Be(SystemLabel.Values.Bin))
            .SendCommand(GetDialog(dialogId2))
            .ExecuteAndAssert<DialogDto>(x => x.SystemLabel.Should().Be(SystemLabel.Values.Bin));
    }
    // [Fact]
    // public async Task BulkSet_Updates_System_Labels_With_Revisions()
    // {
    //     var cmd1 = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
    //     var cmd2 = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
    //     var res1 = await Application.Send(cmd1);
    //     var res2 = await Application.Send(cmd2);
    //
    //     var dialog1 = await Application.Send(new SearchDialogQuery
    //     {
    //         Party = [cmd1.Dto.Party]
    //     });
    //     dialog1.TryPickT0(out var result1, out _).Should().BeTrue();
    //     var rev1 = result1.Items.Single(x => x.Id == res1.AsT0.DialogId).EnduserContextRevision;
    //
    //     var dialog2 = await Application.Send(new SearchDialogQuery
    //     {
    //         Party = [cmd2.Dto.Party]
    //     });
    //     dialog2.TryPickT0(out var result2, out _).Should().BeTrue();
    //     var rev2 = result2.Items.Single(x => x.Id == res2.AsT0.DialogId).EnduserContextRevision;
    //
    //     var command = new BulkSetSystemLabelCommand
    //     {
    //         EnduserId = cmd1.Dto.Party,
    //         Dto = new BulkSetSystemLabelDto
    //         {
    //             Dialogs =
    //             [
    //                 new DialogRevisionDto { DialogId = res1.AsT0.DialogId, EnduserContextRevision = rev1 },
    //                 new DialogRevisionDto { DialogId = res2.AsT0.DialogId, EnduserContextRevision = rev2 }
    //             ],
    //             SystemLabels = [SystemLabel.Values.Bin]
    //         }
    //     };
    //
    //     var result = await Application.Send(command);
    //     result.TryPickT0(out _, out _).Should().BeTrue();
    //
    //     var get1 = await Application.Send(new GetDialogQuery { DialogId = res1.AsT0.DialogId });
    //     get1.AsT0.SystemLabel.Should().Be(SystemLabel.Values.Bin);
    //     var get2 = await Application.Send(new GetDialogQuery { DialogId = res2.AsT0.DialogId });
    //     get2.AsT0.SystemLabel.Should().Be(SystemLabel.Values.Bin);
    // }

    private static GetDialogQuery GetDialog(Guid? id) => new() { DialogId = id!.Value };

}
