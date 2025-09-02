using System.Diagnostics;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using Digdir.Tool.Dialogporten.LargeDataSetSeeder;
using Digdir.Tool.Dialogporten.LargeDataSetSeeder.Common;
using Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;
using Digdir.Tool.Dialogporten.LargeDataSetSeeder.Infrastructure;
using Digdir.Tool.Dialogporten.LargeDataSetSeeder.PostgresWriters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Refit;

#pragma warning disable CS8321 // Local function is declared but never used

var settings = new ConfigurationBuilder()
   .AddEnvironmentVariables()
   .AddUserSecrets<Program>()
   .Build()
   .Get<Settings>()
    ?? throw new InvalidOperationException("Could not get settings from environment variables");
settings.Validate();
var (connectionString, _, dialogAmount, startingDate, endDate, altinnPlatformBaseUrl) = settings;
StaticStore.DialogAmount = dialogAmount;

await using var dataSource = NpgsqlDataSource.Create(connectionString);

await EnsureFreshDb(connectionString, altinnPlatformBaseUrl);

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

static async Task EnsureFreshDb(string connectionString, string altinnPlatformBaseUrl)
{
    await using var db = new DialogDbContext(new DbContextOptionsBuilder<DialogDbContext>()
        .UseNpgsql(connectionString).Options);
    var subjectResources = await db.SubjectResources
        .AsNoTracking()
        .ToListAsync();
    await db.Database.EnsureDeletedAsync();
    await db.Database.EnsureCreatedAsync();
    db.SubjectResources.AddRange(subjectResources);
    await db.SaveChangesAsync();

    // TODO: Oh god, does this really have to be here? 
    using var httpClient = new HttpClient();
    httpClient.BaseAddress = new Uri(altinnPlatformBaseUrl);
    var refitClient = RestService.For<IResourceRegistry>(httpClient);
    var resources = await refitClient.GetResources();
    var (dagls, privs) = resources
        .GroupJoin(subjectResources.Where(x => x.Subject is "urn:altinn:rolecode:dagl" or "urn:altinn:rolecode:priv"),
            x => x.Identifier, x => x.Resource,
            (dto, resources) => new Resource(
                dto.Identifier,
                dto.ResourceType,
                dto.HasCompetentAuthority.Orgcode,
                resources.Select(r => r.Subject)))
        .Aggregate((Dagls: new List<Resource>(), Privs: new List<Resource>()), (acc, resource) =>
        {
            if (resource.HasDagl) acc.Dagls.Add(resource);
            if (resource.HasPriv) acc.Privs.Add(resource);
            return acc;
        });
    StaticStore.DaglResources = dagls.ToArray();
    StaticStore.PrivResources = privs.ToArray();
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
