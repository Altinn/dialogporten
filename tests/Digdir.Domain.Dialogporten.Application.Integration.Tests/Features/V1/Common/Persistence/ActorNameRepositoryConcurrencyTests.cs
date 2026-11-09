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
    private const string SerializationFailureSqlState = "40001";
    private static readonly TimeSpan BlockedTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Concurrent_EnsureActorName_Resolves_To_Same_Row_When_First_Transaction_Commits()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var a = await TransactionalActor.Begin(Application, IsolationLevel.ReadCommitted, ct);
        await using var b = await TransactionalActor.Begin(Application, IsolationLevel.ReadCommitted, ct);

        var actorNameA = await a.EnsureActorName(ActorId, ct);
        var ensureB = b.EnsureActorName(ActorId, ct);
        await WaitUntilBlocked(b.ProcessId, ensureB, ct);

        await a.Commit(ct);
        var actorNameB = await ensureB;
        await b.Commit(ct);

        actorNameB.Id.Should().Be(actorNameA.Id);
        actorNameB.CreatedAt.Should().Be(actorNameA.CreatedAt);
        actorNameA.PopDomainEvents().Should().ContainSingle().Which.Should().BeOfType<ResyncActorNameEvent>();
        actorNameB.HasEvents().Should().BeFalse("B did not create the row, so it should not request a resync");

        var persisted = await Application.GetDbEntities<ActorName>();
        persisted.Should().ContainSingle(x => x.ActorId == ActorId).Which.Id.Should().Be(actorNameA.Id);
    }

    [Fact]
    public async Task Concurrent_EnsureActorName_Creates_Own_Row_When_First_Transaction_Rolls_Back()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var a = await TransactionalActor.Begin(Application, IsolationLevel.ReadCommitted, ct);
        await using var b = await TransactionalActor.Begin(Application, IsolationLevel.ReadCommitted, ct);

        var actorNameA = await a.EnsureActorName(ActorId, ct);
        var ensureB = b.EnsureActorName(ActorId, ct);
        await WaitUntilBlocked(b.ProcessId, ensureB, ct);

        await a.Rollback(ct);
        var actorNameB = await ensureB;
        await b.Commit(ct);

        actorNameB.Id.Should().NotBe(actorNameA.Id);
        actorNameB.PopDomainEvents().Should().ContainSingle().Which.Should().BeOfType<ResyncActorNameEvent>();

        var persisted = await Application.GetDbEntities<ActorName>();
        persisted.Should().ContainSingle(x => x.ActorId == ActorId).Which.Id.Should().Be(actorNameB.Id);
    }

    [Fact]
    public async Task Concurrent_EnsureActorName_In_RepeatableRead_Fails_With_Serialization_Failure_When_First_Transaction_Commits()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var a = await TransactionalActor.Begin(Application, IsolationLevel.ReadCommitted, ct);
        await using var b = await TransactionalActor.Begin(Application, IsolationLevel.RepeatableRead, ct);

        await a.EnsureActorName(ActorId, ct);
        var ensureB = b.EnsureActorName(ActorId, ct);
        await WaitUntilBlocked(b.ProcessId, ensureB, ct);

        await a.Commit(ct);
        var act = () => ensureB;

        var exception = (await act.Should().ThrowAsync<Exception>()).Which;
        FindPostgresException(exception).Should().NotBeNull()
            .And.Subject.As<PostgresException>().SqlState.Should().Be(SerializationFailureSqlState);
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

        public Task<ActorName> EnsureActorName(string actorId, CancellationToken cancellationToken) =>
            _repository.EnsureActorName(new ActorName { ActorId = actorId, Name = null }, cancellationToken);

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
