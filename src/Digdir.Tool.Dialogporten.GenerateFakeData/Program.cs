using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Bogus;
using CommandLine;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;

namespace Digdir.Tool.Dialogporten.GenerateFakeData;

public static class Program
{
    private const int RefreshRateMs = 200; // How often the progress is updated
    private const int DialogsPerBatch = 5; // How many dialogs to generate per call DialogGenerator
    private const int BoundedCapacity = 1000; // Max number of dialogs in the queue
    private const int Consumers = 20; // Number of consumers posting to the API
    private const string FailedDirectory = "failed"; // Directory to write failed requests to
    private const string OutputDirectory = "output"; // Directory to write files to when not posting to the API

    public static async Task Main(string[] args) => await Parser.Default.ParseArguments<Options>(args).WithParsedAsync(RunAsync);

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };

    private static int _dialogCounter;
    private static readonly Stopwatch Stopwatch = new();
    private static Randomizer MyRandomizer => _threadRandomizer ??= new Randomizer();
    [ThreadStatic] private static Randomizer? _threadRandomizer;

    private static async Task RunAsync(Options options)
    {
        if (options is { Submit: false, WriteToDisk: false, Benchmark: false })
        {
            Randomizer.Seed = new Random(options.Seed);
            var dialogs = DialogGenerator.GenerateFakeDialogs(
                count: options.Count, serviceResourceGenerator: () => MaybeGetRandomResource(options),
                partyGenerator: () => MaybeGetRandomParty(options));
            var serialized = JsonSerializer.Serialize(dialogs, JsonSerializerOptions);
            Console.WriteLine(serialized);
            return;
        }

        if (options is { Submit: true, WriteToDisk: true })
        {
            Console.WriteLine("You can only choose one of --submit or --write");
            return;
        }

        if (options is { Submit: true, Benchmark: true } or { WriteToDisk: true, Benchmark: true })
        {
            Console.WriteLine("You cannot supply --submit or --write together with --benchmark");
            return;
        }

        if (options.WriteToDisk)
        {
            Directory.CreateDirectory(OutputDirectory);
        }

        var channel = Channel.CreateBounded<(int, CreateDialogDto)>(
            new BoundedChannelOptions(BoundedCapacity)
            {
                SingleWriter = false,
                SingleReader = false,
                // When the channel is full, WriteAsync will wait (backpressure).
                FullMode = BoundedChannelFullMode.Wait
            });

        var writer = channel.Writer;
        var reader = channel.Reader;

        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        using var client = new HttpClient();
        client.BaseAddress = new Uri(options.Url);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        Console.WriteLine($"Generating {options.Count} fake dialogs...");
        Stopwatch.Start();

        var producerTask = Task.Run(() => ProduceDialogs(options, writer, cancellationToken), cancellationToken);
        var progressTask = Task.Run(() => UpdateProgress(options, cancellationToken), cancellationToken);

        var consumerTasks = new List<Task>();
        for (var i = 0; i < Consumers; i++)
        {
            if (options.Submit)
            {
                // ReSharper disable once AccessToDisposedClosure
                consumerTasks.Add(Task.Run(() =>
                    ConsumeDialogsAndPost(options, reader, client, cancellationToken), cancellationToken));
            }
            else if (options.WriteToDisk)
            {
                consumerTasks.Add(Task.Run(() =>
                    ConsumeDialogsAndWriteToFile(reader, cancellationToken), cancellationToken));
            }
            else // Benchmark
            {
                consumerTasks.Add(Task.Run(() =>
                    ConsumeDialogsAndDiscards(reader, cancellationToken), cancellationToken));
            }
        }

        await producerTask;
        foreach (var task in consumerTasks)
        {
            await task;
        }

        Stopwatch.Stop();
        await progressTask;
    }

    private const double RateCalculationIntervalMilliseconds = 1000;
    private static async Task UpdateProgress(Options options, CancellationToken cancellationToken)
    {
        var lastRateElapsedMilliseconds = 0L;
        var lastRateDialogCount = 0.0;
        var rateLastPeriod = 0.0;
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_dialogCounter == 0 || Stopwatch.ElapsedMilliseconds == 0)
            {
                await Task.Delay(RefreshRateMs, cancellationToken);
                continue;
            }

            var elapsedSinceLastRateCalc = Stopwatch.ElapsedMilliseconds - lastRateElapsedMilliseconds;

            if (elapsedSinceLastRateCalc >= RateCalculationIntervalMilliseconds)
            {
                var dialogsInInterval = _dialogCounter - lastRateDialogCount;
                rateLastPeriod = dialogsInInterval / elapsedSinceLastRateCalc;
                lastRateDialogCount = _dialogCounter;
                lastRateElapsedMilliseconds = Stopwatch.ElapsedMilliseconds;
            }

            Console.Write(
                "\rProgress: {0}/{1} dialogs created, {2:F1} dialogs/second ({3:F1} dialogs/second total).",
                _dialogCounter,
                options.Count,
                rateLastPeriod * 1000,
                _dialogCounter / Stopwatch.Elapsed.TotalSeconds);

            await Task.Delay(RefreshRateMs, cancellationToken);
            if (_dialogCounter >= options.Count)
            {
                break;
            }
        }

        Console.WriteLine(
            "\r{0}/{1} dialogs created in {2:F1} seconds ({3:F1} dialogs/second).                               ",
            _dialogCounter,
            options.Count,
            Stopwatch.Elapsed.TotalSeconds,
            _dialogCounter / Stopwatch.Elapsed.TotalSeconds);
    }

    private static async Task ProduceDialogs(Options options, ChannelWriter<(int, CreateDialogDto)> writer, CancellationToken cancellationToken)
    {
        var totalDialogs = options.Count;
        var dialogCounter = 0;

        try
        {
            while (dialogCounter < totalDialogs && !cancellationToken.IsCancellationRequested)
            {
                var dialogsToGenerate = Math.Min(DialogsPerBatch, totalDialogs - dialogCounter);
                var dialogs = DialogGenerator.GenerateFakeDialogs(
                        count: dialogsToGenerate,
                        serviceResourceGenerator: () => MaybeGetRandomResource(options),
                        partyGenerator: () => MaybeGetRandomParty(options))
                    .Take(dialogsToGenerate);

                foreach (var dialog in dialogs)
                {
                    await writer.WriteAsync((dialogCounter + 1, dialog), cancellationToken);
                    dialogCounter++;

                    if (dialogCounter >= totalDialogs)
                        break;
                }
            }
        }
        finally
        {
            writer.Complete();
        }
    }

    private static List<string> _resourceList = [];
    private static string? MaybeGetRandomResource(Options options)
    {
        if (options.ResourceListPath == string.Empty) return null;
        if (_resourceList.Count != 0)
        {
            return _resourceList[MyRandomizer.Number(_resourceList.Count - 1)];
        }

        if (!File.Exists(options.ResourceListPath))
        {
            throw new FileNotFoundException($"{options.ResourceListPath} was not found");
        }

        _resourceList = File.ReadLines(options.ResourceListPath).ToList();
        if (_resourceList.Count == 0)
        {
            throw new InvalidOperationException(
                $"{options.ResourceListPath} needs to contain newline separated resources (eg. urn:altinn:resource:foobar)");
        }

        return _resourceList[MyRandomizer.Number(_resourceList.Count - 1)];
    }

    private static List<string> _partyList = [];
    private static string? MaybeGetRandomParty(Options options)
    {
        if (options.PartyListPath == string.Empty) return null;
        if (_partyList.Count != 0)
        {
            return _partyList[MyRandomizer.Number(_partyList.Count - 1)];
        }

        if (!File.Exists(options.PartyListPath))
        {
            throw new FileNotFoundException($"{options.PartyListPath} was not found");
        }

        _partyList = File.ReadLines(options.PartyListPath).ToList();
        if (_partyList.Count == 0)
        {
            throw new InvalidOperationException(
                $"{options.PartyListPath} needs to contain newline separated parties (eg. urn:altinn:person:identifier-no:12345678901)");
        }

        return _partyList[MyRandomizer.Number(_partyList.Count - 1)];
    }

    private static async Task ConsumeDialogsAndPost(Options options, ChannelReader<(int, CreateDialogDto)> reader, HttpClient client, CancellationToken cancellationToken)
    {
        await foreach (var item in reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                var json = JsonSerializer.Serialize(item.Item2, JsonSerializerOptions);
                using var request = new HttpRequestMessage(HttpMethod.Post, options.Url)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                if (!string.IsNullOrWhiteSpace(options.Token))
                {
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", options.Token);
                }

                var response = await client.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    await HandleFailedDialog(item, response);
                }

                Interlocked.Increment(ref _dialogCounter);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine($"\nException occurred while posting dialog: {ex.Message}");
                }
            }
        }
    }

    private static async Task ConsumeDialogsAndDiscards(ChannelReader<(int, CreateDialogDto)> reader,
        CancellationToken cancellationToken)
    {
        await foreach (var _ in reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                Interlocked.Increment(ref _dialogCounter);
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine($"\nException occurred while processing dialog: {ex.Message}");
                }
            }
        }
    }

    private static async Task ConsumeDialogsAndWriteToFile(ChannelReader<(int, CreateDialogDto)> reader,
        CancellationToken cancellationToken)
    {
        await foreach (var item in reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                var json = JsonSerializer.Serialize(item.Item2, JsonSerializerOptions);
                await File.WriteAllTextAsync($"{OutputDirectory}/dialog_{string.Format(CultureInfo.InvariantCulture, "{0:D6}", item.Item1)}.json", json, cancellationToken);
                Interlocked.Increment(ref _dialogCounter);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine($"\nException occurred while posting dialog: {ex.Message}");
                }
            }
        }
    }

    private static async Task HandleFailedDialog((int, object) item, HttpResponseMessage response)
    {
        Console.WriteLine($"\nFailed to post dialog: {response.StatusCode}");
        var result = await response.Content.ReadAsStringAsync();
        Console.WriteLine(result);
        var json = JsonSerializer.Serialize(item.Item2, JsonSerializerOptions);
        var output = $"{FailedDirectory}/{item.Item1}.json";
        try
        {
            Directory.CreateDirectory(FailedDirectory);
            await File.WriteAllTextAsync(output, json);
            Console.WriteLine($"Wrote request payload to '{output}'");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to write request payload to '{output}': {e.Message}");
        }
    }
}

