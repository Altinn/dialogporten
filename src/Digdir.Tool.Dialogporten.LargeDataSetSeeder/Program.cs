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
   .Build()
   .Get<Settings>()
    ?? throw new InvalidOperationException("Could not get settings from environment variables");
settings.Validate();
var (connectionString, _, dialogAmount, startingDate, endDate) = settings;

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

static async Task GenerateDataUsingGeneratorsSafe(NpgsqlDataSource npgsqlDataSource, DateTimeOffset dateTimeOffset,
    DateTimeOffset endDate1, int i)
{
    await using var actorWriter = await PostgresCopyWriter<Actor>.Create(npgsqlDataSource);
    await using var attachmentWriter = await PostgresCopyWriter<Attachment>.Create(npgsqlDataSource);
    await using var attachmentUrlWriter = await PostgresCopyWriter<AttachmentUrl>.Create(npgsqlDataSource);
    await using var dialogWriter = await PostgresCopyWriter<Dialog>.Create(npgsqlDataSource);
    await using var dialogActivityWriter = await PostgresCopyWriter<DialogActivity>.Create(npgsqlDataSource);
    await using var dialogContentWriter = await PostgresCopyWriter<DialogContent>.Create(npgsqlDataSource);
    await using var dialogEndUserContextWriter = await PostgresCopyWriter<DialogEndUserContext>.Create(npgsqlDataSource);
    await using var dialogEndUserContextSystemLabelWriter = await PostgresCopyWriter<DialogEndUserContextSystemLabel>.Create(npgsqlDataSource);
    await using var dialogGuiActionWriter = await PostgresCopyWriter<DialogGuiAction>.Create(npgsqlDataSource);
    await using var dialogSearchTagWriter = await PostgresCopyWriter<DialogSearchTag>.Create(npgsqlDataSource);
    await using var dialogSeenLogWriter = await PostgresCopyWriter<DialogSeenLog>.Create(npgsqlDataSource);
    await using var dialogServiceOwnerContextWriter = await PostgresCopyWriter<DialogServiceOwnerContext>.Create(npgsqlDataSource);
    await using var dialogServiceOwnerLabelWriter = await PostgresCopyWriter<DialogServiceOwnerLabel>.Create(npgsqlDataSource);
    await using var dialogTransmissionWriter = await PostgresCopyWriter<DialogTransmission>.Create(npgsqlDataSource);
    await using var dialogTransmissionContentWriter = await PostgresCopyWriter<DialogTransmissionContent>.Create(npgsqlDataSource);
    await using var labelAssignmentLogWriter = await PostgresCopyWriter<LabelAssignmentLog>.Create(npgsqlDataSource);
    await using var localizationWriter = await PostgresCopyWriter<Localization>.Create(npgsqlDataSource);
    await using var localizationSetWriter = await PostgresCopyWriter<LocalizationSet>.Create(npgsqlDataSource);

    await Task.WhenAll(
        Wrap(actorWriter.WriteRecords(Actor.GenerateEntities(GenerateTimestamps()))),
        Wrap(attachmentWriter.WriteRecords(Attachment.GenerateEntities(GenerateTimestamps()))),
        Wrap(attachmentUrlWriter.WriteRecords(AttachmentUrl.GenerateEntities(GenerateTimestamps()))),
        Wrap(dialogWriter.WriteRecords(Dialog.GenerateEntities(GenerateTimestamps()))),
        Wrap(dialogActivityWriter.WriteRecords(DialogActivity.GenerateEntities(GenerateTimestamps()))),
        Wrap(dialogContentWriter.WriteRecords(DialogContent.GenerateEntities(GenerateTimestamps()))),
        Wrap(dialogEndUserContextWriter.WriteRecords(DialogEndUserContext.GenerateEntities(GenerateTimestamps()))),
        Wrap(dialogEndUserContextSystemLabelWriter.WriteRecords(DialogEndUserContextSystemLabel.GenerateEntities(GenerateTimestamps()))),
        Wrap(dialogGuiActionWriter.WriteRecords(DialogGuiAction.GenerateEntities(GenerateTimestamps()))),
        Wrap(dialogSearchTagWriter.WriteRecords(DialogSearchTag.GenerateEntities(GenerateTimestamps()))),
        Wrap(dialogSeenLogWriter.WriteRecords(DialogSeenLog.GenerateEntities(GenerateTimestamps()))),
        Wrap(dialogServiceOwnerContextWriter.WriteRecords(DialogServiceOwnerContext.GenerateEntities(GenerateTimestamps()))),
        Wrap(dialogServiceOwnerLabelWriter.WriteRecords(DialogServiceOwnerLabel.GenerateEntities(GenerateTimestamps()))),
        Wrap(dialogTransmissionWriter.WriteRecords(DialogTransmission.GenerateEntities(GenerateTimestamps()))),
        Wrap(dialogTransmissionContentWriter.WriteRecords(DialogTransmissionContent.GenerateEntities(GenerateTimestamps()))),
        Wrap(labelAssignmentLogWriter.WriteRecords(LabelAssignmentLog.GenerateEntities(GenerateTimestamps()))),
        Wrap(localizationWriter.WriteRecords(Localization.GenerateEntities(GenerateTimestamps()))),
        Wrap(localizationSetWriter.WriteRecords(LocalizationSet.GenerateEntities(GenerateTimestamps())))
    );
    // await actorWriter.WriteRecords(Actor.GenerateEntities(GenerateTimestamps()));
    // await attachmentWriter.WriteRecords(Attachment.GenerateEntities(GenerateTimestamps()));
    // await attachmentUrlWriter.WriteRecords(AttachmentUrl.GenerateEntities(GenerateTimestamps()));
    // await dialogWriter.WriteRecords(Dialog.GenerateEntities(GenerateTimestamps()));
    // await dialogActivityWriter.WriteRecords(DialogActivity.GenerateEntities(GenerateTimestamps()));
    // await dialogContentWriter.WriteRecords(DialogContent.GenerateEntities(GenerateTimestamps()));
    // await dialogEndUserContextWriter.WriteRecords(DialogEndUserContext.GenerateEntities(GenerateTimestamps()));
    // await dialogEndUserContextSystemLabelWriter.WriteRecords(DialogEndUserContextSystemLabel.GenerateEntities(GenerateTimestamps()));
    // await dialogGuiActionWriter.WriteRecords(DialogGuiAction.GenerateEntities(GenerateTimestamps()));
    // await dialogSearchTagWriter.WriteRecords(DialogSearchTag.GenerateEntities(GenerateTimestamps()));
    // await dialogSeenLogWriter.WriteRecords(DialogSeenLog.GenerateEntities(GenerateTimestamps()));
    // await dialogServiceOwnerContextWriter.WriteRecords(DialogServiceOwnerContext.GenerateEntities(GenerateTimestamps()));
    // await dialogServiceOwnerLabelWriter.WriteRecords(DialogServiceOwnerLabel.GenerateEntities(GenerateTimestamps()));
    // await dialogTransmissionWriter.WriteRecords(DialogTransmission.GenerateEntities(GenerateTimestamps()));
    // await dialogTransmissionContentWriter.WriteRecords(DialogTransmissionContent.GenerateEntities(GenerateTimestamps()));
    // await labelAssignmentLogWriter.WriteRecords(LabelAssignmentLog.GenerateEntities(GenerateTimestamps()));
    // await localizationWriter.WriteRecords(Localization.GenerateEntities(GenerateTimestamps()));
    // await localizationSetWriter.WriteRecords(LocalizationSet.GenerateEntities(GenerateTimestamps()));
    return;

    IEnumerable<DialogTimestamp> GenerateTimestamps()
    {
        return DialogTimestamp.Generate(dateTimeOffset, endDate1, i);
    }

    Task Wrap(Task task)
    {
        return Task.Run(async () => await task);
    }
}

static async Task GenerateDataUsingGenerators(NpgsqlDataSource npgsqlDataSource, DateTimeOffset dateTimeOffset, DateTimeOffset endDate1, int i)
{
    var entityGeneratorSeeder = new PostgresCopyWriterCoordinator(npgsqlDataSource);
    await entityGeneratorSeeder.Handle(dateTimeOffset, endDate1, i);
}
