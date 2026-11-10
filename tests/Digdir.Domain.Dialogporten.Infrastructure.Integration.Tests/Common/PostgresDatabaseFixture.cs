using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using Digdir.Library.Entity.Abstractions.Features.Lookup;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;

namespace Digdir.Domain.Dialogporten.Infrastructure.Integration.Tests.Common;

/// <summary>
/// Owns a migrated Postgres test container and resets it between tests.
/// Deliberately contains no IoC container; test classes create their own system under test
/// from <see cref="CreateDbContext"/> and <see cref="GetDataSource"/>.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public sealed class PostgresDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:18.3").Build();
    private NpgsqlDataSource _dataSource = null!;
    private Respawner _respawner = null!;

    public async ValueTask InitializeAsync()
    {
        await _dbContainer.StartAsync();
        _dataSource = new NpgsqlDataSourceBuilder(_dbContainer.GetConnectionString() + ";Include Error Detail=true").Build();

        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();

        var lookupTables = db.Model.GetEntityTypes()
            .Where(x => typeof(ILookupEntity).IsAssignableFrom(x.ClrType))
            .Select(x => new Table(x.GetTableName()!));

        await using var connection = await _dataSource.OpenConnectionAsync();
        _respawner = await Respawner.CreateAsync(connection, new()
        {
            DbAdapter = DbAdapter.Postgres,
            TablesToIgnore = [new Table("__EFMigrationsHistory"), .. lookupTables]
        });
    }

    /// <summary>Creates a new <see cref="DialogDbContext"/>. The caller owns and must dispose it.</summary>
    internal DialogDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<DialogDbContext>()
            .UseNpgsql(_dataSource, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors()
            .Options);

    public NpgsqlDataSource GetDataSource() => _dataSource;

    public async ValueTask ResetState()
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await _respawner.ResetAsync(connection);
    }

    public async ValueTask DisposeAsync()
    {
        await _dataSource.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }
}