public sealed class Options
{
    [Option('c', "count", Required = false, HelpText = "Number of fake dialogs to generate.")]
    public int Count { get; set; } = 1;

    [Option('s', "seed", Required = false, HelpText = "Seed for the random number generator.")]
    public int Seed { get; set; } = 1337;

    [Option('p', "parties", Required = false,
        HelpText = "Path to file containing newline separated parties to pick randomly from")]
    public string PartyListPath { get; set; } = string.Empty;

    [Option('r', "resources", Required = false,
        HelpText = "Path to file containing newline separated resources to pick randomly from")]
    public string ResourceListPath { get; set; } = string.Empty;

    [Option('a', "api", Required = false, HelpText = "Attempt to create the generated dialogs using service owner API.")]
    public bool Submit { get; set; } = false;

    [Option('w', "write", Required = false, HelpText = "Attempt to create the generated dialogs as files.")]
    public bool WriteToDisk { get; set; } = false;

    [Option('d', "discard", Required = false, HelpText = "Generate as fast as possible and discard.")]
    public bool Benchmark { get; set; } = false;

    [Option('u', "url", Required = false,
        Default = "https://localhost:7214/api/v1/serviceowner/dialogs",
        HelpText = "Service owner endpoint to post dialogs")]
    public string Url { get; set; } = null!;

    [Option('t', "token", Required = false, HelpText = "Bearer token to send as authorization header.")]
    public string Token { get; set; } = string.Empty;
}
