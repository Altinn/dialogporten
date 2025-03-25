using System.Diagnostics;
using Digdir.Tool.Dialogporten.LargeDataSetGenerator;
using Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;
using Npgsql;
using Activity = Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators.Activity;

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

    Console.WriteLine($"Connection string: {MaskConnectionString(connString!)}");
    Console.WriteLine($"Starting date: {startingDate}");
    Console.WriteLine($"End date: {endDate}");
    Console.WriteLine($"Dialog amount: {dialogAmount}");

    var totalDialogCreatedStartTimestamp = Stopwatch.GetTimestamp();

    await using var dataSource = NpgsqlDataSource.Create(connString!);
    var dialogsDto = new SeedDatabaseDto(startingDate, endDate, dialogAmount);

    const int taskRetryDelayInMs = 10000;
    const int taskRetryLimit = 1000;
    const int logThreshold = 500_000;


    var actorFetchStartTimestamp = Stopwatch.GetTimestamp();
    await ActorName.FetchInsertedActorNames();
    Console.WriteLine($"Fetched {ActorName.InsertedActorNames.Count} actor names in {Stopwatch.GetElapsedTime(actorFetchStartTimestamp)}");

    var actorNameTasks = new List<Task>();
    CreateCopyTasks(new CopyTaskDto(ActorName.Generate, "actor names", ActorName.CopyCommand, NumberOfTasks: 20), actorNameTasks);
    await Task.WhenAll(actorNameTasks);

    var tasks = new List<Task>();
    void CreateCopyTasks(CopyTaskDto copyTaskDto, List<Task> taskList)
    {
        for (var splitIndex = 0; splitIndex < copyTaskDto.NumberOfTasks; splitIndex++)
        {
            RunCopyTask(copyTaskDto, splitIndex, taskList);
        }
    }

    void RunCopyTask(CopyTaskDto copyTaskDto, int splitIndex, List<Task> taskList)
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

    async Task<int> ConnectAndAttemptInsert(CopyTaskDto copyTaskDto, int splitIndex, int currentCounter)
    {
        try
        {
            await using var dbConnection = await dataSource.OpenConnectionAsync();
            await using var writer = dbConnection.BeginTextImport(copyTaskDto.CopyCommand);

            return await AttemptInsert(copyTaskDto, splitIndex, writer, dbConnection, currentCounter);
        }
        catch (Exception e)
        {
            LogDatabaseError(e);

            await Task.Delay(taskRetryDelayInMs);
            return ++currentCounter;
        }
    }

    async Task<int> AttemptInsert(CopyTaskDto copyTaskDto,
        int splitIndex,
        TextWriter textWriter,
        NpgsqlConnection npgsqlConnection,
        int currentCounter)
    {
        try
        {
            var splitLogThreshold = logThreshold / copyTaskDto.NumberOfTasks;

            await InsertData(copyTaskDto, splitIndex, textWriter, splitLogThreshold);

            textWriter.Close();

            // Done, break out of the retry loop
            return taskRetryLimit;
        }
        catch (Exception e)
        {
            LogInsertError(copyTaskDto, splitIndex, e);

            npgsqlConnection.Close();

            await Task.Delay(taskRetryDelayInMs);
            return ++currentCounter;
        }
    }

    async Task InsertData(CopyTaskDto copyTaskDto, int splitIndex, TextWriter textWriter, int splitLogThreshold)
    {
        foreach (var timestamp in dialogsDto.GetDialogTimestamps(copyTaskDto.NumberOfTasks, splitIndex))
        {
            var data = copyTaskDto.Generator(timestamp);
            if (string.IsNullOrWhiteSpace(data))
            {
                continue;
            }

            if (copyTaskDto.SingleLinePerTimestamp)
            {
                await textWriter.WriteLineAsync(data);
            }
            else
            {
                await textWriter.WriteAsync(data);
            }

            if (timestamp.DialogCounter % logThreshold == 0)
            {
                Console.WriteLine(
                    $"Inserted {splitLogThreshold} dialogs worth of {copyTaskDto.EntityName} " +
                    $"(split {splitIndex + 1}/{copyTaskDto.NumberOfTasks}), counter at {timestamp.DialogCounter}");
            }
        }
    }

    // Localizations, 28 lines per dialog
    CreateCopyTasks(new CopyTaskDto(Localization.Generate, "localizations", Localization.CopyCommand, NumberOfTasks: 12), tasks);

    // LocalizationSets, 14 lines per dialog
    CreateCopyTasks(new CopyTaskDto(LocalizationSet.Generate, "localization sets", LocalizationSet.CopyCommand, NumberOfTasks: 7), tasks);

    // AttachmentUrls, 6 lines per dialog
    CreateCopyTasks(new CopyTaskDto(AttachmentUrl.Generate, "attachment URLs", AttachmentUrl.CopyCommand, NumberOfTasks: 6), tasks);

    // Actors, 5 lines per dialog
    CreateCopyTasks(new CopyTaskDto(Actor.Generate, "actors", Actor.CopyCommand, NumberOfTasks: 4), tasks);

    // TransmissionContent, 4 lines per dialog
    CreateCopyTasks(new CopyTaskDto(TransmissionContent.Generate, "transmission content", TransmissionContent.CopyCommand, NumberOfTasks: 2), tasks);

    // No split, 2-3 lines per dialog
    CreateCopyTasks(new CopyTaskDto(DialogContent.Generate, "dialog content", DialogContent.CopyCommand), tasks);
    CreateCopyTasks(new CopyTaskDto(Transmission.Generate, "transmissions", Transmission.CopyCommand), tasks);
    CreateCopyTasks(new CopyTaskDto(GuiAction.Generate, "dialog gui actions", GuiAction.CopyCommand), tasks);
    CreateCopyTasks(new CopyTaskDto(Activity.Generate, "activities", Activity.CopyCommand), tasks);
    CreateCopyTasks(new CopyTaskDto(Attachment.Generate, "attachments", Attachment.CopyCommand), tasks);
    CreateCopyTasks(new CopyTaskDto(SearchTags.Generate, "search tags", SearchTags.CopyCommand), tasks);

    // Single line per dialog
    CreateCopyTasks(new CopyTaskDto(SeenLog.Generate, "seen logs", SeenLog.CopyCommand, SingleLinePerTimestamp: true), tasks);
    CreateCopyTasks(new CopyTaskDto(EndUserContext.Generate, "end user contexts", EndUserContext.CopyCommand, SingleLinePerTimestamp: true), tasks);
    CreateCopyTasks(new CopyTaskDto(Dialog.Generate, "dialogs", Dialog.CopyCommand, SingleLinePerTimestamp: true), tasks);

    await Task.WhenAll(tasks);

    Console.WriteLine(string.Empty);
    Console.WriteLine(string.Empty);

    var timeItTook = Stopwatch.GetElapsedTime(totalDialogCreatedStartTimestamp);
    Console.WriteLine($"Generated {dialogAmount} in {timeItTook}");


    void LogInsertError(CopyTaskDto copyTaskDto1, int i, Exception exception)
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
