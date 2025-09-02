using System.Diagnostics;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.HorizontalDataLoaders;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.Validators;
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

// Scuffed validation
await Scuffed(connectionString);

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

static async Task GenerateDataUsingGenerators(NpgsqlDataSource npgsqlDataSource, DateTimeOffset dateTimeOffset,
    DateTimeOffset endDate1, int i)
{
    var entityGeneratorSeeder = new PostgresCopyWriterCoordinator(npgsqlDataSource, EvenTypeDistributor.Instance);
    await entityGeneratorSeeder.Handle(dateTimeOffset, endDate1, i);
}

static async Task Scuffed(string connectionString)
{
    var timestamp = Stopwatch.GetTimestamp();
    Console.WriteLine("Starting scuffed validation...");
    await using var db = new DialogDbContext(options: new DbContextOptionsBuilder<DialogDbContext>()
        .UseNpgsql(connectionString: connectionString).Options);
    Console.WriteLine($"[DbContext init] Time taken: {Stopwatch.GetElapsedTime(timestamp)}");
    var dialogIds = await db.Dialogs.Select(selector: d => d.Id).Take(count: 1000).ToListAsync();
    Console.WriteLine($"[Fetch dialog ids] Time taken: {Stopwatch.GetElapsedTime(timestamp)}");

    var localizationValidator = new LocalizationDtosValidator();
    var actorValidator = new ActorValidator();
    var validator = new CreateDialogDtoValidator(
        transmissionValidator: new CreateDialogDialogTransmissionDtoValidator(
            actorValidator: actorValidator,
            contentValidator: new CreateDialogDialogTransmissionContentDtoValidator(user: null)!,
            attachmentValidator: new CreateDialogTransmissionAttachmentDtoValidator(
                localizationsValidator: localizationValidator,
                urlValidator: new CreateDialogTransmissionAttachmentUrlDtoValidator())),
        attachmentValidator: new CreateDialogDialogAttachmentDtoValidator(
            localizationsValidator: localizationValidator,
            urlValidator: new CreateDialogDialogAttachmentUrlDtoValidator()),
        guiActionValidator: new CreateDialogDialogGuiActionDtoValidator(
            localizationsValidator: localizationValidator),
        apiActionValidator: new CreateDialogDialogApiActionDtoValidator(
            apiActionEndpointValidator: new CreateDialogDialogApiActionEndpointDtoValidator()),
        activityValidator: new CreateDialogDialogActivityDtoValidator(
            localizationsValidator: localizationValidator,
            actorValidator: actorValidator),
        searchTagValidator: new CreateDialogSearchTagDtoValidator(),
        contentValidator: new CreateDialogContentDtoValidator(null),
        serviceOwnerContextValidator: new CreateDialogServiceOwnerContextDtoValidator(
            serviceOwnerLabelValidator: new CreateDialogServiceOwnerLabelDtoValidator())!);

    foreach (var dialogId in dialogIds)
    {
        Console.WriteLine($"[Fetch dialog id] Id: {dialogId}");
        var dataLoader = new FullDialogAggregateDataLoader(dialogDbContext: db,
            userResourceRegistry: ThroughThePowerOfScuff.Instance);

        var dialog = await dataLoader.LoadDialogEntity(dialogId: dialogId, cancellationToken: CancellationToken.None);
        Console.WriteLine($"[Load dialog aggregate] Time taken: {Stopwatch.GetElapsedTime(timestamp)}");

        var createDialog = dialog!.ToCreateDto();
        var result = await validator.ValidateAsync(instance: createDialog);
        if (!result.IsValid)
        {
            Console.WriteLine("fuqd dialog");
            Console.WriteLine(string.Join(separator: ", ", values: result.Errors.Select(x => $"{x.PropertyName}: {x.ErrorMessage}")));
            Console.WriteLine();
        }
        else
        {
            Console.WriteLine("al guud");
        }
        Console.WriteLine($"[Validate dialog dto] Time taken: {Stopwatch.GetElapsedTime(timestamp)}");
    }
}

internal sealed class ThroughThePowerOfScuff : IUserResourceRegistry
{
    public static ThroughThePowerOfScuff Instance { get; } = new();

    private ThroughThePowerOfScuff() { }

    public Task<bool> CurrentUserIsOwner(string serviceResource, CancellationToken cancellationToken) =>
        Task.FromResult(true);

    public Task<IReadOnlyCollection<string>> GetCurrentUserResourceIds(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<string>>([]);

    public bool UserCanModifyResourceType(string serviceResourceType) => true;

    public bool IsCurrentUserServiceOwnerAdmin() => true;
}
