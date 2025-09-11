using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading.Channels;

namespace Digdir.Domain.Dialogporten.WebApi.Common.CostManagement;

/// <summary>
/// Cost management service that handles queueing, background processing, and metrics recording
/// </summary>
public sealed class CostManagementService : BackgroundService, ICostManagementMetricsService
{
    private readonly ChannelWriter<TransactionRecord> _writer;
    private readonly ChannelReader<TransactionRecord> _reader;
    private readonly ILogger<CostManagementService> _logger;
    private readonly string _environment;
    private readonly int _queueCapacity;

    // Business metrics
    private readonly Counter<long> _transactionCounter;

    // System monitoring metrics
    private readonly Counter<long>? _droppedTransactionsCounter;

    public CostManagementService(
        ChannelWriter<TransactionRecord> writer,
        ChannelReader<TransactionRecord> reader,
        ILogger<CostManagementService> logger,
        CostManagementOptions options,
        IHostEnvironment hostEnvironment,
        Meter meter)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environment = (hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment))).EnvironmentName;
        _queueCapacity = (options ?? throw new ArgumentNullException(nameof(options))).QueueCapacity;
        ArgumentNullException.ThrowIfNull(meter);

        // Create business transaction counter
        _transactionCounter = meter.CreateCounter<long>(
            CostManagementConstants.TransactionCounterName,
            description: CostManagementConstants.TransactionCounterDescription);

        // Create dropped transactions counter for operational monitoring
        _droppedTransactionsCounter = meter.CreateCounter<long>(
            "cost_dropped_transactions_total",
            description: "Total number of cost management transactions dropped due to queue overflow");

        // Set up queue monitoring gauges for operational visibility
        meter.CreateObservableGauge("cost_queue_depth",
            () => _reader.CanCount ? _reader.Count : 0,
            description: "Current number of transactions waiting in the cost management queue");

        meter.CreateObservableGauge("cost_queue_capacity",
            () => _queueCapacity,
            description: "Maximum capacity of the cost management queue");
    }

    // ICostManagementMetricsService implementation (called by middleware)
    public void QueueTransaction(TransactionType transactionType, int httpStatusCode, string? tokenOrg = null, string? serviceOrg = null, string? serviceResource = null)
    {
        var transaction = new TransactionRecord(transactionType, httpStatusCode, tokenOrg, serviceOrg, serviceResource);
        if (_writer.TryWrite(transaction)) return;

        _logger.LogWarning("Failed to queue cost management transaction (queue unavailable or full). Transaction: {TransactionType}, Status: {StatusCode}",
            transactionType, httpStatusCode);

        // Record dropped transaction metrics with detailed context
        _droppedTransactionsCounter?.Add(1, BuildDroppedTransactionTags("enqueue_failed", transactionType, httpStatusCode, serviceOrg, tokenOrg));
    }

    // Background processing
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Cost management background service started");

        try
        {
            await ProcessTransactionQueueAsync();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unexpected error in cost management background service");
        }
    }

    // Graceful shutdown with transaction draining
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cost management background service stopping");
        _writer.TryComplete();
        await _reader.Completion;
        // Stop the background processing loop
        await base.StopAsync(cancellationToken);
    }

    private async Task ProcessTransactionQueueAsync()
    {
        await foreach (var transaction in _reader.ReadAllAsync())
        {
            var (transactionType, httpStatusCode, tokenOrg, serviceOrg, serviceResource) = transaction;
            var tags = BuildMetricTags(transactionType, httpStatusCode, tokenOrg, serviceOrg, serviceResource);
            _transactionCounter.Add(1, tags);
        }
    }

    private TagList BuildMetricTags(TransactionType transactionType, int httpStatusCode, string? tokenOrg, string? serviceOrg, string? serviceResource)
    {
        var status = httpStatusCode is >= 200 and < 300
            ? CostManagementConstants.StatusSuccess
            : CostManagementConstants.StatusFailed;

        return new TagList
        {
            { CostManagementConstants.TransactionTypeTag, transactionType.ToString() },
            { CostManagementConstants.StatusTag, status },
            { CostManagementConstants.HttpStatusCodeTag, httpStatusCode },
            { CostManagementConstants.EnvironmentTag, _environment },
            { CostManagementConstants.TokenOrgTag, NormalizeTag(tokenOrg) },
            { CostManagementConstants.ServiceOrgTag, NormalizeTag(serviceOrg) },
            { CostManagementConstants.ServiceResourceTag, NormalizeTag(serviceResource) }
        };
    }

    private static string NormalizeTag(string? value) =>
        string.IsNullOrWhiteSpace(value) ? CostManagementConstants.UnknownValue : value;

    private static TagList BuildDroppedTransactionTags(string reason, TransactionType transactionType, int httpStatusCode, string? serviceOrg, string? tokenOrg)
    {
        return new TagList
        {
            { "reason", reason },
            { "transaction_type", transactionType.ToString() },
            { "status_class", $"{httpStatusCode / 100}xx" },
            { "service_org", NormalizeTag(serviceOrg) },
            { "token_org", NormalizeTag(tokenOrg) }
        };
    }

    public override void Dispose()
    {
        // Complete the channel writer to signal no more transactions will be enqueued
        _writer.TryComplete();
        _reader.Completion.GetAwaiter().GetResult();
        base.Dispose();
    }
}

/// <summary>
/// No-operation implementation of ICostManagementMetricsService used when cost management is disabled.
/// Provides a null object pattern to avoid null checks throughout the codebase.
/// </summary>
public sealed class NoOpCostManagementService : ICostManagementMetricsService
{
    public void QueueTransaction(TransactionType transactionType, int httpStatusCode, string? tokenOrg = null, string? serviceOrg = null, string? serviceResource = null)
    {
        // No-op: Do nothing when cost management is disabled
    }
}

/// <summary>
/// No-operation implementation of IHostedService used when cost management is disabled.
/// </summary>
public sealed class NoOpHostedService : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
