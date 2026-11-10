using System.Data;
using System.Diagnostics;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Infrastructure.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Actors.Events;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Npgsql;

namespace Digdir.Domain.Dialogporten.Infrastructure.Integration.Tests.Persistence;

[Collection(nameof(DatabaseCollectionDefinition))]
public sealed class ActorNameRepositoryConcurrencyTests(PostgresDatabaseFixture database)
    : DatabaseCollectionFixture(database)
{
    private const string ActorId = "urn:altinn:person:identifier-no:09095614407";
    private const string Name = "Brando Sando";
    private const string SerializationFailureSqlState = "40001";
    private static readonly TimeSpan BlockedTimeout = TimeSpan.FromSeconds(10);

    private async Task<Sut> CreateSut(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        return await Sut.Begin(
            Database.CreateDbContext(),
            Database.GetDataSource(),
            isolationLevel,
            TestContext.Current.CancellationToken);
    }

    [Theory, ClassData<TestData>]
    public async Task Concurrent_EnsureActorName_Resolves_To_Same_Row_When_First_Transaction_Commits(ActorName actorName)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var a = await CreateSut();
        await using var b = await CreateSut();

        // Act
        var actorNameA = await a.EnsureActorName(actorName, ct);
        var thePromiseOfActorName = await b.EnsureActorNameAssertBlock(actorName, ct);
        await a.Db.Database.CommitTransactionAsync(ct);
        var actorNameB = await thePromiseOfActorName;
        await b.Db.Database.CommitTransactionAsync(ct);

        // Assert
        actorNameB.Id.Should().Be(actorNameA.Id);
        actorNameB.CreatedAt.Should().Be(actorNameA.CreatedAt);
        AssertResyncRequestedIfNeeded(actorNameA);
        actorNameB.HasEvents().Should().BeFalse("B did not create the row, so it should not request a resync");
        var persisted = await GetPersistedActorNames();
        persisted.Should().ContainSingle(x => x.ActorId == actorName.ActorId && x.Name == actorName.Name)
            .Which.Id.Should().Be(actorNameA.Id);
    }

    [Theory, ClassData<TestData>]
    public async Task Concurrent_EnsureActorName_Creates_Own_Row_When_First_Transaction_Rolls_Back(ActorName actorName)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var a = await CreateSut();
        await using var b = await CreateSut();

        var actorNameA = await a.EnsureActorName(actorName, ct);
        var promiseOfB = await b.EnsureActorNameAssertBlock(actorName, ct);

        await a.Db.Database.RollbackTransactionAsync(ct);
        var actorNameB = await promiseOfB;
        await b.Db.Database.CommitTransactionAsync(ct);

        actorNameB.Id.Should().NotBe(actorNameA.Id);
        AssertResyncRequestedIfNeeded(actorNameB);

        var persisted = await GetPersistedActorNames();
        persisted.Should().ContainSingle(x => x.ActorId == actorNameB.ActorId && x.Name == actorNameB.Name)
            .Which.Id.Should().Be(actorNameB.Id);
    }

    [Theory, ClassData<TestData>]
    public async Task Concurrent_EnsureActorName_In_RepeatableRead_Fails_With_Serialization_Failure_When_First_Transaction_Commits(ActorName actorName)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var a = await CreateSut();
        await using var b = await CreateSut(IsolationLevel.RepeatableRead);

        await a.EnsureActorName(actorName, ct);
        var promiseOfB = await b.EnsureActorNameAssertBlock(actorName, ct);

        await a.Db.Database.CommitTransactionAsync(ct);
        var act = () => promiseOfB;

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
    [Theory, ClassData<TestData>]
    public async Task EnsureActorName_For_Existing_Row_Does_Not_Block_Concurrent_Transactions(ActorName actorName)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        ActorName seeded;
        await using (var seed = await CreateSut())
        {
            seeded = await seed.EnsureActorName(actorName, ct);
            await seed.Db.Database.CommitTransactionAsync(ct);
        }

        await using var a = await CreateSut();
        await using var b = await CreateSut();

        // Act
        var actorNameA = await a.EnsureActorName(actorName, ct);
        var promiseOfB = await b.EnsureActorNameAssertNonBlock(actorName, ct);
        var actorNameB = await promiseOfB;
        await a.Db.Database.CommitTransactionAsync(ct);
        await b.Db.Database.CommitTransactionAsync(ct);

        // Assert
        actorNameA.Id.Should().Be(seeded.Id);
        actorNameB.Id.Should().Be(seeded.Id);
        actorNameA.HasEvents().Should().BeFalse("the row already existed, so no resync should be requested");
        actorNameB.HasEvents().Should().BeFalse("the row already existed, so no resync should be requested");

        var persisted = await GetPersistedActorNames();
        persisted.Should().ContainSingle(x => x.ActorId == actorName.ActorId && x.Name == actorName.Name)
            .Which.Id.Should().Be(seeded.Id);
    }

    private sealed class TestData : TheoryData<ActorName>
    {
        public TestData()
        {
            Add(new ActorName { ActorId = ActorId, Name = Name });
            Add(new ActorName { ActorId = ActorId, Name = null });
            Add(new ActorName { ActorId = null, Name = Name });
        }
    }

    private async Task<List<ActorName>> GetPersistedActorNames()
    {
        await using var db = Database.CreateDbContext();
        return await db.Set<ActorName>().AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);
    }

    private static void AssertResyncRequestedIfNeeded(ActorName actorName)
    {
        if (actorName.ActorId is null || actorName.Name is not null)
        {
            actorName.HasEvents().Should().BeFalse("a resync is only needed for an actor id without a name");
            return;
        }

        actorName.PopDomainEvents().Should().ContainSingle().Which.Should().BeOfType<ResyncActorNameEvent>();
    }

    private static PostgresException? FindPostgresException(Exception? exception) => exception switch
    {
        null => null,
        PostgresException postgresException => postgresException,
        _ => FindPostgresException(exception.InnerException)
    };

    private sealed class Sut : IAsyncDisposable
    {
        private readonly NpgsqlDataSource _dataSource;
        private readonly int _processId;

        public DialogDbContext Db { get; set; }
        public ActorNameRepository Repository { get; }

        private Sut(DialogDbContext db, NpgsqlDataSource dataSource, int processId)
        {
            var transactionTime = Substitute.For<ITransactionTime>();
            transactionTime.Value.Returns(DateTimeOffset.UtcNow);
            Db = db;
            _dataSource = dataSource;
            _processId = processId;
            Repository = new(db, transactionTime);
        }

        public static async Task<Sut> Begin(
            DialogDbContext db,
            NpgsqlDataSource dataSource,
            IsolationLevel isolationLevel,
            CancellationToken cancellationToken)
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
            var processId = ((NpgsqlConnection)db.Database.GetDbConnection()).ProcessID;
            var repo = new Sut(db, dataSource, processId);
            await db.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
            return repo;
        }

        public Task<ActorName> EnsureActorName(ActorName actorName, CancellationToken cancellationToken) =>
            Repository.EnsureActorName(CopyOf(actorName), cancellationToken);

        // The repository mutates the instance it is given (Id, CreatedAt, events), so every
        // transaction must work on its own instance, just like separate requests would.
        private static ActorName CopyOf(ActorName actorName) =>
            new() { ActorId = actorName.ActorId, Name = actorName.Name };

        public async Task<Task<ActorName>> EnsureActorNameAssertBlock(ActorName actorName, CancellationToken cancellationToken)
        {
            var task = EnsureActorName(actorName, cancellationToken);
            if (await WasBlockedBeforeCompleting(task, cancellationToken)) return task;
            await task; // Surface any exception
            throw new InvalidOperationException("Expected the transaction to block, but it completed.");
        }

        public async Task<Task<ActorName>> EnsureActorNameAssertNonBlock(ActorName actorName, CancellationToken cancellationToken)
        {
            var task = EnsureActorName(actorName, cancellationToken);
            if (!await WasBlockedBeforeCompleting(task, cancellationToken)) return task;
            throw new InvalidOperationException("Expected the transaction not to block, but it was blocked by another transaction.");
        }

        private async Task<bool> WasBlockedBeforeCompleting(Task pending, CancellationToken cancellationToken)
        {
            await using var command = _dataSource.CreateCommand("SELECT cardinality(pg_blocking_pids($1)) > 0");
            command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = _processId });

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

                await Task.Delay(TimeSpan.FromMilliseconds(20), cancellationToken);
            }

            throw new TimeoutException($"Backend {_processId} neither completed nor blocked within {BlockedTimeout}.");
        }

        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }
}
