using System.Data;
using System.Diagnostics;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Actors.Events;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Persistence;

[Collection(nameof(DialogCqrsCollectionFixture))]
public sealed class ActorNameRepositoryConcurrencyTests(DialogApplication application)
    : ApplicationCollectionFixture(application)
{
    private const string ActorId = "urn:altinn:person:identifier-no:09095614407";
    private const string Name = "Brando Sando";
    private const string SerializationFailureSqlState = "40001";
    private static readonly TimeSpan BlockedTimeout = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(ActorId, null)]
    [InlineData(ActorId, Name)]
    [InlineData(null, Name)]
    public async Task Concurrent_EnsureActorName_Resolves_To_Same_Row_When_First_Transaction_Commits(string? actorId, string? name)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var a = await TransactionalActor.Begin(Application, IsolationLevel.ReadCommitted, ct);
        await using var b = await TransactionalActor.Begin(Application, IsolationLevel.ReadCommitted, ct);

        var actorNameA = await a.EnsureActorName(actorId, name, ct);
        var ensureB = b.EnsureActorName(actorId, name, ct);
        await WaitUntilBlocked(b.ProcessId, ensureB, ct);

        await a.Commit(ct);
        var actorNameB = await ensureB;
        await b.Commit(ct);

        actorNameB.Id.Should().Be(actorNameA.Id);
        actorNameB.CreatedAt.Should().Be(actorNameA.CreatedAt);
        AssertResyncRequestedIfNeeded(actorNameA, actorId, name);
        actorNameB.HasEvents().Should().BeFalse("B did not create the row, so it should not request a resync");

        var persisted = await Application.GetDbEntities<ActorName>();
        persisted.Should().ContainSingle(x => x.ActorId == actorId && x.Name == name)
            .Which.Id.Should().Be(actorNameA.Id);
    }

    [Theory]
    [InlineData(ActorId, null)]
    [InlineData(ActorId, Name)]
    [InlineData(null, Name)]
    public async Task Concurrent_EnsureActorName_Creates_Own_Row_When_First_Transaction_Rolls_Back(string? actorId, string? name)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var a = await TransactionalActor.Begin(Application, IsolationLevel.ReadCommitted, ct);
        await using var b = await TransactionalActor.Begin(Application, IsolationLevel.ReadCommitted, ct);

        var actorNameA = await a.EnsureActorName(actorId, name, ct);
        var ensureB = b.EnsureActorName(actorId, name, ct);
        await WaitUntilBlocked(b.ProcessId, ensureB, ct);

        await a.Rollback(ct);
        var actorNameB = await ensureB;
        await b.Commit(ct);

        actorNameB.Id.Should().NotBe(actorNameA.Id);
        AssertResyncRequestedIfNeeded(actorNameB, actorId, name);

        var persisted = await Application.GetDbEntities<ActorName>();
        persisted.Should().ContainSingle(x => x.ActorId == actorId && x.Name == name)
            .Which.Id.Should().Be(actorNameB.Id);
    }

    [Theory]
    [InlineData(ActorId, null)]
    [InlineData(ActorId, Name)]
    [InlineData(null, Name)]
    public async Task Concurrent_EnsureActorName_In_RepeatableRead_Fails_With_Serialization_Failure_When_First_Transaction_Commits(string? actorId, string? name)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var a = await TransactionalActor.Begin(Application, IsolationLevel.ReadCommitted, ct);
        await using var b = await TransactionalActor.Begin(Application, IsolationLevel.RepeatableRead, ct);

        await a.EnsureActorName(actorId, name, ct);
        var ensureB = b.EnsureActorName(actorId, name, ct);
        await WaitUntilBlocked(b.ProcessId, ensureB, ct);

        await a.Commit(ct);
        var act = () => ensureB;

        var exception = (await act.Should().ThrowAsync<Exception>()).Which;
        FindPostgresException(exception).Should().NotBeNull()
            .And.Subject.As<PostgresException>().SqlState.Should().Be(SerializationFailureSqlState);
    }

    /// <summary>
    /// Ensuring an already committed actor name should be a plain read. If the lookup of
    /// preexisting rows misses (e.g. a non-null-safe join on ActorId), the query falls through
    /// to INSERT ... ON CONFLICT DO UPDATE, which writes a new row version and holds a row lock
    /// until commit, blocking every other transaction that uses the same actor name.
    /// </summary>
    [Theory]
    [InlineData(ActorId, null)]
    [InlineData(ActorId, Name)]
    [InlineData(null, Name)]
    public async Task EnsureActorName_For_Existing_Row_Does_Not_Block_Concurrent_Transactions(string? actorId, string? name)
    {
        var ct = TestContext.Current.CancellationToken;
        ActorName seeded;
        await using (var seed = await TransactionalActor.Begin(Application, IsolationLevel.ReadCommitted, ct))
        {
            seeded = await seed.EnsureActorName(actorId, name, ct);
            await seed.Commit(ct);
        }

        await using var a = await TransactionalActor.Begin(Application, IsolationLevel.ReadCommitted, ct);
        await using var b = await TransactionalActor.Begin(Application, IsolationLevel.ReadCommitted, ct);

        var actorNameA = await a.EnsureActorName(actorId, name, ct);
        var ensureB = b.EnsureActorName(actorId, name, ct);
        var bWasBlocked = await WasBlockedBeforeCompleting(b.ProcessId, ensureB, ct);

        // Release A before asserting, so a blocked B can finish and both scopes dispose cleanly.
        await a.Commit(ct);
        var actorNameB = await ensureB;
        await b.Commit(ct);

        bWasBlocked.Should().BeFalse("ensuring an existing actor name should not take a row lock");
        actorNameA.Id.Should().Be(seeded.Id);
        actorNameB.Id.Should().Be(seeded.Id);
        actorNameA.HasEvents().Should().BeFalse("the row already existed, so no resync should be requested");
        actorNameB.HasEvents().Should().BeFalse("the row already existed, so no resync should be requested");

        var persisted = await Application.GetDbEntities<ActorName>();
        persisted.Should().ContainSingle(x => x.ActorId == actorId && x.Name == name)
            .Which.Id.Should().Be(seeded.Id);
    }

    private async Task<bool> WasBlockedBeforeCompleting(int processId, Task pending, CancellationToken cancellationToken)
    {
        var dataSource = Application.GetServiceProvider().GetRequiredService<NpgsqlDataSource>();
        await using var command = dataSource.CreateCommand("SELECT cardinality(pg_blocking_pids($1)) > 0");
        command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = processId });

        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < BlockedTimeout)
        {
            if (pending.IsCompleted)
            {
                return false;
            }

            if (await command.ExecuteScalarAsync(cancellationToken) is true)
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(5), cancellationToken);
        }

        throw new TimeoutException($"Backend {processId} neither completed nor blocked within {BlockedTimeout}.");
    }

    private async Task WaitUntilBlocked(int processId, Task pending, CancellationToken cancellationToken)
    {
        var dataSource = Application.GetServiceProvider().GetRequiredService<NpgsqlDataSource>();
        await using var command = dataSource.CreateCommand("SELECT cardinality(pg_blocking_pids($1)) > 0");
        command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = processId });

        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < BlockedTimeout)
        {
            if (pending.IsCompleted)
            {
                await pending; // Surface any exception
                throw new InvalidOperationException("Expected the transaction to block, but it completed.");
            }

            if (await command.ExecuteScalarAsync(cancellationToken) is true)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(20), cancellationToken);
        }

        throw new TimeoutException($"Backend {processId} was not blocked within {BlockedTimeout}.");
    }

    /// <summary>
    /// The transaction that creates the row requests a resync only when there is an actor id without a name.
    /// </summary>
    private static void AssertResyncRequestedIfNeeded(ActorName creator, string? actorId, string? name)
    {
        if (actorId is null || name is not null)
        {
            creator.HasEvents().Should().BeFalse("a resync is only needed for an actor id without a name");
            return;
        }

        creator.PopDomainEvents().Should().ContainSingle().Which.Should().BeOfType<ResyncActorNameEvent>();
    }

    private static PostgresException? FindPostgresException(Exception? exception) => exception switch
    {
        null => null,
        PostgresException postgresException => postgresException,
        _ => FindPostgresException(exception.InnerException)
    };

    private sealed class TransactionalActor : IAsyncDisposable
    {
        private readonly IServiceScope _scope;
        private readonly DialogDbContext _db;
        private readonly IActorNameRepository _repository;

        private TransactionalActor(IServiceScope scope)
        {
            _scope = scope;
            _db = scope.ServiceProvider.GetRequiredService<DialogDbContext>();
            _repository = scope.ServiceProvider.GetRequiredService<IActorNameRepository>();
        }

        public int ProcessId { get; private set; }

        public static async Task<TransactionalActor> Begin(
            DialogApplication application,
            IsolationLevel isolationLevel,
            CancellationToken cancellationToken)
        {
            var actor = new TransactionalActor(application.GetServiceProvider().CreateScope());
            await actor._db.Database.OpenConnectionAsync(cancellationToken);
            actor.ProcessId = ((NpgsqlConnection)actor._db.Database.GetDbConnection()).ProcessID;
            await actor._db.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
            return actor;
        }

        public Task<ActorName> EnsureActorName(string? actorId, string? name, CancellationToken cancellationToken) =>
            _repository.EnsureActorName(new ActorName { ActorId = actorId, Name = name }, cancellationToken);

        public Task Commit(CancellationToken cancellationToken) =>
            _db.Database.CommitTransactionAsync(cancellationToken);

        public Task Rollback(CancellationToken cancellationToken) =>
            _db.Database.RollbackTransactionAsync(cancellationToken);

        public async ValueTask DisposeAsync()
        {
            if (_db.Database.CurrentTransaction is not null)
            {
                await _db.Database.RollbackTransactionAsync();
            }

            await _db.Database.CloseConnectionAsync();
            _scope.Dispose();
        }
    }
}
