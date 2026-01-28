using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Search.Commands.ReindexDialogSearch;

public sealed class ReindexDialogSearchCommand : IRequest<ReindexDialogSearchResult>, IFeatureMetricServiceResourceIgnoreRequest
{
    public bool Full { get; init; }
    public DateTimeOffset? Since { get; init; }
    public bool StaleOnly { get; init; }
    public bool StaleFirst { get; init; }
    public bool Resume { get; init; }
    public int? BatchSize { get; init; }
    public int? Workers { get; init; }
    public int? ThrottleMs { get; init; }
    public long? WorkMemBytes { get; init; }
}

[GenerateOneOf]
public sealed partial class ReindexDialogSearchResult : OneOfBase<Success, ValidationError>;

internal sealed partial class ReindexDialogSearchCommandHandler : IRequestHandler<ReindexDialogSearchCommand, ReindexDialogSearchResult>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReindexDialogSearchCommandHandler> _logger;

    public ReindexDialogSearchCommandHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<ReindexDialogSearchCommandHandler> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<ReindexDialogSearchResult> Handle(ReindexDialogSearchCommand request, CancellationToken ct)
    {
        LogReindexDialogSearchCommandStarted(
            request.Full,
            request.Since,
            request.Resume,
            request.StaleOnly,
            request.StaleFirst,
            request.BatchSize,
            request.Workers,
            request.ThrottleMs,
            request.WorkMemBytes);

        var options = BuildOptions(request);
        LogReindexOptionsConfigured(options.BatchSize, options.Workers, options.ThrottleMs, options.WorkMemBytes);

        if (!request.Resume)
        {
            await SeedAsync(request, ct);
        }
        else
        {
            _logger.LogInformation("Resume requested: no seeding performed.");
        }

        var baselineProgress = await WithRepositoryAsync((repo, token) => repo.GetProgressAsync(token), ct);
        var baselineDone = baselineProgress.Done;
        LogBaselineProgressRetrieved(baselineProgress.Total, baselineProgress.Pending, baselineDone);

        var startedAtUtc = DateTimeOffset.UtcNow;

        var workerTasks = CreateWorkerTasks(options, request.StaleFirst, ct);
        LogWorkerTasksCreated(workerTasks.Count);

        using var progressCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var progressTask = StartProgressLoggerAsync(startedAtUtc, baselineDone, progressCts.Token);
        _logger.LogInformation("Progress logger started");

        _logger.LogInformation("Waiting for all workers to complete...");
        await Task.WhenAll(workerTasks);
        await progressCts.CancelAsync();
        await progressTask;

        var total = workerTasks.Sum(t => t.Result);
        var elapsed = DateTimeOffset.UtcNow - startedAtUtc;

        if (ct.IsCancellationRequested)
        {
            var averageSpeedDuringRun = elapsed.TotalSeconds > 0 ? total / elapsed.TotalSeconds : 0d;
            LogReindexCancelled(total, averageSpeedDuringRun, elapsed);
            return new Success();
        }

        var finalProgress = await WithRepositoryAsync((repo, token) => repo.GetProgressAsync(token), ct);
        var processedSinceStart = Math.Max(0, finalProgress.Done - baselineDone);
        var averageSpeed = elapsed.TotalSeconds > 0 ? processedSinceStart / elapsed.TotalSeconds : 0d;

        LogReindexFinished(total, averageSpeed, elapsed);

        _logger.LogInformation("Starting search index optimization...");
        await OptimizeSearchIndex(ct);
        _logger.LogInformation("Search index optimization completed");

        return new Success();
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
            var count = await WithRepositoryAsync(
                (repo, token) => repo.SeedFullAsync(resetExisting: true, token), ct);
            LogSeededFullRebuildQueue(count);
            return;
        }

        if (request.Since.HasValue)
        {
            var count = await WithRepositoryAsync(
                (repo, token) => repo.SeedSinceAsync(request.Since.Value, resetMatching: true, token), ct);
            LogSeededSinceQueue(request.Since, count);
            return;
        }

        if (request.StaleOnly)
        {
            var count = await WithRepositoryAsync(
                (repo, token) => repo.SeedStaleAsync(resetMatching: true, token), ct);
            LogSeededStaleOnlyQueue(count);
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
        LogWorkerStarting(workerId, options.BatchSize, staleFirst);

        long total = 0;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDialogSearchRepository>();

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var processed = await repository.WorkBatchAsync(
                    options.BatchSize, options.WorkMemBytes, staleFirst, ct);

                if (processed <= 0)
                {
                    LogWorkerNoMoreItems(workerId);
                    break;
                }

                total += processed;

                LogWorkerProcessed(workerId, processed, total);

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
            _logger.LogError(ex, "Worker #{WorkerId} failed: {Message}", workerId, ex.Message);
            throw;
        }

        return total;
    }

    private Task StartProgressLoggerAsync(DateTimeOffset startedAtUtc, long baselineDone, CancellationToken ct) =>
        Task.Run(async () =>
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), ct); // log every 30 seconds
                    var p = await WithRepositoryAsync((repo, token) => repo.GetProgressAsync(token), ct);

                    var pct = p.Total > 0 ? (double)p.Done / p.Total : 0.0;
                    var elapsed = DateTimeOffset.UtcNow - startedAtUtc;
                    var doneSinceStart = Math.Max(0, p.Done - baselineDone);
                    var avgDps = elapsed.TotalSeconds > 0 ? doneSinceStart / elapsed.TotalSeconds : 0d;
                    LogProgress(p.Total, p.Pending, p.Done, pct, avgDps);
                }
            }
            catch (OperationCanceledException)
            {
                // expected
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Progress logger failed: {Message}", ex.Message);
            }
        }, ct);

    private async Task<T> WithRepositoryAsync<T>(
        Func<IDialogSearchRepository, CancellationToken, Task<T>> action,
        CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDialogSearchRepository>();
        return await action(repository, ct);
    }

    private async Task OptimizeSearchIndex(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDialogSearchRepository>();
        await repository.OptimizeIndexAsync(ct);
    }

    private sealed record Options
    {
        public required int BatchSize { get; init; }
        public required int Workers { get; init; }
        public required int ThrottleMs { get; init; }
        public required long WorkMemBytes { get; init; }
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "ReindexDialogSearchCommand handler started. Mode: Full={Full}, Since={Since}, Resume={Resume}, StaleOnly={StaleOnly}, StaleFirst={StaleFirst}, BatchSize={BatchSize}, Workers={Workers}, ThrottleMs={ThrottleMs}, WorkMemBytes={WorkMemBytes}")]
    private partial void LogReindexDialogSearchCommandStarted(
        bool full,
        DateTimeOffset? since,
        bool resume,
        bool staleOnly,
        bool staleFirst,
        int? batchSize,
        int? workers,
        int? throttleMs,
        long? workMemBytes);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Reindex options configured: BatchSize={BatchSize}, Workers={Workers}, ThrottleMs={ThrottleMs}, WorkMemBytes={WorkMemBytes}")]
    private partial void LogReindexOptionsConfigured(int batchSize, int workers, int throttleMs, long workMemBytes);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Baseline progress retrieved: Total={Total}, Pending={Pending}, Done={Done}")]
    private partial void LogBaselineProgressRetrieved(long total, long pending, long done);

    [LoggerMessage(Level = LogLevel.Information, Message = "Created {WorkerCount} worker tasks")]
    private partial void LogWorkerTasksCreated(int workerCount);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Reindex cancelled by user, rolling back blocks currently being processed. Processed {Total} dialogs (~{AverageSpeed:F1} dialogs/s across {Elapsed}).")]
    private partial void LogReindexCancelled(long total, double averageSpeed, TimeSpan elapsed);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Reindex finished. Total processed by all workers: {Total}. Average speed ~{AverageSpeed:F1} dialogs/s across {Elapsed}.")]
    private partial void LogReindexFinished(long total, double averageSpeed, TimeSpan elapsed);

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded full rebuild queue with {Count} dialogs.")]
    private partial void LogSeededFullRebuildQueue(long count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded since={Since:o} queue with {Count} dialogs.")]
    private partial void LogSeededSinceQueue(DateTimeOffset? since, long count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded stale-only queue with {Count} dialogs.")]
    private partial void LogSeededStaleOnlyQueue(long count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Worker #{WorkerId} starting (BatchSize={BatchSize}, StaleFirst={StaleFirst})")]
    private partial void LogWorkerStarting(int workerId, int batchSize, bool staleFirst);

    [LoggerMessage(Level = LogLevel.Information, Message = "Worker #{WorkerId}: no more items, exiting.")]
    private partial void LogWorkerNoMoreItems(int workerId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Worker #{WorkerId}: processed {Processed} (total {TotalProcessed})")]
    private partial void LogWorkerProcessed(int workerId, long processed, long totalProcessed);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Progress: total={Total} pending={Pending} done={Done} pct={Pct:P2} avg_dps={AvgDps:F1}")]
    private partial void LogProgress(long total, long pending, long done, double pct, double avgDps);
}
