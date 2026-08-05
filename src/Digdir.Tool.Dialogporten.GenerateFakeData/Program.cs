using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Altinn.ApiClients.Maskinporten.Config;
using Altinn.ApiClients.Maskinporten.Extensions;
using Altinn.ApiClients.Maskinporten.Services;
using Bogus;
using CommandLine;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static Digdir.Domain.Dialogporten.Application.Common.Authorization.Constants;

namespace Digdir.Tool.Dialogporten.GenerateFakeData;

public class Program
{
    private const int RefreshRateMs = 200; // How often the progress is updated
    private const int DialogsPerBatch = 5; // How many dialogs to generate per call DialogGenerator
    private const int BoundedCapacity = 1000; // Max number of dialogs in the queue
    private const int Consumers = 20; // Number of consumers posting to the API
    private const string FailedDirectory = "failed"; // Directory to write failed requests to
    private const string OutputDirectory = "output"; // Directory to write files to when not posting to the API
    private const string ClientBuilderName = "dialogporten";

    public static async Task Main(string[] args) => await Parser.Default.ParseArguments<Options>(args).WithParsedAsync(RunAsync);

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static int _dialogCounter;
    private static readonly Stopwatch Stopwatch = new();

    [field: ThreadStatic]
    private static Randomizer MyRandomizer => field ??= new Randomizer();

    private static async Task RunAsync(Options options)
    {
        DialogGenerator.SetSeed(options.Seed);

        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cancellationTokenSource.Cancel();
        };
        await LoadResources(options, cancellationToken);

