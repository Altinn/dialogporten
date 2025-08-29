using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;

namespace Digdir.Domain.Dialogporten.WebApi.Common.CostManagement;

/// <summary>
/// Cost management service that handles queueing, background processing, and metrics recording
/// </summary>
public sealed class CostManagementService : BackgroundService, ICostManagementMetricsService, ICostManagementTransactionRecorder
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
            "dialogporten_cost_dropped_transactions_total",
            description: "Total number of cost management transactions dropped due to queue overflow");

        // Set up queue monitoring gauges for operational visibility
        meter.CreateObservableGauge("dialogporten_cost_queue_depth",
            () => _reader.CanCount ? _reader.Count : 0,
            description: "Current number of transactions waiting in the cost management queue");

        meter.CreateObservableGauge("dialogporten_cost_queue_capacity",
            () => _queueCapacity,
            description: "Maximum capacity of the cost management queue");
    }

    // ICostManagementMetricsService implementation (called by middleware)
    public void QueueTransaction(TransactionType transactionType, int httpStatusCode, string? tokenOrg = null, string? serviceOrg = null, string? serviceResource = null)
    {
        var transaction = new TransactionRecord(transactionType, httpStatusCode, tokenOrg, serviceOrg, serviceResource);

        try
        {
            if (!_writer.TryWrite(transaction))
            {
                _logger.LogWarning("Failed to queue cost management transaction (queue unavailable or full). Transaction: {TransactionType}, Status: {StatusCode}",
                    transactionType, httpStatusCode);

                // Track dropped transactions
                _droppedTransactionsCounter?.Add(1, new TagList { { "reason", "enqueue_failed" } });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception while queuing cost management transaction: {TransactionType}, Status: {StatusCode}",
                transactionType, httpStatusCode);

            // Track dropped transactions due to exceptions
            _droppedTransactionsCounter?.Add(1, new TagList { { "reason", "exception" } });
        }
    }

    // ICostManagementTransactionRecorder implementation (internal processing)
    public void RecordTransaction(TransactionType transactionType, int httpStatusCode, string? tokenOrg = null, string? serviceOrg = null, string? serviceResource = null)
    {
        var tags = BuildMetricTags(transactionType, httpStatusCode, tokenOrg, serviceOrg, serviceResource);
        _transactionCounter.Add(1, tags);
    }

    // Background processing
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Cost management background service started");

        try
        {
            await ProcessTransactionQueueAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Cost management background service is shutting down");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in cost management background service");
        }
    }

    // Graceful shutdown with transaction draining
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cost management background service stopping");

        await base.StopAsync(cancellationToken);
        DrainRemainingTransactions(cancellationToken);
    }

    private async Task ProcessTransactionQueueAsync(CancellationToken stoppingToken)
    {
        await foreach (var transaction in _reader.ReadAllAsync(stoppingToken))
        {
            ProcessSingleTransaction(transaction);
        }
    }

    private void ProcessSingleTransaction(TransactionRecord transaction)
    {
        try
        {
            RecordTransaction(
                transaction.TransactionType,
                transaction.HttpStatusCode,
                transaction.TokenOrg,
                transaction.ServiceOrg,
                transaction.ServiceResource);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to process cost management transaction: {TransactionType}, Status: {StatusCode}",
                transaction.TransactionType, transaction.HttpStatusCode);
        }
    }

    private void DrainRemainingTransactions(CancellationToken cancellationToken)
    {
        while (_reader.TryRead(out var transaction) && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                RecordTransaction(
                    transaction.TransactionType,
                    transaction.HttpStatusCode,
                    transaction.TokenOrg,
                    transaction.ServiceOrg,
                    transaction.ServiceResource);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to process transaction during shutdown: {TransactionType}, Status: {StatusCode}",
                    transaction.TransactionType, transaction.HttpStatusCode);
            }
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

    public override void Dispose()
    {
        _writer.Complete();
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
