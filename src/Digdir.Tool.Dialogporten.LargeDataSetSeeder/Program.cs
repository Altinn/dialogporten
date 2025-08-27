using System.Diagnostics;
using Digdir.Tool.Dialogporten.LargeDataSetSeeder;
using Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;
using Digdir.Tool.Dialogporten.LargeDataSetSeeder.PostgresWriters;
using Npgsql;
using DialogActivity = Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators.DialogActivity;

// var cs = Environment.GetEnvironmentVariable("CONN_STRING")!;
// await using var ds = NpgsqlDataSource.Create(cs);
//
// await using var writer = await PostgresCopyWriter<DialogActivity>.Create(ds);
//
// await writer.WriteRecords([
//     new(Guid.CreateVersion7(), DialogActivityType.Values.DialogCreated),
//     new(Guid.CreateVersion7(), DialogActivityType.Values.DialogCreated),
//     new(Guid.CreateVersion7(), DialogActivityType.Values.DialogCreated),
// ], CancellationToken.None);

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
    var dialogsDto = new SeedDatabaseDto(startingDate, endDate, dialogAmount);

    const int taskRetryDelayInMs = 10000;
    const int taskRetryLimit = 1000;
    const int logThreshold = 500_000;

    // var actorFetchStartTimestamp = Stopwatch.GetTimestamp();
    // await ActorName.FetchInsertedActorNames();
    // Console.WriteLine($"Fetched {ActorName.InsertedActorNames.Count} actor names in {Stopwatch.GetElapsedTime(actorFetchStartTimestamp)}");
    //
    // var actorNameTasks = new List<Task>();
    // CreateCopyTasks(new CopyTaskDto(ActorName.Generate, "actor names", ActorName.CopyCommand, NumberOfTasks: 20), actorNameTasks);
    // await Task.WhenAll(actorNameTasks);


    await GenerateActorNames(dataSource);



    var tasks = new List<Task>();
    void CreateCopyTasks<T>(CopyTaskDto<T> copyTaskDto, List<Task> taskList) where T : class
    {
        for (var splitIndex = 0; splitIndex < copyTaskDto.NumberOfTasks; splitIndex++)
        {
            RunCopyTask(copyTaskDto, splitIndex, taskList);
        }
    }

    void RunCopyTask<T>(CopyTaskDto<T> copyTaskDto, int splitIndex, List<Task> taskList) where T : class
    {
        taskList.Add(Task.Run(async () =>
        {
            var startTimestamp = Stopwatch.GetTimestamp();
            var counter = 0;

            do
            {
                counter = await ConnectAndAttemptInsert(copyTaskDto, splitIndex, counter);
            } while (counter < taskRetryLimit);

            Console.WriteLine(
                $"Inserted {copyTaskDto.EntityName} (split {splitIndex + 1}/{copyTaskDto.NumberOfTasks})" +
                $" in {Stopwatch.GetElapsedTime(startTimestamp)}");
        }));
    }

    async Task<int> ConnectAndAttemptInsert<T>(CopyTaskDto<T> copyTaskDto, int splitIndex, int currentCounter) where T : class
    {
        try
        {
            // await using var dbConnection = await dataSource.OpenConnectionAsync();
            // await using var writer = dbConnection.BeginTextImport(copyTaskDto.CopyCommand);
            await using var postgresCopyWriter = await PostgresCopyWriter<T>.Create(dataSource);

            return await AttemptInsert(copyTaskDto, splitIndex, postgresCopyWriter, currentCounter);
        }
        catch (Exception e)
        {
            LogDatabaseError(e);

            await Task.Delay(taskRetryDelayInMs);
            return ++currentCounter;
        }
    }

    async Task<int> AttemptInsert<T>(CopyTaskDto<T> copyTaskDto,
        int splitIndex,
        PostgresCopyWriter<T> textWriter,
        int currentCounter)
    where T : class
    {
        try
        {
            var splitLogThreshold = logThreshold / copyTaskDto.NumberOfTasks;

            await InsertData(copyTaskDto, splitIndex, textWriter, splitLogThreshold);

            // Done, break out of the retry loop
            return taskRetryLimit;
        }
        catch (Exception e)
        {
            LogInsertError(copyTaskDto, splitIndex, e);
            await Task.Delay(taskRetryDelayInMs);
            return ++currentCounter;
        }
    }

    async Task InsertData<T>(CopyTaskDto<T> copyTaskDto, int splitIndex, PostgresCopyWriter<T> postgresWriter, int splitLogThreshold) where T : class
    {
        var data = copyTaskDto.Generator(dialogsDto.GetDialogTimestamps(copyTaskDto.NumberOfTasks, splitIndex));
        await postgresWriter.WriteRecords(data, CancellationToken.None);
        // foreach (var timestamp in dialogsDto.GetDialogTimestamps(copyTaskDto.NumberOfTasks, splitIndex))
        // {
        //     var data = copyTaskDto.Generator(timestamp);
        //     // if (string.IsNullOrWhiteSpace(data))
        //     // {
        //     //     continue;
        //     // }
        //
        //     await postgresWriter.WriteRecords(data, CancellationToken.None);
        //     // if (copyTaskDto.SingleLinePerTimestamp)
        //     // {
        //     //     await postgresWriter.WriteLineAsync(data);
        //     // }
        //     // else
        //     // {
        //     //     await postgresWriter.WriteAsync(data);
        //     // }
        //
        //     if (timestamp.DialogCounter % logThreshold == 0)
        //     {
        //         Console.WriteLine(
        //             $"Inserted {splitLogThreshold} dialogs worth of {copyTaskDto.EntityName} " +
        //             $"(split {splitIndex + 1}/{copyTaskDto.NumberOfTasks}), counter at {timestamp.DialogCounter}");
        //     }
        // }
    }

    // var magic = new Magic();

    // Localizations, 28 lines per dialog
    // CreateCopyTasks(new CopyTaskDto(Localization.Generate, "localizations", magic.MakeCopyCommand<Localization>, NumberOfTasks: 12), tasks);

    // LocalizationSets, 14 lines per dialog
    // CreateCopyTasks(new CopyTaskDto(LocalizationSet.Generate, "localization sets", LocalizationSet.CopyCommand, NumberOfTasks: 7), tasks);

    // AttachmentUrls, 6 lines per dialog
    // CreateCopyTasks(new CopyTaskDto(AttachmentUrl.Generate, "attachment URLs", AttachmentUrl.CopyCommand, NumberOfTasks: 6), tasks);

    // Actors, 5 lines per dialog
    // CreateCopyTasks(new CopyTaskDto(Actor.Generate, "actors", Actor.CopyCommand, NumberOfTasks: 4), tasks);

    // TransmissionContent, 4 lines per dialog
    // CreateCopyTasks(new CopyTaskDto(DialogTransmissionContent.Generate, "transmission content", DialogTransmissionContent.CopyCommand, NumberOfTasks: 2), tasks);

    // No split, 2-3 lines per dialog
    // CreateCopyTasks(new CopyTaskDto(DialogContent.Generate, "dialog content", DialogContent.CopyCommand), tasks);
    // CreateCopyTasks(new CopyTaskDto(DialogTransmission.Generate, "transmissions", DialogTransmission.CopyCommand), tasks);
    // CreateCopyTasks(new CopyTaskDto(DialogGuiAction.Generate, "dialog gui actions", DialogGuiAction.CopyCommand), tasks);
    CreateCopyTasks(new CopyTaskDto<DialogActivity>(DialogActivity.GenerateEntities, "activities"), tasks);
    // CreateCopyTasks(new CopyTaskDto(Attachment.Generate, "attachments", Attachment.CopyCommand), tasks);
    // CreateCopyTasks(new CopyTaskDto(DialogSearchTag.Generate, "search tags", DialogSearchTag.CopyCommand), tasks);

    // Single line per dialog
    // CreateCopyTasks(new CopyTaskDto(DialogSeenLog.Generate, "seen logs", DialogSeenLog.CopyCommand, SingleLinePerTimestamp: true), tasks);
    // CreateCopyTasks(new CopyTaskDto(DialogEndUserContext.Generate, "end user contexts", DialogEndUserContext.CopyCommand, SingleLinePerTimestamp: true), tasks);
    // CreateCopyTasks(new CopyTaskDto(Dialog.Generate, "dialogs", Dialog.CopyCommand, SingleLinePerTimestamp: true), tasks);

    await Task.WhenAll(tasks);

    // TODO: Start re-indexing command
    // Import create validator
    // Get dialog, map to create

    Console.WriteLine(string.Empty);
    Console.WriteLine(string.Empty);

    var timeItTook = Stopwatch.GetElapsedTime(totalDialogCreatedStartTimestamp);
    Console.WriteLine($"Generated {dialogAmount} in {timeItTook}");


    void LogInsertError<T>(CopyTaskDto<T> copyTaskDto1, int i, Exception exception) where T : class
    {
        Console.WriteLine();
        Console.WriteLine("====================================");
        Console.WriteLine(
            $"Insert for table {copyTaskDto1.EntityName} failed (split {i + 1}/{copyTaskDto1.NumberOfTasks}), retrying in {taskRetryDelayInMs}ms");
        Console.WriteLine(exception.Message);
        Console.WriteLine(exception.StackTrace);
        Console.WriteLine("====================================");
        Console.WriteLine();
    }

    void LogDatabaseError(Exception exception)
    {
        Console.WriteLine();
        Console.WriteLine("====================================");
        Console.WriteLine(
            $"Database setup failed, either connection or transaction, retrying in {taskRetryDelayInMs}ms");
        Console.WriteLine(exception.Message);
        Console.WriteLine(exception.StackTrace);
        Console.WriteLine("====================================");
        Console.WriteLine();
    }

    string MaskConnectionString(string input)
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
