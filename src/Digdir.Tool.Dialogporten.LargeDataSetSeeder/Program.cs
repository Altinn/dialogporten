using System.Diagnostics;
using System.Text;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.HorizontalDataLoaders;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.Validators;
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
var (connectionString, dialogAmount, startingDate, endDate, altinnPlatformBaseUrl) = settings;

await using var dataSource = NpgsqlDataSource.Create(connectionString);

await StaticStore.Init(connectionString, altinnPlatformBaseUrl, dialogAmount);

await EnsureFreshDb(connectionString);

await DisableDbConstraints(dataSource);

await GenerateActorNames(dataSource);

// Seed everything else
await GenerateDataUsingGenerators(dataSource, startingDate, endDate, dialogAmount);

await EnableDbConstraints(dataSource);

await ScuffedValidation(connectionString);

return;

static async Task EnsureFreshDb(string connectionString)
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
    const string sql = """
                       SELECT create_script
                       FROM constraint_index_backup
                       ORDER BY priority;
                       """;
    var outerTimestamp = Stopwatch.GetTimestamp();
    var createScripts = new List<string>();
    await using (var cmd = dataSource.CreateCommand(sql))
    await using (var reader = await cmd.ExecuteReaderAsync())
    {
        while (await reader.ReadAsync())
        {
            createScripts.Add(reader.GetString(0));
        }
    }

    foreach (var (createScript, index) in createScripts.Select((s, i) => (s, i)))
    {
        Console.WriteLine($"[EnableDbConstraints] Loop {index + 1} of {createScripts.Count}:");
        var timestamp = Stopwatch.GetTimestamp();
        var extendedCreateScript = $"""
                   BEGIN;
                   SET LOCAL maintenance_work_mem = '2GB';
                   {createScript};
                   COMMIT;
                   """;
        await using var cmd = dataSource.CreateCommand(extendedCreateScript);
        await cmd.ExecuteNonQueryAsync();
        Console.WriteLine($"[EnableDbConstraints] Executed script in {Stopwatch.GetElapsedTime(timestamp)}: {createScript}");
    }

    Console.WriteLine("[EnableDbConstraints] Starting ANALYZE on all tables to update statistics...");
    var analyzeTimestamp = Stopwatch.GetTimestamp();
    await using var analyzeCmd = dataSource.CreateCommand("ANALYZE;");
    await analyzeCmd.ExecuteNonQueryAsync();
    Console.WriteLine($"[EnableDbConstraints] ANALYZE completed in {Stopwatch.GetElapsedTime(analyzeTimestamp)}");

    Console.WriteLine($"[EnableDbConstraints] Total time taken: {Stopwatch.GetElapsedTime(outerTimestamp)}");
}

static async Task GenerateDataUsingGenerators(NpgsqlDataSource npgsqlDataSource, DateTimeOffset dateTimeOffset,
    DateTimeOffset endDate1, int i)
{
    var timestamp = Stopwatch.GetTimestamp();
    var entityGeneratorSeeder = new PostgresCopyWriterCoordinator(npgsqlDataSource, new ConstantTypeDistributor(1));
    await entityGeneratorSeeder.Handle(dateTimeOffset, endDate1, i);
    Console.WriteLine($"[GenerateDataUsingGenerators] Time taken: {Stopwatch.GetElapsedTime(timestamp)}");
}

static async Task ScuffedValidation(string connectionString)
{
    var timestamp = Stopwatch.GetTimestamp();
    Console.WriteLine("[ScuffedValidation]: Starting...");

    var dialogIds = Array.Empty<Guid>();
    await using (var db = new DialogDbContext(options: new DbContextOptionsBuilder<DialogDbContext>()
                     .UseNpgsql(connectionString: connectionString).Options))
    {
        dialogIds = await db.Dialogs.Select(selector: d => d.Id).Take(count: 100).ToArrayAsync();
    }

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

    var allErrors = new List<FluentValidation.Results.ValidationFailure>();
    foreach (var dialogId in dialogIds)
    {
        await using var db = new DialogDbContext(options: new DbContextOptionsBuilder<DialogDbContext>()
            .UseNpgsql(connectionString: connectionString).Options);
        var dataLoader = new FullDialogAggregateDataLoader(dialogDbContext: db,
            userResourceRegistry: ThroughThePowerOfScuff.Instance);
        var dialog = await dataLoader.LoadDialogEntity(dialogId: dialogId, cancellationToken: CancellationToken.None);

        var createDialog = dialog!.ToCreateDto();
        var result = await validator.ValidateAsync(instance: createDialog);
        allErrors.AddRange(result.Errors);
    }

    Console.WriteLine($"[ScuffedValidation] Time taken: {Stopwatch.GetElapsedTime(timestamp)}");
    Console.WriteLine($"[ScuffedValidation] Found {allErrors.Count} validation errors in {dialogIds.Length} dialogs.");
    var errorReport = allErrors
        .GroupBy(x => x.PropertyName)
        .Aggregate(
            new StringBuilder(),
            (sb, propertyNameGroup) => sb.AppendLine($"{propertyNameGroup.Key}: {string.Join(Environment.NewLine, propertyNameGroup)}"),
            sb => sb.ToString());
    Console.WriteLine(errorReport);
}
