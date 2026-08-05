using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Commands.SetSystemLabel;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Queries.SearchLabelAssignmentLog;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using NSubstitute;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;
using static Digdir.Domain.Dialogporten.Infrastructure.Altinn.NameRegistry.IPartyNameRegistryTransport;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.SystemLabels.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class SetSystemLabelTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public Task Create_Sets_Default_System_Labels_Mask_On_Dialog() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .Do(async (_, ctx) =>
            {
                var dialogs = await Application.GetDbEntities<DialogEntity>();
                dialogs.Should().ContainSingle(x =>
                    x.Id == ctx.GetDialogId() &&
                    x.SystemLabelsMask == 1);
            })
            .ExecuteAsync();

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
            .CreateSimpleDialog((x, _) => x.Dto.Id = dialogId)
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
            .CreateSimpleDialog((x, _) => x.Dto.Id = dialogId)
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

        var dialogs = await Application.GetDbEntities<DialogEntity>();
        dialogs.Should().ContainSingle(x =>
            x.Id == dialogId &&
            x.SystemLabelsMask == 1);
    }

    [Fact]
    public async Task Set_Updates_Dialog_System_Labels_Mask()
    {
        var dialogId = NewUuidV7();

        await FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.Dto.Id = dialogId)
            .SetSystemLabelsEndUser(x =>
                x.AddLabels = [SystemLabel.Values.MarkedAsUnopened])
            .ExecuteAndAssert<SetSystemLabelSuccess>();

        short expectedMask = 1 | (1 << ((int)SystemLabel.Values.MarkedAsUnopened - 1));
        var dialogs = await Application.GetDbEntities<DialogEntity>();

        dialogs.Should().ContainSingle(x =>
            x.Id == dialogId &&
            x.SystemLabelsMask == expectedMask);
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
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(x =>
                    x.Type = DialogTransmissionType.Values.Submission))
            .SetSystemLabelsEndUser(x =>
                x.RemoveLabels = [SystemLabel.Values.Sent])
            .ExecuteAndAssert<ValidationError>(x =>
                x.ShouldHaveErrorWithText(
                    ValidationErrorStrings.SentLabelNotAllowed));

    [Fact]
    public Task Set_Adds_Three_LabelLog_Entries_When_Changing_NonDefault_Label_Twice() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SetSystemLabelsEndUser(x => x.AddLabels = [SystemLabel.Values.Bin])
            .SetSystemLabelsEndUser(x => x.AddLabels = [SystemLabel.Values.Archive])
            .GetLabelAssignmentLogs()
            .ExecuteAndAssert<List<LabelAssignmentLogDto>>(x =>
            {
                var actorNameEntities = Application.GetDbEntities<ActorName>()
                    .GetAwaiter().GetResult();
                actorNameEntities.Should().ContainSingle();

                var actorName = actorNameEntities.Single();
                x.Should().HaveCount(3)
                    .And.AllSatisfy(x =>
                    {
                        x.PerformedBy.Should().NotBeNull();
                        x.PerformedBy.ActorName.Should().Be(actorName.Name);
                    });
            });

    [Fact]
    public Task Set_Adds_LabelLog_Even_When_Party_Name_Registry_Is_Down() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .ConfigurePartyNameRegistry(p =>
            {
                p.QueryPartyName(Arg.Any<NameLookup>(), Arg.Any<CancellationToken>())
                    .Returns(TestPartyNameRegistry.InternalServerError);
            })
            .SetSystemLabelsEndUser(x => x.AddLabels = [SystemLabel.Values.Bin])
            .GetLabelAssignmentLogs()
            .AssertResult<List<LabelAssignmentLogDto>>(x =>
            {
                x.Should().HaveCount(1)
                    .And.AllSatisfy(x =>
                    {
                        x.PerformedBy.Should().NotBeNull();
                        x.PerformedBy.ActorId.Should().StartWith("urn:altinn:person:identifier-ephemeral:");
                        x.PerformedBy.ActorName.Should().BeNull();
                        x.PerformedBy.ActorType.Should().Be(ActorType.Values.PartyRepresentative);
                    });
            })
            .ResetPartyNameRegistry()
            .ConsumeEvents()
            .GetLabelAssignmentLogs()
            .ExecuteAndAssert<List<LabelAssignmentLogDto>>(x =>
            {
                x.Should().HaveCount(1)
                    .And.AllSatisfy(x =>
                    {
                        x.PerformedBy.Should().NotBeNull();
                        x.PerformedBy.ActorId.Should().StartWith("urn:altinn:person:identifier-ephemeral:");
                        x.PerformedBy.ActorName.Should().Be("Brando Sando");
                        x.PerformedBy.ActorType.Should().Be(ActorType.Values.PartyRepresentative);
                    });
            });

    [Fact]
    public async Task Set_As_SystemUser_Records_The_Correct_Assignment_Log()
    {
        await FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .AsSystemUser()
            .SetSystemLabelsEndUser(command =>
            {
                command.AddLabels = [SystemLabel.Values.Archive];
            })
            .SendCommand((_, ctx) => GetDialog(ctx.GetDialogId()))
            .AssertResult<DialogDto>(x =>
                x.EndUserContext.SystemLabels.Should().ContainSingle(label => label == SystemLabel.Values.Archive))
            .GetLabelAssignmentLogs()
            .ExecuteAndAssert<List<LabelAssignmentLogDto>>(x =>
            {
                x.Should().HaveCount(1)
                    .And.AllSatisfy(x =>
                    {
                        x.PerformedBy.Should().NotBeNull();
                        x.PerformedBy.ActorId.Should().Be(TestUsers.DefaultSystemUserUrn);
                        x.PerformedBy.ActorName.Should().Be("Systembruker");
                        x.PerformedBy.ActorType.Should().Be(ActorType.Values.PartyRepresentative);
                    });
            });
    }

    private static GetDialogQuery GetDialog(Guid? id) => new() { DialogId = id!.Value };
}
