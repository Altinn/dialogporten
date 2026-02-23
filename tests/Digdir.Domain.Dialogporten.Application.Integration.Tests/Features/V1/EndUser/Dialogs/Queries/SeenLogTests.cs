using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.GetSeenLog;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.SearchSeenLogs;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.Parties;
using AwesomeAssertions;
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
            .GetEndUserDialog()
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
    public Task Multiple_Updates_Should_Result_In_Single_Entry_In_SeenSinceLastUpdate_On_Dialog_Search() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.Dto.ServiceResource = DummyService)
            .GetEndUserDialog()
            .ConsumeEvents()
            .AssertResult<DialogDto>()
            .UpdateDialog(x => x.Dto.ExternalReference = "foo:bar")
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
