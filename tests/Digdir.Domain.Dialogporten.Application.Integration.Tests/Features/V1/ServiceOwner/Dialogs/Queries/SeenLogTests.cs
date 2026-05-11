using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.GetSeenLog;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.SearchSeenLogs;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Parties;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.Dialogs.Queries.SeenLogTests;
using GetDialogQueryEU = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get.GetDialogQuery;
using DialogDtoEU = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get.DialogDto;
using DialogDtoSO = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get.DialogDto;
using SearchDialogSeenLogDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search.DialogSeenLogDto;
using DialogSeenLogDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get.DialogSeenLogDto;
using SearchDialogDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search.DialogDto;
using SeenLogDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.GetSeenLog.SeenLogDto;
using SearchSeenLogDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.SearchSeenLogs.SeenLogDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Queries;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class SeenLogTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public Task Dialog_Not_Fetched_By_EndUser_Should_Have_Empty_SeenLogs() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDtoSO>(x =>
            {
                x.SeenSinceLastContentUpdate.Should().BeEmpty();
                x.SeenSinceLastUpdate.Should().BeEmpty();
            });

    [Fact]
    public Task Get_Dialog_SeenLog_Should_Return_User_Ids_Unhashed()
        => FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .GetEndUserDialog()
            .SendCommand((_, ctx) => new GetDialogQuery
            {
                DialogId = ctx.GetDialogId()
            })
            .ExecuteAndAssert<DialogDtoSO>(BothSeenLogsContainsOneUnHashedEntry);

    [Fact]
    public Task Search_Dialog_SeenLog_Should_Return_User_Ids_Unhashed() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .GetEndUserDialog()
            .AssertResult<DialogDtoEU>()
            .SendCommand((_, ctx) => new SearchDialogQuery { ServiceResource = [ctx.GetServiceResource()] })
            .ExecuteAndAssert<PaginatedList<SearchDialogDto>>(result =>
                result.Items
                    .Single()
                    .SeenSinceLastUpdate
                    .AssertSingleActorIdUnHashed());

    [Fact]
    public Task Get_SeenLog_Should_Return_User_Ids_Unhashed() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .GetEndUserDialog()
            .AssertResult<DialogDtoEU>()
            .SendCommand(x => new GetSeenLogQuery
            {
                DialogId = x.Id,
                SeenLogId = x.SeenSinceLastUpdate.Single().Id
            })
            .ExecuteAndAssert<SeenLogDto>(result =>
                result.SeenBy
                    .ActorId
                    .Should()
                    .StartWith(NorwegianPersonIdentifier.PrefixWithSeparator));

    [Fact]
    public Task Search_SeenLog_Should_Return_User_Ids_Unhashed() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .GetEndUserDialog()
            .AssertResult<DialogDtoEU>()
            .SendCommand(x => new SearchSeenLogQuery { DialogId = x.Id })
            .ExecuteAndAssert<List<SearchSeenLogDto>>(result =>
                result.Single()
                    .SeenBy
                    .ActorId
                    .Should()
                    .StartWith(NorwegianPersonIdentifier.PrefixWithSeparator));

    [Fact]
    public async Task SeenLogs_Should_Track_ContentUpdatedAt_For_Different_Users()
    {
        var dialogId = NewUuidV7();

        await FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.Dto.Id = dialogId)
            .GetEndUserDialog() // Default integration test user
            .AssertResult<DialogDtoEU>()
            // Non-content update
            .UpdateDialog(x => x.Dto.ExternalReference = "foo:bar")
            .AsIntegrationTestUser(x => x.WithPid("13213312833"))
            .SendCommand(_ => new GetDialogQueryEU { DialogId = dialogId })
            .SendCommand(_ => new GetDialogQuery { DialogId = dialogId })
            .SendCommand(_ => new GetDialogQuery { DialogId = dialogId })
            .ExecuteAndAssert<DialogDtoSO>(x =>
            {
                // Both users should be in SeenSinceLastContentUpdate
                x.SeenSinceLastContentUpdate.Count.Should().Be(2);

                // Only the new user should be in SeenSinceLastUpdate
                x.SeenSinceLastUpdate.AssertSingleActorIdUnHashed();
            });
    }

    [Fact]
    public Task SeenLogs_Should_Track_UpdatedAt_And_ContentUpdatedAt_For_Different_Users() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .AsIntegrationTestUser(x => x.WithPid("06326702550"))
            .GetServiceOwnerDialogAsEndUser()
            .AsIntegrationTestUser(x => x.WithPid("15242005985"))
            .GetServiceOwnerDialogAsEndUser()
            .ExecuteAndAssert<DialogDtoSO>(x =>
            {
                x.SeenSinceLastContentUpdate.Count.Should().Be(2);
                x.SeenSinceLastUpdate.Count.Should().Be(2);
                x.SeenSinceLastContentUpdate.Select(l => l.SeenBy.ActorId).Distinct().Count().Should().Be(2);
                x.SeenSinceLastUpdate.Select(l => l.SeenBy.ActorId).Distinct().Count().Should().Be(2);
            });

    [Fact]
    public Task Multiple_Non_content_Updates_Should_Result_In_New_Entry_In_Both_Seen_Logs() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .GetServiceOwnerDialogAsEndUser()
            .AssertResult<DialogDtoSO>(BothSeenLogsContainsOneUnHashedEntry)
            .UpdateDialog(x => x.Dto.ExternalReference = "foo:bar")
            .GetServiceOwnerDialogAsEndUser()
            .AssertResult<DialogDtoSO>(BothSeenLogsContainsOneUnHashedEntry)
            .UpdateDialog(x => x.Dto.ExternalReference = "bar:baz")
            .GetServiceOwnerDialogAsEndUser()
            .ExecuteAndAssert<DialogDtoSO>(BothSeenLogsContainsOneUnHashedEntry);

    [Fact]
    public Task Multiple_Content_Updates_Should_Result_In_Single_Entry_In_Both_Seen_Logs() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .GetServiceOwnerDialogAsEndUser()
            .AssertResult<DialogDtoSO>(BothSeenLogsContainsOneUnHashedEntry)
            .UpdateDialog(x => x.Dto.Content!.Title.Value =
            [
                new LocalizationDto
                {
                    Value = "updated",
                    LanguageCode = "nb"
                }
            ])
            .GetServiceOwnerDialogAsEndUser()
            .AssertResult<DialogDtoSO>(BothSeenLogsContainsOneUnHashedEntry)
            .UpdateDialog(x => x.Dto.Content!.Title.Value =
            [
                new LocalizationDto
                {
                    Value = "updated again",
                    LanguageCode = "nb"
                }
            ])
            .GetServiceOwnerDialogAsEndUser()
            .ExecuteAndAssert<DialogDtoSO>(BothSeenLogsContainsOneUnHashedEntry);

    private const string DummyService = "urn:altinn:resource:test-service";

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
            .AssertResult<DialogDtoEU>()
            // Non-content update
            .UpdateDialog(x => x.Dto.ExternalReference = "foo:bar")
            .AsIntegrationTestUser(x => x.WithPid("13213312833"))
            .ConsumeEvents()
            .SendCommand(_ => new GetDialogQueryEU { DialogId = dialogId })
            .ConsumeEvents()
            .SearchServiceOwnerDialogs(x => x.ServiceResource = [DummyService])
            .ExecuteAndAssert<PaginatedList<SearchDialogDto>>(result =>
            {
                var dialog = result.Items.Single();
                dialog.SeenSinceLastContentUpdate.Count.Should().Be(2);
                dialog.SeenSinceLastUpdate.AssertSingleActorIdUnHashed();
            });
    }

    [Fact]
    public Task Multiple_Updates_Should_Result_In_Single_Entry_In_SeenSinceLastUpdate_On_Dialog_Search() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.Dto.ServiceResource = DummyService)
            .GetEndUserDialog()
            .AssertResult<DialogDtoEU>()
            .UpdateDialog(x => x.Dto.ExternalReference = "foo:bar")
            .GetEndUserDialog()
            .AssertResult<DialogDtoEU>()
            .UpdateDialog(x => x.Dto.ExternalReference = "bar:baz")
            .GetEndUserDialog()
            .AssertResult<DialogDtoEU>()
            .SearchServiceOwnerDialogs(x => x.ServiceResource = [DummyService])
            .ExecuteAndAssert<PaginatedList<SearchDialogDto>>(x => x.Items
                .Single()
                .SeenSinceLastContentUpdate
                .AssertSingleActorIdUnHashed());

    [Fact]
    public Task Multiple_Gets_Should_Only_Create_One_SeenLogs() => FlowBuilder.For(Application)
        .CreateSimpleDialog()
        .ConsumeEvents()
        .GetServiceOwnerDialogAsEndUser()
        .GetServiceOwnerDialogAsEndUser()
        .GetServiceOwnerSeenLogs()
        .ExecuteAndAssert<List<SearchSeenLogDto>>((x, ctx) =>
        {
            x.Count.Should().Be(1);
            ctx.Application.GetPublishedEvents().Count.Should().Be(0);
        });

    [Fact]
    public Task Multiple_Gets_Around_Update_Should_Create_Multiple_SeenLogs() => FlowBuilder.For(Application)
        .CreateSimpleDialog()
        .GetServiceOwnerDialogAsEndUser()
        .UpdateDialog(x => x.Dto.ExternalReference = "foo:bar")
        .ConsumeEvents()
        .GetServiceOwnerDialogAsEndUser()
        .GetServiceOwnerSeenLogs()
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

        var ct = TestContext.Current.CancellationToken;
        using var scope = Application.GetServiceProvider().CreateScope();
        var dialog = DialogApplication.QueryDbEntities<DialogEntity>(scope)
            .Where(x => x.Id == createResult.DialogId)
            .Include(x => x.EndUserContext)
            .Single();
        var gate = new ConcurrentSeenLogWriterGate(parallelism: 8);

        await Task.WhenAll(Enumerable
            .Range(0, 8)
            .Select(_ => OnSeen(gate, dialog))
        );

        var seenLogs = await DialogApplication
            .QueryDbEntities<DialogSeenLog>(scope)
            .Include(x => x.SeenBy)
            .ToListAsync(ct);
        var seenByActors = await Application.GetDbEntities<DialogSeenLogSeenByActor>();
        var actorNames = await Application.GetDbEntities<ActorName>();
        var actorId = TestUsers.DefaultParty.ToLowerInvariant();

        var actor = actorNames.Should().ContainSingle(x => x.ActorId == actorId && x.Name == "Brando Sando");
        var seenLog = seenLogs.Should().ContainSingle(x => x.SeenBy.ActorNameEntityId == actor.Subject.Id);
        seenByActors.Should().ContainSingle(x => x.DialogSeenLogId == seenLog.Subject.Id);
    }

    private async Task OnSeen(ConcurrentSeenLogWriterGate gate, DialogEntity dialogEntity)
    {
        using var scope = Application.GetServiceProvider().CreateScope();
        var cancellationToken = TestContext.Current.CancellationToken;
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await gate.WaitUntilAllWritersAreReady(cancellationToken);

        var result = await mediator.Send(new GetDialogQuery
        {
            DialogId = dialogEntity.Id,
            EndUserId = dialogEntity.Party
        }, cancellationToken);

        result.Value.Should().BeOfType<DialogDtoSO>();
    }

    private static void BothSeenLogsContainsOneUnHashedEntry(DialogDtoSO x)
    {
        x.SeenSinceLastContentUpdate.AssertSingleActorIdUnHashed();
        x.SeenSinceLastUpdate.AssertSingleActorIdUnHashed();
    }
}

internal static class SeenLogAssertionExtensions
{
    public static void AssertSingleActorIdUnHashed(this List<DialogSeenLogDto> seenLogs) =>
        seenLogs
            .Single()
            .SeenBy
            .ActorId
            .Should()
            .StartWith(NorwegianPersonIdentifier.PrefixWithSeparator);

    public static void AssertSingleActorIdUnHashed(this List<SearchDialogSeenLogDto> seenLogs) =>
        seenLogs
            .Single()
            .SeenBy
            .ActorId
            .Should()
            .StartWith(NorwegianPersonIdentifier.PrefixWithSeparator);
}
