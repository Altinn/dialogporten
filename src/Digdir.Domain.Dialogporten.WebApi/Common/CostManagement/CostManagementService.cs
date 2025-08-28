using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;

namespace Digdir.Domain.Dialogporten.WebApi.Common.CostManagement;

/// <summary>
/// Implementation of cost management metrics service using background queue processing
/// </summary>
public sealed class CostManagementService : ICostManagementMetricsService, IDisposable
{
    private readonly ChannelWriter<TransactionRecord> _writer;
    private readonly ChannelReader<TransactionRecord> _reader;
    private readonly ILogger<CostManagementService> _logger;
    private readonly Counter<long>? _droppedTransactionsCounter;
    private readonly int _queueCapacity;

    public CostManagementService(
        ChannelWriter<TransactionRecord> writer,
        ChannelReader<TransactionRecord> reader,
        ILogger<CostManagementService> logger,
        CostManagementOptions options,
        Meter? meter = null)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _logger = logger;
        _queueCapacity = options.QueueCapacity;

        // Create dropped transactions counter whenever meter is available (independent of monitoring flag)
        if (meter != null)
        {
            _droppedTransactionsCounter = meter.CreateCounter<long>(
                "dialogporten_cost_dropped_transactions_total",
                description: "Total number of cost management transactions dropped due to queue overflow");
        }

        // Set up queue monitoring gauges if explicitly enabled
        if (options.EnableQueueMonitoring && meter != null)
        {
            // Observable gauge for current queue depth - reported on-demand
            meter.CreateObservableGauge("dialogporten_cost_queue_depth",
                () => _reader.CanCount ? _reader.Count : 0,
                description: "Current number of transactions waiting in the cost management queue");

            // Observable gauge for queue capacity - static value
            meter.CreateObservableGauge("dialogporten_cost_queue_capacity",
                () => _queueCapacity,
                description: "Maximum capacity of the cost management queue");
        }
    }

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

    public void Dispose()
    {
        _writer.Complete();
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
