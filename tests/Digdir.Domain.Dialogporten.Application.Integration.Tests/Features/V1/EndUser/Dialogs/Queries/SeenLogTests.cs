using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.GetSeenLog;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.SearchSeenLogs;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Parties;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using OneOf.Types;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;
using DialogDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get.DialogDto;
using SearchDialogDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search.DialogDto;
using SearchDialogSeenLogDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search.DialogSeenLogDto;
using SeenLogDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.GetSeenLog.SeenLogDto;
using SearchSeenLogDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.SearchSeenLogs.SeenLogDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.Dialogs.Queries;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class SeenLogTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    private const string DummyService = "urn:altinn:resource:test-service";

    [Fact]
    public Task Search_Dialog_SeenLog_Should_Not_Return_User_Ids_Unhashed() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.Dto.ServiceResource = DummyService)
            .GetEndUserDialog()
            .ConsumeEvents()
            .GetEndUserDialog()
            .AssertResult<DialogDto>(result =>
                result.SeenSinceLastUpdate.AssertSingleActorIdHashed())
            .SearchEndUserDialogs(x => x.ServiceResource = [DummyService])
            .ExecuteAndAssert<PaginatedList<SearchDialogDto>>(x => x.Items
                .Single()
                .SeenSinceLastUpdate
                .AssertSingleActorIdHashed());

    [Fact]
    public Task Get_SeenLog_Should_Not_Return_User_Ids_Unhashed() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .GetEndUserDialog()
            .ConsumeEvents()
            .GetEndUserDialog()
            .AssertResult<DialogDto>()
            .SendCommand(x => new GetSeenLogQuery
            {
                DialogId = x.Id,
                SeenLogId = x.SeenSinceLastUpdate.Single().Id
            })
            .ExecuteAndAssert<SeenLogDto>(result =>
            {
                result.SeenBy.ActorId
                    .Should()
                    .StartWith(NorwegianPersonIdentifier.HashPrefixWithSeparator);
            });

    [Fact]
    public Task Search_SeenLog_Should_Not_Return_User_Ids_Unhashed() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .GetEndUserDialog()
            .ConsumeEvents()
            .GetEndUserDialog()
            .AssertResult<DialogDto>()
            .SendCommand(x => new SearchSeenLogQuery { DialogId = x.Id })
            .ExecuteAndAssert<List<SearchSeenLogDto>>(result => result.Single()
                .SeenBy
                .ActorId
                .Should()
                .StartWith(NorwegianPersonIdentifier.HashPrefixWithSeparator));

    [Fact]
    public Task SeenLogs_Should_Track_UpdatedAt_And_ContentUpdatedAt_For_Different_Users() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .GetEndUserDialog() // Default integration test user
            .ConsumeEvents()
            .GetEndUserDialog() // Default integration test user
            .AssertResult<DialogDto>(BothSeenLogsContainsOneHashedEntry)
            // Non-content update
            .UpdateDialog(x => x.Dto.ExternalReference = "foo:bar")
            .AsIntegrationTestUser(x => x.WithPid("13213312833"))
            .GetEndUserDialog()
            .ConsumeEvents()
            .GetEndUserDialog() // Default integration test user
            .ExecuteAndAssert<DialogDto>(x =>
            {
                // Both users should be in SeenSinceLastContentUpdate
                x.SeenSinceLastContentUpdate.Count.Should().Be(2);

                // Only the new user should be in SeenSinceLastUpdate
                x.SeenSinceLastUpdate.AssertSingleActorIdHashed();
            });

    [Fact]
    public Task Multiple_Updates_Should_Result_In_Single_Entry_In_SeenSinceLastContentUpdate() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .GetEndUserDialog()
            .ConsumeEvents()
            .GetEndUserDialog()
            .AssertResult<DialogDto>(BothSeenLogsContainsOneHashedEntry)
            .UpdateDialog(x => x.Dto.ExternalReference = "foo:bar")
            .GetEndUserDialog()
            .ConsumeEvents()
            .GetEndUserDialog()
            .AssertResult<DialogDto>(BothSeenLogsContainsOneHashedEntry)
            .UpdateDialog(x => x.Dto.ExternalReference = "bar:baz")
            .GetEndUserDialog()
            .ConsumeEvents()
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(BothSeenLogsContainsOneHashedEntry);

    [Fact]
    public async Task SeenLogs_Should_Track_UpdatedAt_And_ContentUpdatedAt_For_Different_Users_On_Dialog_Search()
    {
        var dialogId = NewUuidV7();

        await FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.ServiceResource = DummyService;
                x.Dto.Id = dialogId;
            })
            .GetEndUserDialog() // Default integration test user
            .ConsumeEvents()
            .GetEndUserDialog()
            .AssertResult<DialogDto>(BothSeenLogsContainsOneHashedEntry)
            // Non-content update
            .UpdateDialog(x => x.Dto.ExternalReference = "foo:bar")
            .AsIntegrationTestUser(x => x.WithPid("13213312833"))
            .SendCommand(_ => new GetDialogQuery { DialogId = dialogId })
            .ConsumeEvents()
            .SearchEndUserDialogs(x => x.ServiceResource = [DummyService])
            .ExecuteAndAssert<PaginatedList<SearchDialogDto>>(result =>
            {
                var dialog = result.Items.Single();
                dialog.SeenSinceLastContentUpdate.Count.Should().Be(2);
                dialog.SeenSinceLastUpdate.AssertSingleActorIdHashed();
            });
    }

    [Fact]
    public Task Multiple_Gets_Should_Only_Create_One_SeenLogs() => FlowBuilder.For(Application)
        .CreateSimpleDialog()
        .GetEndUserDialog()
        .GetEndUserDialog()
        .ConsumeEvents()
        .GetEndUserSeenLogs()
        .ExecuteAndAssert<List<SearchSeenLogDto>>((x, ctx) =>
        {
            x.Count.Should().Be(1);
            ctx.Application.GetPublishedEvents().Count.Should().Be(0);
        });


    [Fact]
    public Task Multiple_Gets_Around_Update_Should_Create_Multiple_SeenLogs() => FlowBuilder.For(Application)
        .CreateSimpleDialog()
        .GetEndUserDialog()
        .UpdateDialog(x => x.Dto.ExternalReference = "foo:bar")
        .GetEndUserDialog()
        .ConsumeEvents()
        .GetEndUserSeenLogs()
        .ExecuteAndAssert<List<SearchSeenLogDto>>((x, ctx) =>
        {
            x.Count.Should().Be(2);
            ctx.Application.GetPublishedEvents().Count.Should().Be(0);
        });

    [Fact]
    public async Task Concurrent_SeenLog_Writes_For_Same_SeenLog_Should_Be_Idempotent()
    {
        var createResult = await FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .ExecuteAndAssert<CreateDialogSuccess>();
        var dialog = (await Application.GetDbEntities<DialogEntity>())
            .Single(x => x.Id == createResult.DialogId);
        var seenLogId = Guid.CreateVersion7();
        var gate = new ConcurrentSeenLogWriterGate(parallelism: 8);

        var results = await Task.WhenAll(Enumerable
            .Range(0, 8)
            .Select(_ => EnsureSeenLog(
                gate,
                seenLogId,
                dialog.Id,
                TestUsers.DefaultParty,
                DialogUserType.Values.Person,
                DateTimeOffset.UtcNow)));

        var seenLogs = await Application.GetDbEntities<DialogSeenLog>();
        var seenByActors = await Application.GetDbEntities<DialogSeenLogSeenByActor>();
        var actorNames = await Application.GetDbEntities<ActorName>();
        var actorId = TestUsers.DefaultParty.ToLowerInvariant();
        var actorName = actorNames.Should().ContainSingle(x =>
            x.ActorId == actorId
         && x.Name == "Brando Sando").Subject;

        seenLogs.Should().ContainSingle(x => x.Id == seenLogId);
        seenByActors.Should().ContainSingle(x => x.DialogSeenLogId == seenLogId);
        results
            .Select(x => x.ActorNameId)
            .Should()
            .OnlyContain(x => x == actorName.Id);
    }

    [Fact]
    public Task Multiple_Updates_Should_Result_In_Single_Entry_In_SeenSinceLastUpdate_On_Dialog_Search() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.Dto.ServiceResource = DummyService)
            .GetEndUserDialog()
            .ConsumeEvents()
            .AssertResult<DialogDto>()
            //.Assert bare en event igjen
            .UpdateDialog(x =>
            {
                x.Dto.ExternalReference = "foo:bar";
            })
            .GetEndUserDialog()
            .ConsumeEvents()
            .AssertResult<DialogDto>()
            .UpdateDialog(x => x.Dto.ExternalReference = "bar:baz")
            .GetEndUserDialog()
            .ConsumeEvents()
            .AssertResult<DialogDto>()
            .SearchEndUserDialogs(x => x.ServiceResource = [DummyService])
            .ConsumeEvents()
            .ExecuteAndAssert<PaginatedList<SearchDialogDto>>(x => x.Items
                .Single()
                .SeenSinceLastContentUpdate
                .AssertSingleActorIdHashed());

    private static void BothSeenLogsContainsOneHashedEntry(DialogDto x)
    {
        x.SeenSinceLastContentUpdate.AssertSingleActorIdHashed();
        x.SeenSinceLastUpdate.AssertSingleActorIdHashed();
    }

    private async Task<DialogSeenLogWriteResult> EnsureSeenLog(
        ConcurrentSeenLogWriterGate gate,
        Guid seenLogId,
        Guid dialogId,
        string actorId,
        DialogUserType.Values userType,
        DateTimeOffset seenAt)
    {
        using var scope = Application.GetServiceProvider().CreateScope();
        var cancellationToken = TestContext.Current.CancellationToken;
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var seenLogWriter = scope.ServiceProvider.GetRequiredService<IDialogSeenLogWriter>();

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        await gate.WaitUntilAllWritersAreReady(cancellationToken);

        var result = await seenLogWriter.EnsureSeenLog(
            seenLogId,
            dialogId,
            actorId,
            userType,
            seenAt,
            cancellationToken);

        var saveResult = await unitOfWork.SaveChangesAsync(cancellationToken);
        saveResult.Value.Should().BeOfType<Success>();
        return result;
    }

    [Fact]
    public Task CaseInsensitive_Party_Match_For_IsCurrentUser() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.Dto.Party = IdportenEmailUserIdentifier.PrefixWithSeparator + "Test@Test.no")
            .AsIntegrationEmailUser()
            .GetEndUserDialog()
            .ConsumeEvents()
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.SeenSinceLastUpdate.Should().ContainSingle()
                    .Which.IsCurrentEndUser.Should().BeTrue());

    private sealed class ConcurrentSeenLogWriterGate(int parallelism)
    {
        private readonly TaskCompletionSource _allWritersAreReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _entered;

        public async Task WaitUntilAllWritersAreReady(CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _entered) == parallelism)
            {
                _allWritersAreReady.TrySetResult();
            }

            await _allWritersAreReady.Task.WaitAsync(cancellationToken);
        }
    }
}

internal static class SeenLogAssertionExtensions
{
    public static void AssertSingleActorIdHashed(this List<DialogSeenLogDto> seenLogs) =>
        seenLogs
            .Single()
            .SeenBy
            .ActorId
            .Should()
            .StartWith(NorwegianPersonIdentifier.HashPrefixWithSeparator);

    public static void AssertSingleActorIdHashed(this List<SearchDialogSeenLogDto> seenLogs) =>
        seenLogs
            .Single()
            .SeenBy
            .ActorId
            .Should()
            .StartWith(NorwegianPersonIdentifier.HashPrefixWithSeparator);
}
