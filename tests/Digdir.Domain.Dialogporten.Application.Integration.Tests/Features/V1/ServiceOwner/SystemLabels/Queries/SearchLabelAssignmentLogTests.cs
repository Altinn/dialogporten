using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.EndUserContext.Queries.SearchLabelAssignmentLog;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using NSubstitute;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.SystemLabels.Queries;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class SearchLabelAssignmentLogTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    private const string AdminPerformedByActorId = "urn:altinn:organization:identifier-no:991825827";

    [Fact]
    public Task Search_With_EndUserId_Returns_Label_Assignment_Log() =>
        FlowBuilder.For(Application)
            .AsServiceOwnerOnBehalfOfPerson()
            .CreateSimpleDialog()
            .SetSystemLabelsServiceOwner(x => x.AddLabels = [SystemLabel.Values.Bin])
            .SearchSystemLabelsServiceOwner()
            .ExecuteAndAssert<List<LabelAssignmentLogDto>>(x =>
            {
                var log = x.Should().ContainSingle().Subject;
                log.Name.Should().Be("systemlabel:Bin");
                log.Action.Should().Be("set");
                log.PerformedBy.ActorType.Should().Be(ActorType.Values.PartyRepresentative);
                log.PerformedBy.ActorId.Should().Be(TestUsers.DefaultParty);
            });

    [Fact]
    public Task Search_With_EndUserId_Returns_NotFound_When_End_User_Has_No_Access() =>
        FlowBuilder.For(Application)
            .AsServiceOwnerOnBehalfOfPerson()
            .CreateSimpleDialog()
            .SetSystemLabelsServiceOwner(x => x.AddLabels = [SystemLabel.Values.Bin])
            .ConfigureAltinnAuthorization(altinnAuthorization =>
            {
                altinnAuthorization
                    .GetDialogDetailsAuthorization(Arg.Any<DialogEntity>(), Arg.Any<CancellationToken>())
                    .Returns(new DialogDetailsAuthorizationResult { AuthorizedChecks = [] });
                altinnAuthorization
                    .HasListAuthorizationForDialog(Arg.Any<DialogEntity>(), Arg.Any<CancellationToken>())
                    .Returns(false);
            })
            .SearchSystemLabelsServiceOwner()
            .ExecuteAndAssert<EntityNotFound<DialogEntity>>();

    [Fact]
    public Task Search_Without_EndUserId_Returns_Label_Assignment_Log() =>
        FlowBuilder.For(Application)
            .AsServiceOwnerOnBehalfOfPerson()
            .CreateSimpleDialog()
            .SetSystemLabelsServiceOwner(x => x.AddLabels = [SystemLabel.Values.Archive])
            .AsServiceOwner()
            .SearchSystemLabelsServiceOwner()
            .ExecuteAndAssert<List<LabelAssignmentLogDto>>(x =>
            {
                var log = x.Should().ContainSingle().Subject;
                log.Name.Should().Be("systemlabel:Archive");
                log.Action.Should().Be("set");
                log.PerformedBy.ActorType.Should().Be(ActorType.Values.PartyRepresentative);
                log.PerformedBy.ActorId.Should().Be(TestUsers.DefaultParty);
            });

    [Fact]
    public Task Search_Without_EndUserId_Does_Not_Check_End_User_Access() =>
        FlowBuilder.For(Application)
            .AsServiceOwnerOnBehalfOfPerson()
            .CreateSimpleDialog()
            .SetSystemLabelsServiceOwner(x => x.AddLabels = [SystemLabel.Values.Bin])
            .ConfigureAltinnAuthorization(altinnAuthorization =>
            {
                altinnAuthorization
                    .GetDialogDetailsAuthorization(Arg.Any<DialogEntity>(), Arg.Any<CancellationToken>())
                    .Returns(new DialogDetailsAuthorizationResult { AuthorizedChecks = [] });
                altinnAuthorization
                    .HasListAuthorizationForDialog(Arg.Any<DialogEntity>(), Arg.Any<CancellationToken>())
                    .Returns(false);
            })
            .AsServiceOwner()
            .SearchSystemLabelsServiceOwner(x => x.EndUserId = null)
            .ExecuteAndAssert<List<LabelAssignmentLogDto>>(x => x.Should().ContainSingle());

    [Fact]
    public Task Search_Returns_Entries_Ordered_By_CreatedAt() =>
        FlowBuilder.For(Application)
            .AsServiceOwnerOnBehalfOfPerson()
            .CreateSimpleDialog()
            .SetSystemLabelsServiceOwner(x => x.AddLabels = [SystemLabel.Values.Bin])
            .SetSystemLabelsServiceOwner(x => x.AddLabels = [SystemLabel.Values.Archive])
            .SearchSystemLabelsServiceOwner()
            .ExecuteAndAssert<List<LabelAssignmentLogDto>>(x =>
            {
                x.Count.Should().Be(3);
                x.Should().BeInAscendingOrder(log => log.CreatedAt);
            });

    [Fact]
    public Task Search_Returns_PerformedBy_Set_By_Admin() =>
        FlowBuilder.For(Application)
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
            .SearchSystemLabelsServiceOwner()
            .ExecuteAndAssert<List<LabelAssignmentLogDto>>(x =>
            {
                var log = x.Should().ContainSingle().Subject;
                log.PerformedBy.ActorType.Should().Be(ActorType.Values.PartyRepresentative);
                log.PerformedBy.ActorId.Should().Be(AdminPerformedByActorId);
            });

    [Fact]
    public Task Search_With_EndUserId_Returns_Gone_For_Deleted_Dialog() =>
        FlowBuilder.For(Application)
            .AsServiceOwnerOnBehalfOfPerson()
            .CreateSimpleDialog()
            .DeleteDialog()
            .SearchSystemLabelsServiceOwner()
            .ExecuteAndAssert<EntityDeleted<DialogEntity>>();

    [Fact]
    public Task Search_Without_EndUserId_Returns_Gone_For_Deleted_Dialog() =>
        FlowBuilder.For(Application)
            .AsServiceOwner()
            .CreateSimpleDialog()
            .DeleteDialog()
            .SearchSystemLabelsServiceOwner()
            .ExecuteAndAssert<EntityDeleted<DialogEntity>>();

    [Fact]
    public Task Search_With_EndUserId_Returns_NotFound_For_Unknown_Dialog() =>
        FlowBuilder.For(Application)
            .AsServiceOwnerOnBehalfOfPerson()
            .SearchSystemLabelsServiceOwner(_ => new SearchLabelAssignmentLogQuery
            {
                DialogId = NewUuidV7(),
                EndUserId = TestUsers.DefaultParty,
            })
            .ExecuteAndAssert<EntityNotFound<DialogEntity>>();

    [Fact]
    public Task Search_Without_EndUserId_Returns_NotFound_For_Unknown_Dialog() =>
        FlowBuilder.For(Application)
            .AsServiceOwner()
            .SearchSystemLabelsServiceOwner(_ => new SearchLabelAssignmentLogQuery
            {
                DialogId = NewUuidV7(),
            })
            .ExecuteAndAssert<EntityNotFound<DialogEntity>>();
}
