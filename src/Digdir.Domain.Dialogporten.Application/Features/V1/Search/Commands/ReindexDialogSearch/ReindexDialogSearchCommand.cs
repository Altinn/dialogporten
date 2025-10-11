using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using MediatR;
using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Search.Commands.ReindexDialogSearch;

public sealed class ReindexDialogSearchCommand : IRequest<ReindexDialogSearchResult>, IFeatureMetricServiceResourceIgnoreRequest
{
    public bool Full { get; init; }
    public DateTimeOffset? Since { get; init; }
    public bool StaleOnly { get; set; }
    public bool StaleFirst { get; init; }
    public bool Resume { get; init; }
    public int? BatchSize { get; init; }
    public int? Workers { get; init; }
    public int? ThrottleMs { get; init; }
    public long? WorkMemBytes { get; init; }
}

[GenerateOneOf]
public sealed partial class ReindexDialogSearchResult : OneOfBase<Success, ValidationError>;

internal sealed class ReindexDialogSearchCommandHandler : IRequestHandler<ReindexDialogSearchCommand, ReindexDialogSearchResult>
{
    private readonly IDialogSearchRepository _dialogSearchRepository;
    private readonly ILogger<ReindexDialogSearchCommandHandler> _logger;

    public ReindexDialogSearchCommandHandler(
        IDialogSearchRepository dialogSearchRepository,
        ILogger<ReindexDialogSearchCommandHandler> logger)
    {
        _dialogSearchRepository = dialogSearchRepository ?? throw new ArgumentNullException(nameof(dialogSearchRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReindexDialogSearchResult> Handle(ReindexDialogSearchCommand request, CancellationToken ct)
    {
        var options = BuildOptions(request);

        if (!request.Resume)
        {
            await SeedAsync(request, ct);
        }
        else
        {
            _logger.LogInformation("Resume requested: no seeding performed.");
        }

        var workerTasks = CreateWorkerTasks(options, request.StaleFirst, ct);

        using var progressCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var progressTask = StartProgressLoggerAsync(progressCts.Token);

        await Task.WhenAll(workerTasks);
        await progressCts.CancelAsync();
        await progressTask;

        var total = workerTasks.Sum(t => t.Result);
        _logger.LogInformation("Reindex finished. Total processed by all workers: {Total}.", total);

        return new ReindexDialogSearchResult(new Success());
    }

    private static Options BuildOptions(ReindexDialogSearchCommand request) => new()
    {
        BatchSize = request.BatchSize ?? 1000,
        Workers = Math.Max(1, request.Workers ?? 1),
        ThrottleMs = request.ThrottleMs ?? 0,
        WorkMemBytes = request.WorkMemBytes ?? 268_435_456L // 256MB
    };

    private async Task SeedAsync(ReindexDialogSearchCommand request, CancellationToken ct)
    {
        if (request.Full)
        {
            var count = await _dialogSearchRepository.SeedFullAsync(resetExisting: true, ct);
            _logger.LogInformation("Seeded full rebuild queue with {Count} dialogs.", count);
            return;
        }

        if (request.Since.HasValue)
        {
            var count = await _dialogSearchRepository.SeedSinceAsync(request.Since.Value, resetMatching: true, ct);
            _logger.LogInformation("Seeded since={Since:o} queue with {Count} dialogs.", request.Since, count);
            return;
        }

        if (request.StaleOnly)
        {
            var count = await _dialogSearchRepository.SeedStaleAsync(resetMatching: true, ct);
            _logger.LogInformation("Seeded stale-only queue with {Count} dialogs.", count);
        }
    }

    private List<Task<long>> CreateWorkerTasks(Options options, bool staleFirst, CancellationToken ct)
    {
        var tasks = new List<Task<long>>(options.Workers);

        for (var i = 1; i <= options.Workers; i++)
        {
            var workerId = i;
            tasks.Add(Task.Run(() => RunWorkerAsync(workerId, options, staleFirst, ct), ct));
        }

        return tasks;
    }

    private async Task<long> RunWorkerAsync(int workerId, Options options, bool staleFirst, CancellationToken ct)
    {
        long total = 0;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var processed = await _dialogSearchRepository.WorkBatchAsync(
                    options.BatchSize, options.WorkMemBytes, staleFirst, ct);

                if (processed <= 0)
                {
                    _logger.LogInformation("Worker #{WorkerId}: no more items, exiting.", workerId);
                    break;
                }

                total += processed;

                _logger.LogDebug(
                    "Worker #{WorkerId}: processed {Processed} (total {TotalProcessed})",
                    workerId, processed, total);

                if (options.ThrottleMs > 0)
                    await Task.Delay(options.ThrottleMs, ct);
            }
        }
        catch (OperationCanceledException)
        {
            // expected
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Worker #{WorkerId} failed", workerId);
            throw;
        }

        return total;
    }

    private Task StartProgressLoggerAsync(CancellationToken ct) =>
        Task.Run(async () =>
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), ct);
                    var p = await _dialogSearchRepository.GetProgressAsync(ct);

                    var pct = p.Total > 0 ? (double)p.Done / p.Total : 0.0;
                    _logger.LogInformation(
                        "Progress: done={Done}/{Total} ({Pct:P2}) pending={Pending} failed={Failed} dps_5m~{Dps5m:F1}",
                        p.Done, p.Total, pct, p.Pending, p.Failed, p.DialogsPerSec5m);
                }
            }
            catch (OperationCanceledException)
            {
                // expected
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Progress logger failed");
            }
        }, ct);

    private sealed record Options
    {
        public required int BatchSize { get; init; }
        public required int Workers { get; init; }
        public required int ThrottleMs { get; init; }
        public required long WorkMemBytes { get; init; }
    }
}