        if (options is { Submit: false, WriteToDisk: false, Benchmark: false })
        {
            var dialogs = DialogGenerator.GenerateFakeDialogs(
                count: options.Count, serviceResourceGenerator: () => GetNextResource(options),
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

        Console.WriteLine($"Generating {options.Count} fake dialogs...");
        Stopwatch.Start();

        var producerTask = Task.Run(() => ProduceDialogs(options, writer, cancellationToken), cancellationToken);
        var progressTask = Task.Run(() => UpdateProgress(options, cancellationToken), cancellationToken);
        var host = await CreateHost(options, cancellationToken);
        using var client = host.Services.GetRequiredService<IHttpClientFactory>().CreateClient(ClientBuilderName);

        var consumerTasks = new List<Task>();
        for (var i = 0; i < Consumers; i++)
        {
            Func<Task> consumerAction = options switch
            {
                // ReSharper disable once AccessToDisposedClosure
                { Submit: true } => () => ConsumeDialogsAndPost(options, reader, client, cancellationToken),
                { WriteToDisk: true } => () => ConsumeDialogsAndWriteToFile(reader, cancellationToken),
                _ => () => ConsumeDialogsAndDiscards(reader, cancellationToken)
            };

            consumerTasks.Add(Task.Run(consumerAction, cancellationToken));
        }

        await producerTask;
        foreach (var task in consumerTasks)
        {
            await task;
        }

        Stopwatch.Stop();
        await progressTask;
    }

    private static async Task<IHost> CreateHost(Options options, CancellationToken cancellationToken)
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(config => config.AddUserSecrets<Program>())
            .ConfigureServices((ctx, services) =>
            {
                if (string.IsNullOrEmpty(options.Token))
                {
                    var settings = new MaskinportenSettings();
                    ctx.Configuration.GetRequiredSection("DialogportenSettings:Maskinporten").Bind(settings);

                    settings.Scope = "digdir:dialogporten.serviceprovider digdir:dialogporten.serviceprovider.admin";
                    settings.Environment = "test";

                    services.RegisterMaskinportenClientDefinition<SettingsJwkClientDefinition>(ClientBuilderName, settings);
                    services.AddHttpClient(ClientBuilderName)
                        .AddMaskinportenHttpMessageHandler<SettingsJwkClientDefinition>(ClientBuilderName);
                }
                else
                {
                    services.AddHttpClient(ClientBuilderName);
                }
            }).Build();
        await host.StartAsync(cancellationToken);
        return host;
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
                        serviceResourceGenerator: () => GetNextResource(options),
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

    private static long _position;
    private static List<string> _resourceList = [];

    private static async Task LoadResources(Options options, CancellationToken ct)
    {
        _resourceList = options.ResourceListPath == string.Empty
            ? await LoadResourcesFromRegister(options, ct)
            : LoadResourcesFromFile(options);
    }

    private sealed class RegisterResponse : List<RegisterResource>;
    private sealed record RegisterResource(
        string Identifier,
        string ResourceType,
        RegisterCompetentAuthority HasCompetentAuthority
    );
    private sealed record RegisterCompetentAuthority(string? Organization, string? Orgcode);

    private static async Task<List<string>> LoadResourcesFromRegister(Options options, CancellationToken ct)
    {
        var requestUri = $"{options.PlatformBaseUrl}/resourceregistry/api/v1/resource/resourcelist";
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        using var client = new HttpClient();
        var response = await client.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Unable to fetch resource list. Got status {response.StatusCode}");
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        var registerResponse = JsonSerializer.Deserialize<RegisterResponse>(json, JsonSerializerOptions)
                   ?? throw new UnreachableException("Register returned null");

        if (registerResponse.Count == 0) throw new UnreachableException("Register returned empty list");

        var resources = registerResponse
            .Where(x =>
                SupportedResourceTypes.Contains(x.ResourceType)
                && !string.IsNullOrEmpty(x.HasCompetentAuthority.Organization)
                && !string.IsNullOrEmpty(x.HasCompetentAuthority.Orgcode)
            )
            .Select(x => $"urn:altinn:resource:{x.Identifier}")
            .ToList();

        if (resources.Count == 0) throw new UnreachableException("Resources cant be empty");

        Console.WriteLine("Found {0} resource(s) in Register", resources.Count);

        return resources;
    }

    private static List<string> LoadResourcesFromFile(Options options)
    {
        var resources = !File.Exists(options.ResourceListPath)
            ? throw new FileNotFoundException($"{options.ResourceListPath} was not found")
            : File.ReadLines(options.ResourceListPath).Distinct().ToList();

        return resources.Count == 0
            ? throw new InvalidOperationException(
                $"{options.ResourceListPath} needs to contain newline separated resources (eg. urn:altinn:resource:foobar)")
            : resources;
    }

    private static string GetNextResource(Options options)
    {
        return options.RandomizeResources
            ? _resourceList[MyRandomizer.Number(_resourceList.Count - 1)]
            : _resourceList[(int)(_position++ % _resourceList.Count)];
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

    private static async Task ConsumeDialogsAndPost(Options options, ChannelReader<(int, CreateDialogDto)> reader, HttpClient client, CancellationToken ct)
    {
        await foreach (var item in reader.ReadAllAsync(ct))
        {
            try
            {
                var requestUri = $"{options.Url}/api/v1/serviceowner/dialogs";
                var json = JsonSerializer.Serialize(item.Item2, JsonSerializerOptions);
                using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                if (!string.IsNullOrWhiteSpace(options.Token))
                {
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", options.Token);
                }

                var response = await client.SendAsync(request, ct);

                if (!response.IsSuccessStatusCode)
                {
                    await HandleFailedDialog(item, response);
                }

                Interlocked.Increment(ref _dialogCounter);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                if (!ct.IsCancellationRequested)
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
            Interlocked.Increment(ref _dialogCounter);
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
                await File.WriteAllTextAsync($"{OutputDirectory}/dialog_{item.Item1:D6}.json", json, cancellationToken);
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
                    Console.WriteLine($"\nException occurred while writing dialog to file: {ex.Message}");
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
        HelpText = "Path to file containing newline separated resources to pick randomly from. Uses resource registry if unset")]
    public string ResourceListPath { get; set; } = string.Empty;

    [Option('z', "randomize", Required = false,
        HelpText = "Randomly pick resources if true, otherwise pick with round robin")]
    public bool RandomizeResources { get; set; } = false;

    [Option('e', "platformEnvironmentUrl", Required = false, HelpText = "Platform environment base URL")]
    public string PlatformBaseUrl { get; set; } = "https://platform.at23.altinn.cloud";

    [Option('a', "api", Required = false, HelpText = "Attempt to create the generated dialogs using service owner API.")]
    public bool Submit { get; set; } = false;

    [Option('w', "write", Required = false, HelpText = "Attempt to create the generated dialogs as files.")]
    public bool WriteToDisk { get; set; } = false;

    [Option('d', "discard", Required = false, HelpText = "Generate as fast as possible and discard.")]
    public bool Benchmark { get; set; } = false;

    [Option('u', "url", Required = false,
        Default = "https://localhost:7214",
        HelpText = "Base url for dialogporten")]
    public string Url { get; set; } = null!;

    [Option('t', "token", Required = false, HelpText = "Bearer token to send as authorization header.")]
    public string Token { get; set; } = string.Empty;
}
