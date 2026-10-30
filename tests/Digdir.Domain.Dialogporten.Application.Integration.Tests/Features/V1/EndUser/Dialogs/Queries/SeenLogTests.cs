using System.Diagnostics;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.GetSeenLog;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.SearchSeenLogs;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Actors.Events;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Events;
using Digdir.Domain.Dialogporten.Domain.Parties;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;
using static Digdir.Domain.Dialogporten.Infrastructure.Altinn.NameRegistry.IPartyNameRegistryTransport;
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
    public Task SeenLogs_Should_Track_ContentUpdatedAt_For_Different_Users() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .GetEndUserDialog() // Default integration test user
            .AssertResult<DialogDto>(BothSeenLogsContainsOneHashedEntry)
            // Non-content update
            .UpdateDialog(x => x.Dto.ExternalReference = "foo:bar")
            .AsIntegrationTestUser(x => x.WithPid("13213312833"))
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                // Both users should be in SeenSinceLastContentUpdate
                x.SeenSinceLastContentUpdate.Count.Should().Be(2);

                // Only the new user should be in SeenSinceLastUpdate
                x.SeenSinceLastUpdate.AssertSingleActorIdHashed();
            });

    [Fact]
    public Task SeenLogs_Should_Track_UpdatedAt_And_ContentUpdatedAt_For_Different_Users() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .AsIntegrationTestUser(x => x.WithPid("06326702550"))
            .GetEndUserDialog()
            .AsIntegrationTestUser(x => x.WithPid("15242005985"))
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                x.SeenSinceLastContentUpdate.Count.Should().Be(2);
                x.SeenSinceLastUpdate.Count.Should().Be(2);
                x.SeenSinceLastContentUpdate.Select(l => l.SeenBy.ActorId).Distinct().Count().Should().Be(2);
                x.SeenSinceLastUpdate.Select(l => l.SeenBy.ActorId).Distinct().Count().Should().Be(2);
            });


    [Fact]
    public Task SeenLogs_Should_Track_UpdatedAt_And_ContentUpdatedAt_For_SystemUser() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .AsSystemUser()
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                x.SeenSinceLastContentUpdate.Count.Should().Be(1);
                x.SeenSinceLastUpdate.Count.Should().Be(1);
                x.SeenSinceLastUpdate.First().SeenBy.ActorId.Should().Be(TestUsers.DefaultSystemUserUrn);
                x.SeenSinceLastContentUpdate.First().SeenBy.ActorId.Should().Be(TestUsers.DefaultSystemUserUrn);
            });

    [Fact]
    public Task Multiple_Non_content_Updates_Should_Result_In_New_Entry_In_Both_Seen_Logs() =>
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
    public Task Multiple_Content_Updates_Should_Result_In_Single_Entry_In_Both_Seen_Logs() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .GetEndUserDialog()
            .AssertResult<DialogDto>(BothSeenLogsContainsOneHashedEntry)
            .UpdateDialog(x => x.Dto.Content!.Title.Value =
            [
                new LocalizationDto
                {
                    Value = "updated",
                    LanguageCode = "nb"
                }
            ])
            .GetEndUserDialog()
            .AssertResult<DialogDto>(BothSeenLogsContainsOneHashedEntry)
            .UpdateDialog(x => x.Dto.Content!.Title.Value =
            [
                new LocalizationDto
                {
                    Value = "updated again",
                    LanguageCode = "nb"
                }
            ])
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
            .AssertResult<DialogDto>(BothSeenLogsContainsOneHashedEntry)
            // Non-content update
            .UpdateDialog(x => x.Dto.ExternalReference = "foo:bar")
            .AsIntegrationTestUser(x => x.WithPid("13213312833"))
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
    public Task Multiple_Gets_Should_Only_Create_One_SeenLogs_And_One_Pair_Of_Events()
    {
        var firstSeenLogId = Guid.Empty;
        return FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .ConsumeEvents((x, ctx) =>
            {
                x.Count.Should().Be(1);
                ((DialogCreatedDomainEvent)x[0]).DialogId.Should().Be(ctx.GetDialogId());
            })
            .GetEndUserDialog()
            .GetEndUserSeenLogs()
            .AssertResult<List<SearchSeenLogDto>>((x, ctx) =>
            {
                x.Count.Should().Be(1);
                x[0].Id.Should().NotBeEmpty();
                firstSeenLogId = x[0].Id;
                var events = ctx.Application.GetPublishedEvents();
                events.Count.Should().Be(2);
                ((DialogUpdatedDomainEvent)events[0]).DialogId.Should().Be(ctx.GetDialogId());
                ((DialogSeenDomainEvent)events[1]).DialogId.Should().Be(ctx.GetDialogId());
            })
            .GetEndUserDialog()
            .GetEndUserSeenLogs()
            .ExecuteAndAssert<List<SearchSeenLogDto>>((x, ctx) =>
            {
                x.Count.Should().Be(1);
                x[0].Id.Should().Be(firstSeenLogId);
                var events = ctx.Application.GetPublishedEvents();
                events.Count.Should().Be(2);
                ((DialogUpdatedDomainEvent)events[0]).DialogId.Should().Be(ctx.GetDialogId());
                ((DialogSeenDomainEvent)events[1]).DialogId.Should().Be(ctx.GetDialogId());
            });
    }


    [Fact]
    public Task Multiple_Gets_Should_Only_Create_One_SeenLogs_And_One_Pair_Of_Events_Even_When_Party_Name_Registry_Is_Down()
    {
        var firstSeenLogId = Guid.Empty;
        return FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .ConsumeEvents((x, ctx) =>
            {
                x.Count.Should().Be(1);
                ((DialogCreatedDomainEvent)x[0]).DialogId.Should().Be(ctx.GetDialogId());
            })
            .ConfigurePartyNameRegistry(p =>
            {
                p.QueryPartyNameResponse(Arg.Any<NameLookup>(), Arg.Any<CancellationToken>())
                    .Returns(TestPartyNameRegistry.InternalServerError);
            })
            .GetEndUserDialog()
            .GetEndUserSeenLogs()
            .AssertResult<List<SearchSeenLogDto>>((x, ctx) =>
            {
                x.Count.Should().Be(1);
                x[0].Id.Should().NotBeEmpty();
                x[0].SeenBy.ActorName.Should().BeNull();
                firstSeenLogId = x[0].Id;

                var events = ctx.Application.GetPublishedEvents();
                events.Count.Should().Be(3);
                ((DialogUpdatedDomainEvent)events[0]).DialogId.Should().Be(ctx.GetDialogId());
                ((DialogSeenDomainEvent)events[1]).DialogId.Should().Be(ctx.GetDialogId());

                var resyncActorNameEvent = (ResyncActorNameEvent)events[2];
                resyncActorNameEvent.ActorNameId.Should().NotBeEmpty();
                resyncActorNameEvent.Reason.Should().NotBeNullOrEmpty();
            })
            .ResetPartyNameRegistry()
            .ConsumeEvents()
            .GetEndUserDialog()
            .GetEndUserSeenLogs()
            .ExecuteAndAssert<List<SearchSeenLogDto>>((x, ctx) =>
            {
                x.Count.Should().Be(1);
                x[0].Id.Should().Be(firstSeenLogId);
                x[0].SeenBy.ActorName.Should().Be("Brando Sando");
                var events = ctx.Application.GetPublishedEvents();
                events.Count.Should().Be(0);
            });
    }

    [Theory]
    [InlineData("InternalServerError")]
    [InlineData("HttpRequestException")]
    [InlineData("NoMatch")]
    public Task Resync_Should_Throw_And_Keep_Null_Name_While_Party_Name_Registry_Is_Still_Failing(string failHow)
    {
        ResyncActorNameEvent? resyncEvent = null;
        return FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .ConsumeEvents()
            .ConfigurePartyNameRegistry(p =>
            {
                switch (failHow)
                {
                    case "InternalServerError":
                        p.QueryPartyNameResponse(Arg.Any<NameLookup>(), Arg.Any<CancellationToken>())
                            .Returns(TestPartyNameRegistry.InternalServerError);
                        break;
                    case "HttpRequestException":
                        p.QueryPartyNameResponse(Arg.Any<NameLookup>(), Arg.Any<CancellationToken>())
                            .Throws(new HttpRequestException());
                        break;
                    case "NoMatch":
                        p.QueryPartyNameResponse(Arg.Any<NameLookup>(), Arg.Any<CancellationToken>())
                            .Returns(TestPartyNameRegistry.OkNoMatch());
                        break;
                    default: throw new UnreachableException($"Unknown failhow {failHow}");
                }
            })
            .GetEndUserDialog()
            .Do(async ctx =>
            {
                resyncEvent = ctx.Application.GetPublishedEvents().OfType<ResyncActorNameEvent>().Single();

                // The handler throws so that the message bus retries the event later
                var consume = () => ctx.Application.PublishEvents();
                await consume.Should().ThrowAsync<InvalidOperationException>();
            })
            .GetEndUserDialog()
            .AssertResult<DialogDto>(x =>
                x.SeenSinceLastUpdate.Should().ContainSingle().Which.SeenBy.ActorName.Should().BeNull())
            .ResetPartyNameRegistry()
            .Do(async ctx =>
            {
                // Simulate the message bus retrying the failed event once the registry has recovered
                using var scope = ctx.Application.GetServiceProvider().CreateScope();
                var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
                resyncEvent.Should().NotBeNull();
                await publisher.Publish(resyncEvent);
            })
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.SeenSinceLastUpdate.Should().ContainSingle().Which.SeenBy.ActorName.Should().Be("Brando Sando"));
    }

    [Fact]
    public Task Multiple_Gets_Around_Update_Should_Create_Multiple_SeenLogs_And_Events() => FlowBuilder.For(Application)
        .CreateSimpleDialog()
        .ConsumeEvents((x, ctx) =>
        {
            x.Count.Should().Be(1);
            ((DialogCreatedDomainEvent)x[0]).DialogId.Should().Be(ctx.GetDialogId());
        })
        .GetEndUserDialog()
        .ConsumeEvents((x, ctx) =>
        {
            x.Count.Should().Be(2);
            ((DialogUpdatedDomainEvent)x[0]).DialogId.Should().Be(ctx.GetDialogId());
            ((DialogSeenDomainEvent)x[1]).DialogId.Should().Be(ctx.GetDialogId());
        })
        .UpdateDialog(x => x.Dto.ExternalReference = "foo:bar")
        .ConsumeEvents((x, ctx) =>
        {
            x.Count.Should().Be(1);
            ((DialogUpdatedDomainEvent)x[0]).DialogId.Should().Be(ctx.GetDialogId());
        })
        .GetEndUserDialog()
        .GetEndUserSeenLogs()
        .ExecuteAndAssert<List<SearchSeenLogDto>>((x, ctx) =>
        {
            x.Count.Should().Be(2);
            var events = ctx.Application.GetPublishedEvents();
            events.Count.Should().Be(2);
            ((DialogUpdatedDomainEvent)events[0]).DialogId.Should().Be(ctx.GetDialogId());
            ((DialogSeenDomainEvent)events[1]).DialogId.Should().Be(ctx.GetDialogId());
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

    [Fact]
    public Task Multiple_Updates_Should_Result_In_Single_Entry_In_SeenSinceLastUpdate_On_Dialog_Search() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.Dto.ServiceResource = DummyService)
            .GetEndUserDialog()
            .AssertResult<DialogDto>()
            //.Assert bare en event igjen
            .UpdateDialog(x =>
            {
                x.Dto.ExternalReference = "foo:bar";
            })
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

    [Fact]
    public Task CaseInsensitive_Party_Match_For_IsCurrentUser() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.Dto.Party = IdportenEmailUserIdentifier.PrefixWithSeparator + "Test@Test.no")
            .AsIntegrationEmailUser()
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.SeenSinceLastUpdate.Should().ContainSingle().Which.IsCurrentEndUser.Should().BeTrue());

    [Theory]
    [InlineData("InternalServerError")]
    [InlineData("HttpRequestException")]
    [InlineData("NoMatch")]
    public Task Party_Name_Register_Down_Or_No_Results_Should_Fallback_To_Async_Name_Fetch(string failHow) =>
        FlowBuilder.For(Application)
            .ConfigurePartyNameRegistry(p =>
            {
                switch (failHow)
                {
                    case "InternalServerError":
                        p.QueryPartyNameResponse(Arg.Any<NameLookup>(), Arg.Any<CancellationToken>())
                            .Returns(TestPartyNameRegistry.InternalServerError);
                        break;
                    case "HttpRequestException":
                        p.QueryPartyNameResponse(Arg.Any<NameLookup>(), Arg.Any<CancellationToken>())
                            .Throws(new HttpRequestException());
                        break;
                    case "NoMatch":
                        p.QueryPartyNameResponse(Arg.Any<NameLookup>(), Arg.Any<CancellationToken>())
                            .Returns(TestPartyNameRegistry.OkNoMatch());
                        break;
                    default: throw new UnreachableException($"Uknown failhow {failHow}");
                }
            })
            .ConsumeEvents()
            .CreateSimpleDialog()
            .GetEndUserDialog()
            .AssertResult<DialogDto>(x =>
                {
                    x.SeenSinceLastUpdate.Should().ContainSingle().Which.SeenBy.ActorName.Should().BeNull();
                }
            )
            .ResetPartyNameRegistry()
            .ConsumeEvents()
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                {
                    x.SeenSinceLastUpdate.Should().ContainSingle().Which.SeenBy.ActorName.Should().Be("Brando Sando");
                }
            );

    [Theory]
    [InlineData("InternalServerError")]
    [InlineData("HttpRequestException")]
    [InlineData("NoMatch")]
    public Task Party_Name_Register_Down_Or_No_Results_Should_Fallback_To_Async_Name_Fetch_For_System_User(string failHow) =>
        FlowBuilder.For(Application)
            .AsSystemUser()
            .ConfigurePartyNameRegistry(p =>
            {
                switch (failHow)
                {
                    case "InternalServerError":
                        p.QueryPartyNameResponse(Arg.Any<NameLookup>(), Arg.Any<CancellationToken>())
                            .Returns(TestPartyNameRegistry.InternalServerError);
                        break;
                    case "HttpRequestException":
                        p.QueryPartyNameResponse(Arg.Any<NameLookup>(), Arg.Any<CancellationToken>())
                            .Throws(new HttpRequestException());
                        break;
                    case "NoMatch":
                        p.QueryPartyNameResponse(Arg.Any<NameLookup>(), Arg.Any<CancellationToken>())
                            .Returns(TestPartyNameRegistry.OkNoMatch());
                        break;
                    default: throw new UnreachableException($"Uknown failhow {failHow}");
                }
            })
            .ConsumeEvents()
            .CreateSimpleDialog()
            .GetEndUserDialog()
            .AssertResult<DialogDto>(x =>
                {
                    x.SeenSinceLastUpdate.Should().ContainSingle().Which.SeenBy.ActorName.Should().BeNull();
                }
            )
            .ResetPartyNameRegistry()
            .ConsumeEvents()
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                {
                    x.SeenSinceLastUpdate.Should().ContainSingle().Which.SeenBy.ActorName.Should().Be("Systembruker");
                }
            );

    private static void BothSeenLogsContainsOneHashedEntry(DialogDto x)
    {
        x.SeenSinceLastContentUpdate.AssertSingleActorIdHashed();
        x.SeenSinceLastUpdate.AssertSingleActorIdHashed();
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
            AcceptedLanguages = []
        }, cancellationToken);

        result.Value.Should().BeOfType<DialogDto>();
    }

    public sealed class ConcurrentSeenLogWriterGate(int parallelism)
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
