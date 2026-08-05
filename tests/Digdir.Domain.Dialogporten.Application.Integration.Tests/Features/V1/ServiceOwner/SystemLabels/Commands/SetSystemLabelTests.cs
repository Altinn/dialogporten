using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Queries.SearchLabelAssignmentLog;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.EndUserContext.Commands.SetSystemLabels;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using NSubstitute;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;
using static Digdir.Domain.Dialogporten.Infrastructure.Altinn.NameRegistry.IPartyNameRegistryTransport;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.SystemLabels.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class SetSystemLabelTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    private const string AdminPerformedByActorId = "urn:altinn:organization:identifier-no:991825827";

    [Fact]
    public Task Set_Updates_System_Label() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SetSystemLabelsServiceOwner(x => x.AddLabels = [SystemLabel.Values.Bin])
            .SendCommand((_, ctx) => GetDialog(ctx.GetDialogId()))
            .ExecuteAndAssert<DialogDto>(x =>
                x.EndUserContext.SystemLabels.FirstOrDefault().Should().Be(SystemLabel.Values.Bin));

    [Fact]
    public Task Set_Returns_ConcurrencyError_On_Revision_Mismatch() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SetSystemLabelsServiceOwner(x =>
            {
                x.IfMatchEndUserContextRevision = NewUuidV7();
                x.AddLabels = [SystemLabel.Values.Bin];
            })
            .ExecuteAndAssert<ConcurrencyError>();

    [Fact]
    public async Task Set_Succeeds_On_Revision_Match()
    {
        Guid? dialogId = NewUuidV7();
        string? party = null;
        Guid? revision = null;

        await FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.Dto.Id = dialogId)
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

    [Fact]
    public Task Can_Set_And_Remove_MarkedAsUnopened_Label() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SetSystemLabelsServiceOwner(x =>
                x.AddLabels = [SystemLabel.Values.MarkedAsUnopened])
            .SendCommand((_, ctx) => GetDialog(ctx.GetDialogId()))
            .AssertResult<DialogDto>(x =>
            {
                x.EndUserContext.SystemLabels.Should().ContainSingle(x => x == SystemLabel.Values.MarkedAsUnopened);
                x.EndUserContext.SystemLabels.Should().ContainSingle(x => x == SystemLabel.Values.Default);
            })
            .SendCommand((_, ctx) => new SetSystemLabelCommand
            {
                EndUserId = ctx.GetParty(),
                DialogId = ctx.GetDialogId(),
                RemoveLabels = [SystemLabel.Values.MarkedAsUnopened]
            })
            .SendCommand((_, ctx) => GetDialog(ctx.GetDialogId()))
            .ExecuteAndAssert<DialogDto>(x =>
            {
                x.EndUserContext.SystemLabels.Should().NotContain(x => x == SystemLabel.Values.MarkedAsUnopened);
                x.EndUserContext.SystemLabels.Should().ContainSingle(x => x == SystemLabel.Values.Default);
            });

    [Fact]
    public Task Cannot_Set_Sent_System_Label() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SetSystemLabelsServiceOwner(x =>
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
            .SetSystemLabelsServiceOwner(x =>
                x.RemoveLabels = [SystemLabel.Values.Sent])
            .ExecuteAndAssert<ValidationError>(x =>
                x.ShouldHaveErrorWithText(
                    ValidationErrorStrings.SentLabelNotAllowed));

    [Fact]
    public async Task Set_Allows_PerformedBy_For_Admin()
    {
        await FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .AsAdminUser()
            .SetSystemLabelsServiceOwner(command =>
            {
                command.EndUserId = null;
                command.PerformedBy = new ActorDto
                {
                    ActorType = ActorType.Values.PartyRepresentative,
                    ActorId = AdminPerformedByActorId
                };
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
                        x.PerformedBy.ActorName.Should().Be("Brando Sando");
                        x.PerformedBy.ActorType.Should().Be(ActorType.Values.PartyRepresentative);
                        x.PerformedBy.ActorId.Should().Be(AdminPerformedByActorId);
                    });
            });
    }

    [Fact]
    public async Task Set_Allows_PerformedBy_With_Override_Name_For_Admin()
    {
        await FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .AsAdminUser()
            .SetSystemLabelsServiceOwner(command =>
            {
                command.EndUserId = null;
                command.PerformedBy = new ActorDto
                {
                    ActorType = ActorType.Values.PartyRepresentative,
                    ActorName = "Set by an admin"
                };
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
                        x.PerformedBy.ActorName.Should().Be("Set by an admin");
                        x.PerformedBy.ActorType.Should().Be(ActorType.Values.PartyRepresentative);
                        x.PerformedBy.ActorId.Should().BeNull();
                    });
            });
    }

    [Fact]
    public async Task Set_Allows_PerformedBy_For_Admin_Even_When_Party_Name_Registry_Is_Down()
    {
        await FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .ConfigurePartyNameRegistry(p =>
            {
                p.QueryPartyName(Arg.Any<NameLookup>(), Arg.Any<CancellationToken>())
                    .Returns(TestPartyNameRegistry.InternalServerError);
            })
            .AsAdminUser()
            .SetSystemLabelsServiceOwner(command =>
            {
                command.EndUserId = null;
                command.PerformedBy = new ActorDto
                {
                    ActorType = ActorType.Values.PartyRepresentative,
                    ActorId = AdminPerformedByActorId
                };
                command.AddLabels = [SystemLabel.Values.Archive];
            })
            // .SendCommand((_, ctx) => GetDialog(ctx.GetDialogId()))
            // .AssertResult<DialogDto>(x =>
            //     x.EndUserContext.SystemLabels.Should().ContainSingle(label => label == SystemLabel.Values.Archive))
            .GetLabelAssignmentLogs()
            .AssertResult<List<LabelAssignmentLogDto>>(x =>
            {
                x.Should().HaveCount(1)
                    .And.AllSatisfy(x =>
                    {
                        x.PerformedBy.Should().NotBeNull();
                        x.PerformedBy.ActorName.Should().BeNull();
                        x.PerformedBy.ActorType.Should().Be(ActorType.Values.PartyRepresentative);
                        x.PerformedBy.ActorId.Should().Be(AdminPerformedByActorId);
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
                        x.PerformedBy.ActorName.Should().Be("Brando Sando");
                        x.PerformedBy.ActorType.Should().Be(ActorType.Values.PartyRepresentative);
                        x.PerformedBy.ActorId.Should().Be(AdminPerformedByActorId);
                    });
            });
    }

    [Fact]
    public Task Set_PerformedBy_For_Non_Admin_Is_Forbidden() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SetSystemLabelsServiceOwner(command =>
            {
                command.EndUserId = null;
                command.PerformedBy = new ActorDto
                {
                    ActorType = ActorType.Values.PartyRepresentative,
                    ActorId = AdminPerformedByActorId
                };
                command.AddLabels = [SystemLabel.Values.Archive];
            })
            .ExecuteAndAssert<Forbidden>();

    private static GetDialogQuery GetDialog(Guid? id) => new() { DialogId = id!.Value };
}
