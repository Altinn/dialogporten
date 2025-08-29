using System.Diagnostics;
using Digdir.Tool.Dialogporten.LargeDataSetSeeder;
using Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;
using Digdir.Tool.Dialogporten.LargeDataSetSeeder.PostgresWriters;
using Npgsql;

try
{
    var logicalProcessorCount = Environment.ProcessorCount;
    Console.WriteLine($"Logical Processor Count: {logicalProcessorCount}");
    Console.WriteLine("Starting large data set generator...");

    if (!File.Exists("./parties"))
    {
        Console.Error.WriteLine("No file 'parties' found, exiting...");
        Environment.Exit(1);
    }

    Console.WriteLine($"Using {Parties.List.Length} parties from ./parties");

    if (!File.Exists("./service_resources"))
    {
        Console.Error.WriteLine("No file 'service_resources' found, exiting...");
        Environment.Exit(1);
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

    var connString = Environment.GetEnvironmentVariable("CONN_STRING");
    if (string.IsNullOrWhiteSpace(connString))
    {
        Console.Error.WriteLine("No connection string found, exiting...");
        Environment.Exit(1);
    }

    var startingDate = DateTimeOffset.Parse(Environment.GetEnvironmentVariable("FROM_DATE")!);
    var endDate = DateTimeOffset.Parse(Environment.GetEnvironmentVariable("TO_DATE")!);
    var dialogAmount = int.Parse(Environment.GetEnvironmentVariable("DIALOG_AMOUNT")!);

    Console.WriteLine($"Connection string: {MaskConnectionString(connString)}");
    Console.WriteLine($"Starting date: {startingDate}");
    Console.WriteLine($"End date: {endDate}");
    Console.WriteLine($"Dialog amount: {dialogAmount}");

    var totalDialogCreatedStartTimestamp = Stopwatch.GetTimestamp();

    await using var dataSource = NpgsqlDataSource.Create(connString);

    await GenerateActorNames(dataSource);

    // var lala = new PostgresCopyWriterCoordinator(dataSource);
    // await lala.Handle(startingDate, endDate, dialogAmount);

    var tasks = new List<Task>();

    await using var localizationPool = await PostgresCopyWriterPool<Localization>.Create(dataSource, 12);
    tasks.Add(localizationPool.WriteAsync(Localization.GenerateEntities(DialogTimestamp.Generate(startingDate, endDate, dialogAmount))));

    await using var actorPool = await PostgresCopyWriterPool<Actor>.Create(dataSource, 6);
    tasks.Add(actorPool.WriteAsync(Actor.GenerateEntities(DialogTimestamp.Generate(startingDate, endDate, dialogAmount))));

    await using var attachmentPool = await PostgresCopyWriterPool<Attachment>.Create(dataSource, 7);
    tasks.Add(attachmentPool.WriteAsync(Attachment.GenerateEntities(DialogTimestamp.Generate(startingDate, endDate, dialogAmount))));

    await using var attachmentUrlPool = await PostgresCopyWriterPool<AttachmentUrl>.Create(dataSource, 4);
    tasks.Add(attachmentUrlPool.WriteAsync(AttachmentUrl.GenerateEntities(DialogTimestamp.Generate(startingDate, endDate, dialogAmount))));

    await using var dialogPool = await PostgresCopyWriterPool<Dialog>.Create(dataSource, 4);
    tasks.Add(dialogPool.WriteAsync(Dialog.GenerateEntities(DialogTimestamp.Generate(startingDate, endDate, dialogAmount))));

    await using var dialogActivityPool = await PostgresCopyWriterPool<DialogActivity>.Create(dataSource, 4);
    tasks.Add(dialogActivityPool.WriteAsync(DialogActivity.GenerateEntities(DialogTimestamp.Generate(startingDate, endDate, dialogAmount))));

    await using var dialogContentPool = await PostgresCopyWriterPool<DialogContent>.Create(dataSource, 4);
    tasks.Add(dialogContentPool.WriteAsync(DialogContent.GenerateEntities(DialogTimestamp.Generate(startingDate, endDate, dialogAmount))));

    await using var dialogEndUserContextPool = await PostgresCopyWriterPool<DialogEndUserContext>.Create(dataSource, 4);
    tasks.Add(dialogEndUserContextPool.WriteAsync(DialogEndUserContext.GenerateEntities(DialogTimestamp.Generate(startingDate, endDate, dialogAmount))));

    await using var dialogEndUserContextSystemLabelPool = await PostgresCopyWriterPool<DialogEndUserContextSystemLabel>.Create(dataSource, 4);
    tasks.Add(dialogEndUserContextSystemLabelPool.WriteAsync(DialogEndUserContextSystemLabel.GenerateEntities(DialogTimestamp.Generate(startingDate, endDate, dialogAmount))));

    await using var dialogGuiActionPool = await PostgresCopyWriterPool<DialogGuiAction>.Create(dataSource, 4);
    tasks.Add(dialogGuiActionPool.WriteAsync(DialogGuiAction.GenerateEntities(DialogTimestamp.Generate(startingDate, endDate, dialogAmount))));

    await using var dialogSearchTagPool = await PostgresCopyWriterPool<DialogSearchTag>.Create(dataSource, 4);
    tasks.Add(dialogSearchTagPool.WriteAsync(DialogSearchTag.GenerateEntities(DialogTimestamp.Generate(startingDate, endDate, dialogAmount))));

    await using var dialogSeenLogPool = await PostgresCopyWriterPool<DialogSeenLog>.Create(dataSource, 4);
    tasks.Add(dialogSeenLogPool.WriteAsync(DialogSeenLog.GenerateEntities(DialogTimestamp.Generate(startingDate, endDate, dialogAmount))));

    await using var dialogServiceOwnerContextPool = await PostgresCopyWriterPool<DialogServiceOwnerContext>.Create(dataSource, 4);
    tasks.Add(dialogServiceOwnerContextPool.WriteAsync(DialogServiceOwnerContext.GenerateEntities(DialogTimestamp.Generate(startingDate, endDate, dialogAmount))));

    await using var dialogServiceOwnerLabelPool = await PostgresCopyWriterPool<DialogServiceOwnerLabel>.Create(dataSource, 4);
    tasks.Add(dialogServiceOwnerLabelPool.WriteAsync(DialogServiceOwnerLabel.GenerateEntities(DialogTimestamp.Generate(startingDate, endDate, dialogAmount))));

    await using var dialogTransmissionPool = await PostgresCopyWriterPool<DialogTransmission>.Create(dataSource, 4);
    tasks.Add(dialogTransmissionPool.WriteAsync(DialogTransmission.GenerateEntities(DialogTimestamp.Generate(startingDate, endDate, dialogAmount))));

    await using var dialogTransmissionContentPool = await PostgresCopyWriterPool<DialogTransmissionContent>.Create(dataSource, 4);
    tasks.Add(dialogTransmissionContentPool.WriteAsync(DialogTransmissionContent.GenerateEntities(DialogTimestamp.Generate(startingDate, endDate, dialogAmount))));

    await using var labelAssignmentLogPool = await PostgresCopyWriterPool<LabelAssignmentLog>.Create(dataSource, 4);
    tasks.Add(labelAssignmentLogPool.WriteAsync(LabelAssignmentLog.GenerateEntities(DialogTimestamp.Generate(startingDate, endDate, dialogAmount))));

    await using var localizationSetPool = await PostgresCopyWriterPool<LocalizationSet>.Create(dataSource, 4);
    tasks.Add(localizationSetPool.WriteAsync(LocalizationSet.GenerateEntities(DialogTimestamp.Generate(startingDate, endDate, dialogAmount))));

    await Task.WhenAll(tasks);

    //// After Task.WhenAll(tasks)
    // await localizationPool.DisposeAsync();
    // await actorPool.DisposeAsync();
    // await attachmentPool.DisposeAsync();
    // await attachmentUrlPool.DisposeAsync();
    // await dialogPool.DisposeAsync();
    // await dialogActivityPool.DisposeAsync();
    // await dialogContentPool.DisposeAsync();
    // await dialogEndUserContextPool.DisposeAsync();
    // await dialogEndUserContextSystemLabelPool.DisposeAsync();
    // await dialogGuiActionPool.DisposeAsync();
    // await dialogSearchTagPool.DisposeAsync();
    // await dialogSeenLogPool.DisposeAsync();
    // await dialogServiceOwnerContextPool.DisposeAsync();
    // await dialogServiceOwnerLabelPool.DisposeAsync();
    // await dialogTransmissionPool.DisposeAsync();
    // await dialogTransmissionContentPool.DisposeAsync();
    // await labelAssignmentLogPool.DisposeAsync();
    // await localizationSetPool.DisposeAsync();

    // // for each pool above, call public CompleteAndWaitAsync() on the pool object
    // await localizationPool.CompleteAndWaitAsync();
    // await actorPool.CompleteAndWaitAsync();
    // await attachmentPool.CompleteAndWaitAsync();
    // await attachmentUrlPool.CompleteAndWaitAsync();
    // await dialogPool.CompleteAndWaitAsync();
    // await dialogActivityPool.CompleteAndWaitAsync();
    // await dialogContentPool.CompleteAndWaitAsync();
    // await dialogEndUserContextPool.CompleteAndWaitAsync();
    // await dialogEndUserContextSystemLabelPool.CompleteAndWaitAsync();
    // await dialogGuiActionPool.CompleteAndWaitAsync();
    // await dialogSearchTagPool.CompleteAndWaitAsync();
    // await dialogSeenLogPool.CompleteAndWaitAsync();
    // await dialogServiceOwnerContextPool.CompleteAndWaitAsync();
    // await dialogServiceOwnerLabelPool.CompleteAndWaitAsync();
    // await dialogTransmissionPool.CompleteAndWaitAsync();
    // await dialogTransmissionContentPool.CompleteAndWaitAsync();
    // await labelAssignmentLogPool.CompleteAndWaitAsync();
    // await localizationSetPool.CompleteAndWaitAsync();

    // TODO: Start re-indexing command
    // Import create validator
    // Get dialog, map to create

    Console.WriteLine(string.Empty);
    Console.WriteLine(string.Empty);

    var timeItTook = Stopwatch.GetElapsedTime(totalDialogCreatedStartTimestamp);
    Console.WriteLine($"Generated {dialogAmount} in {timeItTook}");


    // void LogInsertError<T>(CopyTaskDto<T> copyTaskDto1, int i, Exception exception) where T : class
    // {
    //     Console.WriteLine();
    //     Console.WriteLine("====================================");
    //     Console.WriteLine(
    //         $"Insert for table {copyTaskDto1.EntityName} failed (split {i + 1}/{copyTaskDto1.NumberOfTasks}), retrying in {taskRetryDelayInMs}ms");
    //     Console.WriteLine(exception.Message);
    //     Console.WriteLine(exception.StackTrace);
    //     Console.WriteLine("====================================");
    //     Console.WriteLine();
    // }
    //
    // void LogDatabaseError(Exception exception)
    // {
    //     Console.WriteLine();
    //     Console.WriteLine("====================================");
    //     Console.WriteLine(
    //         $"Database setup failed, either connection or transaction, retrying in {taskRetryDelayInMs}ms");
    //     Console.WriteLine(exception.Message);
    //     Console.WriteLine(exception.StackTrace);
    //     Console.WriteLine("====================================");
    //     Console.WriteLine();
    // }

    static string MaskConnectionString(string input)
    {
        const string passwordKey = "Password=";

        var startIdx = input.IndexOf(passwordKey, StringComparison.Ordinal);
        if (startIdx == -1)
        {
            return input;
        }

        startIdx += passwordKey.Length;
        var endIdx = input.IndexOf(';', startIdx);

        return endIdx != -1
            ? input[..startIdx] + "****" + input[endIdx..]
            : input[..startIdx] + "****";
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine(ex.StackTrace);
}

static async Task GenerateActorNames(NpgsqlDataSource dataSource)
{
    await using var actorNameWriter = await PostgresCopyWriter<ActorName>.Create(dataSource);
    await actorNameWriter.WriteRecords(ActorName.Values);
}
