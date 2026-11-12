using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Context;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Actors.Events;
using Digdir.Domain.Dialogporten.Infrastructure.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Npgsql;

namespace Digdir.Domain.Dialogporten.Infrastructure.Integration.Tests.Persistence;

/// <summary>
/// Verifies how <see cref="ActorNameRepository"/> chooses between committing actor names and their
/// resync events in an isolated transaction, and writing them in the caller's transaction.
/// Uses the real MassTransit Entity Framework outbox, with the delivery service disabled.
/// </summary>
[Collection(nameof(DatabaseCollectionDefinition))]
public sealed class ActorNameRepositoryTransactionTests(PostgresDatabaseFixture database)
    : DatabaseCollectionFixture(database)
{
    private const string ActorId = "urn:altinn:person:identifier-no:09095614407";
    private const string Name = "Brando Sando";

    private Sut CreateSut() => Sut.Begin(Database.GetDataSource());

    [Fact]
    public async Task EnsureActorName_Outside_Transaction_Commits_Actor_Name_And_Resync_Event()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var sut = CreateSut();

        // Act
        var actorName = await sut.EnsureActorName(new ActorName { ActorId = ActorId }, ct);

        // Assert
        actorName.HasEvents().Should().BeFalse("resync event was committed to the outbox by the isolated transaction");

        var persisted = await GetPersistedActorNames(ct);
        persisted.Should().ContainSingle().Which.Id.Should().Be(actorName.Id);

        var outboxMessageTypes = await GetOutboxMessageTypes(ct);
        outboxMessageTypes.Should().ContainSingle().Which.Should().Contain(nameof(ResyncActorNameEvent));
    }

    [Fact]
    public async Task EnsureActorName_Outside_Transaction_Is_Not_Affected_By_Caller_Changes()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var sut = CreateSut();
        var unrelated = new ActorName { ActorId = "urn:altinn:person:identifier-no:01017512345", Name = "Unrelated" };
        sut.Db.Add(unrelated);

        // Act
        var actorName = await sut.EnsureActorName(new ActorName { ActorId = ActorId }, ct);

        // Assert
        sut.Db.Entry(unrelated).State.Should().Be(EntityState.Added, "the caller's pending changes must not be saved");

        var persisted = await GetPersistedActorNames(ct);
        persisted.Should().ContainSingle().Which.Id.Should().Be(actorName.Id);
    }

    [Fact]
    public async Task EnsureActorName_Outside_Transaction_Without_Resync_Need_Writes_No_Outbox_Message()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var sut = CreateSut();

        // Act
        var actorName = await sut.EnsureActorName(new ActorName { ActorId = ActorId, Name = Name }, ct);

        // Assert
        actorName.HasEvents().Should().BeFalse();

        var persisted = await GetPersistedActorNames(ct);
        persisted.Should().ContainSingle().Which.Id.Should().Be(actorName.Id);

        var outboxMessageTypes = await GetOutboxMessageTypes(ct);
        outboxMessageTypes.Should().BeEmpty();
    }

    [Fact]
    public async Task EnsureActorName_Outside_Transaction_For_Existing_Row_Writes_No_Outbox_Message()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        ActorName seeded;
        await using (var seeder = CreateSut())
        {
            seeded = await seeder.EnsureActorName(new ActorName { ActorId = ActorId }, ct);
        }

        await using var sut = CreateSut();
        await ClearOutbox(ct);

        // Act
        var actorName = await sut.EnsureActorName(new ActorName { ActorId = ActorId }, ct);

        // Assert
        actorName.Id.Should().Be(seeded.Id);
        actorName.HasEvents().Should().BeFalse();

        var outboxMessageTypes = await GetOutboxMessageTypes(ct);
        outboxMessageTypes.Should().BeEmpty();
    }

    [Fact]
    public async Task EnsureActorName_Inside_Transaction_Raises_Resync_Event_On_Entity_And_Follows_Caller_Transaction()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var sut = CreateSut();
        await sut.Db.Database.BeginTransactionAsync(ct);

        // Act
        var actorName = await sut.EnsureActorName(new ActorName { ActorId = ActorId }, ct);
        await sut.Db.Database.RollbackTransactionAsync(ct);

        // Assert
        actorName.PopDomainEvents().Should().ContainSingle().Which.Should().BeOfType<ResyncActorNameEvent>();

        var persisted = await GetPersistedActorNames(ct);
        persisted.Should().BeEmpty("the caller's transaction was rolled back");

        var outboxMessageTypes = await GetOutboxMessageTypes(ct);
        outboxMessageTypes.Should().BeEmpty();
    }

    // These helpers use their own context, so they only observe committed rows and never touch
    // the change tracker of the context under test.
    private async Task<List<ActorName>> GetPersistedActorNames(CancellationToken ct)
    {
        await using var db = Database.CreateDbContext();
        return await db.Set<ActorName>().AsNoTracking().ToListAsync(ct);
    }

    private async Task<List<string>> GetOutboxMessageTypes(CancellationToken ct)
    {
        await using var db = Database.CreateDbContext();
        return await db.Set<OutboxMessage>().Select(x => x.MessageType).ToListAsync(ct);
    }

    private async Task ClearOutbox(CancellationToken ct)
    {
        await using var db = Database.CreateDbContext();
        await db.Set<OutboxMessage>().ExecuteDeleteAsync(ct);
        await db.Set<OutboxState>().ExecuteDeleteAsync(ct);
    }

    private sealed class Sut : IAsyncDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly AsyncServiceScope _scope;

        public DialogDbContext Db { get; }
        public ActorNameRepository Repository { get; }

        private Sut(ServiceProvider provider)
        {
            _provider = provider;
            _scope = provider.CreateAsyncScope();
            Db = _scope.ServiceProvider.GetRequiredService<DialogDbContext>();
            Repository = _scope.ServiceProvider.GetRequiredService<ActorNameRepository>();
        }

        public static Sut Begin(NpgsqlDataSource dataSource) => new(BuildServiceProvider(dataSource));

        public Task<ActorName> EnsureActorName(ActorName actorName, CancellationToken ct) =>
            Repository.EnsureActorName(actorName, ct);

        public async ValueTask DisposeAsync()
        {
            await _scope.DisposeAsync();
            await _provider.DisposeAsync();
        }

        private static ServiceProvider BuildServiceProvider(NpgsqlDataSource dataSource)
        {
            var transactionTime = Substitute.For<ITransactionTime>();
            transactionTime.Value.Returns(DateTimeOffset.UtcNow);
            var applicationContext = Substitute.For<IApplicationContext>();
            applicationContext.Metadata.Returns([]);

            var services = new ServiceCollection();
            services.AddDbContext<DialogDbContext>(options => options
                .UseNpgsql(dataSource, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors());
            services.AddMassTransit(x =>
            {
                x.AddEntityFrameworkOutbox<DialogDbContext>(o =>
                {
                    o.UsePostgres();
                    o.UseBusOutbox(y => y.DisableDeliveryService());
                    o.DisableInboxCleanupService();
                });
                x.UsingInMemory();
            });
            services.AddLogging();
            services.AddSingleton(transactionTime);
            services.AddSingleton(applicationContext);
            services.AddScoped<ActorNameRepository>();
            return services.BuildServiceProvider();
        }
    }
}
