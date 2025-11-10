using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.GetSeenLog;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.SearchSeenLogs;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.Parties;
using FluentAssertions;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;
using DialogDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get.DialogDto;
using SearchDialogDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.SearchOld.DialogDto;
using SearchDialogSeenLogDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.SearchOld.DialogSeenLogDto;
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
            .CreateSimpleDialog(x => x.Dto.ServiceResource = DummyService)
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
            .AssertResult<DialogDto>()
            .SendCommand(x => new SearchSeenLogQuery { DialogId = x.Id })
            .ExecuteAndAssert<List<SearchSeenLogDto>>(result => result.Single()
                .SeenBy
                .ActorId
                .Should()
                .StartWith(NorwegianPersonIdentifier.HashPrefixWithSeparator));

    [Fact]
    public async Task SeenLogs_Should_Track_UpdatedAt_And_ContentUpdatedAt_For_Different_Users()
    {
        var dialogId = NewUuidV7();

        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.Id = dialogId)
            .GetEndUserDialog() // Default integration test user
            .AssertResult<DialogDto>(BothSeenLogsContainsOneHashedEntry)
            // Non-content update
            .UpdateDialog(x => x.Dto.ExternalReference = "foo:bar")
            .ExecuteAndAssert<UpdateDialogSuccess>();

        Application.ConfigureServices(x => x.ChangeUserPid("13213312833"));

        await FlowBuilder.For(Application)
            // Fetch as new EndUser
            .SendCommand(_ => new GetDialogQuery { DialogId = dialogId })
            .ExecuteAndAssert<DialogDto>(x =>
            {
                // Both users should be in SeenSinceLastContentUpdate
                x.SeenSinceLastContentUpdate.Count.Should().Be(2);

                // Only the new user should be in SeenSinceLastUpdate
                x.SeenSinceLastUpdate.AssertSingleActorIdHashed();
            });
    }

    [Fact]
    public Task Multiple_Updates_Should_Result_In_Single_Entry_In_SeenSinceLastContentUpdate() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .GetEndUserDialog()
            .AssertResult<DialogDto>(BothSeenLogsContainsOneHashedEntry)
            .UpdateDialog(x => x.Dto.ExternalReference = "foo:bar")
            .GetEndUserDialog()
            .AssertResult<DialogDto>(BothSeenLogsContainsOneHashedEntry)
            .UpdateDialog(x => x.Dto.ExternalReference = "bar:baz")
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(BothSeenLogsContainsOneHashedEntry);

    [Fact]
    public async Task SeenLogs_Should_Track_UpdatedAt_And_ContentUpdatedAt_For_Different_Users_On_Dialog_Search()
    {
        var dialogId = NewUuidV7();

        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                x.Dto.ServiceResource = DummyService;
                x.Dto.Id = dialogId;
            })
            .GetEndUserDialog() // Default integration test user
            .AssertResult<DialogDto>(BothSeenLogsContainsOneHashedEntry)
            // Non-content update
            .UpdateDialog(x => x.Dto.ExternalReference = "foo:bar")
            .ExecuteAndAssert<UpdateDialogSuccess>();

        Application.ConfigureServices(x => x.ChangeUserPid("13213312833"));

        await FlowBuilder.For(Application)
            // Fetch as new EndUser
            .SendCommand(_ => new GetDialogQuery { DialogId = dialogId })
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
            .CreateSimpleDialog(x => x.Dto.ServiceResource = DummyService)
            .GetEndUserDialog()
            .AssertResult<DialogDto>()
            .UpdateDialog(x => x.Dto.ExternalReference = "foo:bar")
            .GetEndUserDialog()
            .AssertResult<DialogDto>()
            .UpdateDialog(x => x.Dto.ExternalReference = "bar:baz")
            .GetEndUserDialog()
            .AssertResult<DialogDto>()
            .SearchEndUserDialogs(x => x.ServiceResource = [DummyService])
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
