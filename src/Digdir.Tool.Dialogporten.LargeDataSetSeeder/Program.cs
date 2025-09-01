using System.Diagnostics;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using Digdir.Tool.Dialogporten.LargeDataSetSeeder;
using Digdir.Tool.Dialogporten.LargeDataSetSeeder.Common;
using Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;
using Digdir.Tool.Dialogporten.LargeDataSetSeeder.PostgresWriters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
#pragma warning disable CS8321 // Local function is declared but never used

var settings = new ConfigurationBuilder()
   .AddEnvironmentVariables()
   .AddUserSecrets<Program>()
   .Build()
   .Get<Settings>()
    ?? throw new InvalidOperationException("Could not get settings from environment variables");
settings.Validate();
var (connectionString, _, dialogAmount, startingDate, endDate) = settings;
Settings.DialogAmount_S = dialogAmount;

await using var dataSource = NpgsqlDataSource.Create(connectionString);

await EnsureFreshDb(connectionString);

// Remove db constraints
await DisableDbConstraints(dataSource);

// Seed actor names
await GenerateActorNames(dataSource);

// Seed everything else
var timestamp = Stopwatch.GetTimestamp();
await GenerateDataUsingGenerators(dataSource, startingDate, endDate, dialogAmount);
Console.WriteLine($"[GenerateDataUsingGenerators] Time taken: {Stopwatch.GetElapsedTime(timestamp)}");

// Add db constraints
timestamp = Stopwatch.GetTimestamp();
await EnableDbConstraints(dataSource);
Console.WriteLine($"[EnablingDbConstraints] Time taken: {Stopwatch.GetElapsedTime(timestamp)}");

return;

static async Task EnsureFreshDb(string connectionString)
{
    await using var db = new DialogDbContext(new DbContextOptionsBuilder<DialogDbContext>()
        .UseNpgsql(connectionString).Options);
    await db.Database.EnsureDeletedAsync();
    await db.Database.EnsureCreatedAsync();
}

static async Task GenerateActorNames(NpgsqlDataSource dataSource)
{
    await using var actorNameWriter = await PostgresCopyWriter<ActorName>.Create(dataSource);
    await actorNameWriter.WriteRecords(ActorName.Values);
}

static async Task DisableDbConstraints(NpgsqlDataSource dataSource)
{
    await using var cmd = dataSource.CreateCommand(Sql.DisableAllIndexesConstraints);
    await cmd.ExecuteNonQueryAsync();
}

static async Task EnableDbConstraints(NpgsqlDataSource dataSource)
{
    await using var conn = await dataSource.OpenConnectionAsync();
    conn.Notice += (_, e) => Console.WriteLine($"[Postgres Notice] {e.Notice.MessageText}");
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = Sql.EnableAllIndexesConstraints;
    await cmd.ExecuteNonQueryAsync();
}

static async Task GenerateDataUsingGenerators(NpgsqlDataSource npgsqlDataSource, DateTimeOffset dateTimeOffset, DateTimeOffset endDate1, int i)
{
    var entityGeneratorSeeder = new PostgresCopyWriterCoordinator(npgsqlDataSource, EvenTypeDistributor.Instance);
    await entityGeneratorSeeder.Handle(dateTimeOffset, endDate1, i);
}
