using System.Diagnostics;
using Digdir.Tool.Dialogporten.LargeDataSetSeeder;
using Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;
using Digdir.Tool.Dialogporten.LargeDataSetSeeder.PostgresWriters;
using Npgsql;
using DialogActivity = Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators.DialogActivity;

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
    const int taskRetryDelayInMs = 10000;
    const int taskRetryLimit = 1000;
    const int logThreshold = 500_000;

    await GenerateActorNames(dataSource);

    var tasks = new List<Task>();
    // IL3050
#pragma warning disable CS8321 // Local function is declared but never used
    void CreateCopyTasks<T>(CopyTaskDto<T> copyTaskDto, List<Task> taskList) where T : class
#pragma warning restore CS8321 // Local function is declared but never used
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
        var data = copyTaskDto.Generator!(DialogTimestamp.Generate(startingDate, endDate, dialogAmount));
        await postgresWriter.WriteRecords(data, CancellationToken.None);
    }

    CreateCopyTasks(new CopyTaskDto<Actor>(Actor.GenerateEntities, "temp"), tasks);
    CreateCopyTasks(new CopyTaskDto<Attachment>(Attachment.GenerateEntities, "temp"), tasks);
    CreateCopyTasks(new CopyTaskDto<AttachmentUrl>(AttachmentUrl.GenerateEntities, "temp"), tasks);
    CreateCopyTasks(new CopyTaskDto<Dialog>(Dialog.GenerateEntities, "temp"), tasks);
    CreateCopyTasks(new CopyTaskDto<DialogActivity>(DialogActivity.GenerateEntities, "temp"), tasks);
    CreateCopyTasks(new CopyTaskDto<DialogContent>(DialogContent.GenerateEntities, "temp"), tasks);
    CreateCopyTasks(new CopyTaskDto<DialogEndUserContext>(DialogEndUserContext.GenerateEntities, "temp"), tasks);
    CreateCopyTasks(new CopyTaskDto<DialogEndUserContextSystemLabel>(DialogEndUserContextSystemLabel.GenerateEntities, "temp"), tasks);
    CreateCopyTasks(new CopyTaskDto<DialogGuiAction>(DialogGuiAction.GenerateEntities, "temp"), tasks);
    CreateCopyTasks(new CopyTaskDto<DialogSearchTag>(DialogSearchTag.GenerateEntities, "temp"), tasks);
    CreateCopyTasks(new CopyTaskDto<DialogSeenLog>(DialogSeenLog.GenerateEntities, "temp"), tasks);
    CreateCopyTasks(new CopyTaskDto<DialogServiceOwnerContext>(DialogServiceOwnerContext.GenerateEntities, "temp"), tasks);
    CreateCopyTasks(new CopyTaskDto<DialogServiceOwnerLabel>(DialogServiceOwnerLabel.GenerateEntities, "temp"), tasks);
    CreateCopyTasks(new CopyTaskDto<DialogTransmission>(DialogTransmission.GenerateEntities, "temp"), tasks);
    CreateCopyTasks(new CopyTaskDto<DialogTransmissionContent>(DialogTransmissionContent.GenerateEntities, "temp"), tasks);
    CreateCopyTasks(new CopyTaskDto<LabelAssignmentLog>(LabelAssignmentLog.GenerateEntities, "temp"), tasks);
    CreateCopyTasks(new CopyTaskDto<Localization>(Localization.GenerateEntities, "temp"), tasks);
    CreateCopyTasks(new CopyTaskDto<LocalizationSet>(LocalizationSet.GenerateEntities, "temp"), tasks);


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
