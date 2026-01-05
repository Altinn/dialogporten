using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.EndUserContext.Commands.BulkSetSystemLabels;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Parties;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;
using SearchDialogDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search.DialogDto;

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
                x.EndUserId = ctx.GetParty();
                x.Dto = new()
                {
                    Dialogs =
                    [
                        new() { DialogId = dialogId1.Value },
                        new() { DialogId = dialogId2.Value }
                    ],
                    AddLabels = [SystemLabel.Values.Bin]
                };
            })
            .SendCommand(_ => GetDialog(dialogId1))
            .AssertResult<DialogDto>(x =>
                AssertOneLabelWithValue(x, SystemLabel.Values.Bin))
            .SendCommand(_ => GetDialog(dialogId2))
            .ExecuteAndAssert<DialogDto>(x =>
                AssertOneLabelWithValue(x, SystemLabel.Values.Bin));
    }

    [Fact]
    public Task BulkSet_Returns_Forbidden_For_Invalid_Id() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .BulkSetSystemLabelServiceOwner((x, ctx) =>
            {
                x.EndUserId = ctx.GetParty();
                x.Dto = new()
                {
                    Dialogs =
                    [
                        new() { DialogId = ctx.GetDialogId() },
                        new() { DialogId = NewUuidV7() }
                    ],
                    AddLabels = [SystemLabel.Values.Bin]
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
                x.EndUserId = ctx.GetParty();
                x.Dto = new()
                {
                    Dialogs =
                    [
                        new DialogRevisionDto
                        {
                            DialogId = ctx.GetDialogId(),
                            EndUserContextRevision = NewUuidV7()
                        }
                    ],
                    AddLabels = [SystemLabel.Values.Bin]
                };
            })
            .ExecuteAndAssert<ConcurrencyError>();

    [Fact]
    public async Task BulkSet_Updates_System_Labels_With_Revisions()
    {
        var enduserId = NorwegianPersonIdentifier.PrefixWithSeparator + "22834498646";
        var dialogId1 = NewUuidV7();
        Guid? revision1 = null;

        var dialogId2 = NewUuidV7();
        Guid? revision2 = null;

        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x => (x.Dto.Party, x.Dto.Id) = (enduserId, dialogId1))
            .CreateSimpleDialog(x => (x.Dto.Party, x.Dto.Id) = (enduserId, dialogId2))
            .SearchServiceOwnerDialogs(x => x.Party = [enduserId])
            .AssertResult<PaginatedList<SearchDialogDto>>(x =>
            {
                var dialog1 = x.Items.Single(d => d.Id == dialogId1);
                var dialog2 = x.Items.Single(d => d.Id == dialogId2);
                revision1 = dialog1.EndUserContext.Revision;
                revision2 = dialog2.EndUserContext.Revision;
            })
            .SendCommand(_ => new BulkSetSystemLabelCommand
            {
                EndUserId = enduserId,
                Dto = new BulkSetSystemLabelDto
                {
                    Dialogs =
                    [
                        new DialogRevisionDto { DialogId = dialogId1, EndUserContextRevision = revision1!.Value },
                        new DialogRevisionDto { DialogId = dialogId2, EndUserContextRevision = revision2!.Value }
                    ],
                    AddLabels = [SystemLabel.Values.Bin]
                }
            })
            .AssertResult<BulkSetSystemLabelSuccess>()
            .SendCommand(_ => GetDialog(dialogId1))
            .AssertResult<DialogDto>(x => AssertOneLabelWithValue(x, SystemLabel.Values.Bin))
            .SendCommand(_ => GetDialog(dialogId2))
            .ExecuteAndAssert<DialogDto>(x => AssertOneLabelWithValue(x, SystemLabel.Values.Bin));
    }

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


    [Fact]
    public Task Cannot_BulkSet_Sent_System_Label() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .BulkSetSystemLabelServiceOwner((x, ctx) =>
            {
                x.EndUserId = ctx.GetParty();
                x.Dto = new()
                {
                    Dialogs = [new() { DialogId = ctx.GetDialogId() }],
                    AddLabels = [SystemLabel.Values.Sent]
                };
            })
            .ExecuteAndAssert<ValidationError>(x =>
                x.ShouldHaveErrorWithText(
                    ValidationErrorStrings.SentLabelNotAllowed));

    [Fact]
    public Task Cannot_Bulk_Remove_Existing_Sent_System_Label() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
                x.AddTransmission(x =>
                    x.Type = DialogTransmissionType.Values.Submission))
            .BulkSetSystemLabelServiceOwner((x, ctx) =>
            {
                x.EndUserId = ctx.GetParty();
                x.Dto = new()
                {
                    Dialogs = [new() { DialogId = ctx.GetDialogId() }],
                    RemoveLabels = [SystemLabel.Values.Sent]
                };
            })
            .ExecuteAndAssert<ValidationError>(x =>
                x.ShouldHaveErrorWithText(
                    ValidationErrorStrings.SentLabelNotAllowed));

    [Fact]
    public Task BulkSet_Allows_PerformedBy_For_Admin() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .ConfigureServices(x => x.Decorate<IUserResourceRegistry, AdminUserResourceRegistryDecorator>())
            .BulkSetSystemLabelServiceOwner((command, ctx) =>
            {
                command.EndUserId = null;
                command.Dto = new BulkSetSystemLabelDto
                {
                    Dialogs = [new() { DialogId = ctx.GetDialogId() }],
                    AddLabels = [SystemLabel.Values.Archive],
                    PerformedBy = new ActorDto
                    {
                        ActorType = ActorType.Values.PartyRepresentative,
                        ActorId = "urn:altinn:organization:identifier-no:991825827"
                    }
                };
            })
            .SendCommand((_, ctx) => GetDialog(ctx.GetDialogId()))
            .ExecuteAndAssert<DialogDto>(x =>
                x.EndUserContext.SystemLabels.Should().ContainSingle(label => label == SystemLabel.Values.Archive));

    [Fact]
    public Task BulkSet_PerformedBy_For_Non_Admin_Is_Forbidden() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .ConfigureServices(x => x.Decorate<IUserResourceRegistry, NonAdminUserResourceRegistryDecorator>())
            .BulkSetSystemLabelServiceOwner((command, ctx) =>
            {
                command.EndUserId = null;
                command.Dto = new BulkSetSystemLabelDto
                {
                    Dialogs = [new() { DialogId = ctx.GetDialogId() }],
                    AddLabels = [SystemLabel.Values.Archive],
                    PerformedBy = new ActorDto
                    {
                        ActorType = ActorType.Values.PartyRepresentative,
                        ActorId = "urn:altinn:organization:identifier-no:991825827"
                    }
                };
            })
            .ExecuteAndAssert<Forbidden>();

    private static GetDialogQuery GetDialog(Guid? id) => new() { DialogId = id!.Value };

    private static void AssertOneLabelWithValue(DialogDto dialog, SystemLabel.Values value)
    {
        dialog.EndUserContext.SystemLabels.Should().HaveCount(1);
        dialog.EndUserContext.SystemLabels.First().Should().Be(value);
    }
}
