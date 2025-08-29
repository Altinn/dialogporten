using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using Digdir.Tool.Dialogporten.LargeDataSetSeeder;
using Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;
using Digdir.Tool.Dialogporten.LargeDataSetSeeder.FileImport;
using Digdir.Tool.Dialogporten.LargeDataSetSeeder.PostgresWriters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

var settings = new ConfigurationBuilder()
   .AddEnvironmentVariables()
   .Build()
   .Get<Settings>()
    ?? throw new InvalidOperationException("Could not get settings from environment variables");

settings.Validate();
var (connectionString, _, dialogAmount, startingDate, endDate) = settings;
ValidateRequiredFilesAndValues();
await EnsureFreshDb(connectionString);

await using var dataSource = NpgsqlDataSource.Create(connectionString);

// Remove db constraints
await DisableDbConstraints(dataSource);

// Seed actor names
await GenerateActorNames(dataSource);

// Seed everything else
await GenerateDataUsingGenerators(dataSource, startingDate, endDate, dialogAmount);

// Add db constraints
await EnableDbConstraints(dataSource);

return;

static async Task EnableDbConstraints(NpgsqlDataSource dataSource)
{
    await using var cmd = dataSource.CreateCommand(Sql.EnableAllIndexesConstraints);
    await cmd.ExecuteNonQueryAsync();
}

static async Task DisableDbConstraints(NpgsqlDataSource dataSource)
{
    await using var cmd = dataSource.CreateCommand(Sql.DisableAllIndexesConstraints);
    await cmd.ExecuteNonQueryAsync();
}

static void ValidateRequiredFilesAndValues()
{
    if (!File.Exists("./parties"))
    {
        throw new InvalidOperationException("No file 'parties' found");
    }

    // Console.WriteLine($"Using {Parties.List.Length} parties from ./parties");

    if (!File.Exists("./service_resources"))
    {
        throw new InvalidOperationException("No file 'service_resources' found");
    }

    if (Words.Norwegian.Length < 2)
    {
        Console.Error.WriteLine("Too few words in wordlist_no, need to be more than 2 (pref. much more), exiting...");
        Environment.Exit(1);
    }

    if (Words.English.Length < 2)
    {
        Console.Error.WriteLine("Too few words in wordlist_en, need to be more than 2 (pref. much more), exiting...");
        Environment.Exit(1);
    }
}

static async Task GenerateActorNames(NpgsqlDataSource dataSource)
{
    await using var actorNameWriter = await PostgresCopyWriter<ActorName>.Create(dataSource);
    await actorNameWriter.WriteRecords(ActorName.Values);
}

static async Task EnsureFreshDb(string connectionString)
{
    await using var db = new DialogDbContext(new DbContextOptionsBuilder<DialogDbContext>()
        .UseNpgsql(connectionString).Options);
    await db.Database.EnsureDeletedAsync();
    await db.Database.EnsureCreatedAsync();
}

static async Task GenerateDataUsingGenerators(NpgsqlDataSource npgsqlDataSource, DateTimeOffset dateTimeOffset, DateTimeOffset endDate1, int i)
{
    var entityGeneratorSeeder = new PostgresCopyWriterCoordinator(npgsqlDataSource);
    await entityGeneratorSeeder.Handle(dateTimeOffset, endDate1, i);
}
