using System.Diagnostics;
using Digdir.Tool.Dialogporten.LargeDataSetGenerator;
using Npgsql;
using Activity = Digdir.Tool.Dialogporten.LargeDataSetGenerator.Activity;

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
    var startingDate = DateTimeOffset.Parse(Environment.GetEnvironmentVariable("FROM_DATE")!);
    var endDate = DateTimeOffset.Parse(Environment.GetEnvironmentVariable("TO_DATE")!);
    var dialogAmount = int.Parse(Environment.GetEnvironmentVariable("DIALOG_AMOUNT")!);

    Console.WriteLine($"Connection string: {connString}");
    Console.WriteLine($"Starting date: {startingDate}");
    Console.WriteLine($"End date: {endDate}");
    Console.WriteLine($"Dialog amount: {dialogAmount}");

    var totalDialogCreatedStartTimestamp = Stopwatch.GetTimestamp();

    await using var dataSource = NpgsqlDataSource.Create(connString!);
    var dto = new SeedDatabaseDto(startingDate, endDate, dialogAmount);
    var tasks = new List<Task>();

    const int taskRetryDelayInMs = 10000;
    const int taskRetryLimit = 1000;

    foreach (var arg in args)
    {
        Console.WriteLine(arg);
        if (arg == "RestoreIndexes")
        {
            const string primaryKeys =
                """
                DO
                $$
                DECLARE
                x RECORD;
                BEGIN
                    FOR x IN 
                SELECT create_script
                FROM constraint_index_backup
                WHERE priority IN(1,2)
                ORDER BY priority
                    LOOP
                EXECUTE x.create_script;
                END LOOP;
                END;
                $$;

                COMMIT;
                """;

            await using var primaryKeysCommand = dataSource.CreateCommand(primaryKeys);
            await primaryKeysCommand.ExecuteNonQueryAsync();
            await primaryKeysCommand.DisposeAsync();

            const string foreignKeys = "SELECT create_script FROM constraint_index_backup WHERE priority = 3";

            await using var foreignKeyCommand = dataSource.CreateCommand(foreignKeys);
            await using var foreignKeyReader = await foreignKeyCommand.ExecuteReaderAsync();

            var foreignKeyTasks = new List<Task>();

            while (await foreignKeyReader.ReadAsync())
            {
                var createConstraintCommand = foreignKeyReader.GetString(0);
                foreignKeyTasks.Add(Task.Run(async () =>
                    {
                        Console.WriteLine();
                        Console.WriteLine($"Restoring constraint: {createConstraintCommand}");

                        await using var createCommand = dataSource.CreateCommand(createConstraintCommand);
                        await createCommand.ExecuteNonQueryAsync();

                        Console.WriteLine($"Restored constraint: {createConstraintCommand}");
                        Console.WriteLine();
                    }
                ));
            }

            await Task.WhenAll(foreignKeyTasks);

            const string indexes = "SELECT create_script FROM constraint_index_backup WHERE priority = 4";

            await using var indexCommand = dataSource.CreateCommand(indexes);
            await using var indexReader = await indexCommand.ExecuteReaderAsync();

            var indexTasks = new List<Task>();

            while (await indexReader.ReadAsync())
            {
                var createIndexCommand = indexReader.GetString(0);
                indexTasks.Add(Task.Run(async () =>
                    {
                        Console.WriteLine();
                        Console.WriteLine($"Restoring index: {createIndexCommand}");

                        await using var createCommand = dataSource.CreateCommand(createIndexCommand);
                        await createCommand.ExecuteNonQueryAsync();

                        Console.WriteLine($"Restored index: {createIndexCommand}");
                        Console.WriteLine();
                    }
                ));
            }

            await Task.WhenAll(indexTasks);

            Environment.Exit(0);
        }
    }

    void CreateCopyTask(Func<DialogTimestamp, string> generator, string entityName,
        string copyCommand, bool singleLinePerTimestamp = false, int numberOfTasks = 1)
    {
        for (var i = 0; i < numberOfTasks; i++)
        {
            var splitIndex = i;
            tasks.Add(Task.Run(async () =>
            {
                var startTimestamp = Stopwatch.GetTimestamp();
                var counter = 0;

                do
                {
                    try
                    {
                        await using var dbConnection = await dataSource.OpenConnectionAsync();
                        await using var writer = dbConnection.BeginTextImport(copyCommand);

                        try
                        {
                            const int logThreshold = 500_000;
                            var splitLogThreshold = logThreshold / numberOfTasks;

                            foreach (var timestamp in dto.GetDialogTimestamps(numberOfTasks, splitIndex))
                            {
                                var data = generator(timestamp);

                                if (singleLinePerTimestamp)
                                {
                                    await writer.WriteLineAsync(data);
                                }
                                else
                                {
                                    await writer.WriteAsync(data);
                                }

                                if (timestamp.Counter % logThreshold == 0)
                                {
                                    Console.WriteLine(
                                        $"Inserted {splitLogThreshold} dialogs worth of {entityName} (split {splitIndex + 1}/{numberOfTasks}), counter at {timestamp.Counter}");
                                }
                            }

                            writer.Close();
                            counter = taskRetryLimit;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine();
                            Console.WriteLine("====================================");
                            Console.WriteLine(
                                $"Insert for table {entityName} failed (split {splitIndex + 1}/{numberOfTasks}), retrying in {taskRetryDelayInMs}ms");
                            Console.WriteLine(e.Message);
                            Console.WriteLine(e.StackTrace);
                            Console.WriteLine("====================================");
                            Console.WriteLine();

                            dbConnection.Close();

                            counter++;
                            Thread.Sleep(taskRetryDelayInMs);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine();
                        Console.WriteLine("====================================");
                        Console.WriteLine(
                            $"Database setup failed, either connection or transaction, retrying in {taskRetryDelayInMs}ms");
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e.StackTrace);
                        Console.WriteLine("====================================");
                        Console.WriteLine();

                        counter++;
                        Thread.Sleep(taskRetryDelayInMs);
                    }
                } while (counter < taskRetryLimit);

                Console.WriteLine(
                    $"Inserted {entityName} (split {splitIndex + 1}/{numberOfTasks}) in {Stopwatch.GetElapsedTime(startTimestamp)}");
            }));
        }
    }

    // Split Localizations, 28 lines per dialog
    CreateCopyTask(Localization.Generate, "localizations", Localization.CopyCommand, numberOfTasks: 12);

    // Split LocalizationSets, 14 lines per dialog
    CreateCopyTask(LocalizationSet.Generate, "localization sets", LocalizationSet.CopyCommand, numberOfTasks: 8);

    // Split AttachmentUrls, 6 lines per dialog
    CreateCopyTask(AttachmentUrl.Generate, "attachment URLs", AttachmentUrl.CopyCommand, numberOfTasks: 6);

    // Split Actors, 5 lines per dialog
    CreateCopyTask(Actor.Generate, "actors", Actor.CopyCommand, numberOfTasks: 4);

    // Split TransmissionContent, 4 lines per dialog
    CreateCopyTask(TransmissionContent.Generate, "transmission content", TransmissionContent.CopyCommand,
        numberOfTasks: 3);

    // No split, 2-3 lines per dialog
    CreateCopyTask(DialogContent.Generate, "dialog content", DialogContent.CopyCommand);
    CreateCopyTask(Transmission.Generate, "transmissions", Transmission.CopyCommand);
    CreateCopyTask(GuiAction.Generate, "dialog gui actions", GuiAction.CopyCommand);
    CreateCopyTask(Activity.Generate, "activities", Activity.CopyCommand);
    CreateCopyTask(Attachment.Generate, "attachments", Attachment.CopyCommand);
    CreateCopyTask(SearchTags.Generate, "search tags", SearchTags.CopyCommand);

    // Single line per dialog
    CreateCopyTask(SeenLog.Generate, "seen logs", SeenLog.CopyCommand, singleLinePerTimestamp: true);
    CreateCopyTask(EndUserContext.Generate, "end user contexts", EndUserContext.CopyCommand,
        singleLinePerTimestamp: true);
    CreateCopyTask(Dialog.Generate, "dialogs", Dialog.CopyCommand, singleLinePerTimestamp: true);


    await Task.WhenAll(tasks);

    Console.WriteLine(string.Empty);
    Console.WriteLine(string.Empty);

    var timeItTook = Stopwatch.GetElapsedTime(totalDialogCreatedStartTimestamp);
    Console.WriteLine($"Generated {dialogAmount} in {timeItTook}");
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine(ex.StackTrace);
}

internal static class Parties
{
    internal static readonly string[] List = File.ReadAllLines("./parties");
}

internal static class Words
{
    internal static readonly string[]
        English = File.Exists("./wordlist_en") ? File.ReadAllLines("./wordlist_en") : [];

    internal static readonly string[]
        Norwegian = File.Exists("./wordlist_no") ? File.ReadAllLines("./wordlist_no") : [];

    static Words()
    {
        if (English.Length > Norwegian.Length)
        {
            English = English.Except(Norwegian).ToArray();
        }
        else
        {
            Norwegian = Norwegian.Except(English).ToArray();
        }
    }
}
