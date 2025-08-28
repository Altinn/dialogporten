using System.Diagnostics.Metrics;
using System.Threading.Channels;

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
        _writer = writer;
        _reader = reader;
        _logger = logger;
        _queueCapacity = options.QueueCapacity;

        // Set up monitoring if enabled
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

            _droppedTransactionsCounter = meter.CreateCounter<long>(
                "dialogporten_cost_dropped_transactions_total",
                description: "Total number of cost management transactions dropped due to queue overflow");
        }
    }

    public void QueueTransaction(TransactionType transactionType, int httpStatusCode, string? tokenOrg = null, string? serviceOrg = null, string? serviceResource = null)
    {
        var transaction = new TransactionRecord(transactionType, httpStatusCode, tokenOrg, serviceOrg, serviceResource);

        try
        {
            if (!_writer.TryWrite(transaction))
            {
                _logger.LogWarning("Failed to queue cost management transaction - queue is full. Transaction: {TransactionType}, Status: {StatusCode}",
                    transactionType, httpStatusCode);

                // Track dropped transactions
                _droppedTransactionsCounter?.Add(1);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception while queuing cost management transaction: {TransactionType}, Status: {StatusCode}",
                transactionType, httpStatusCode);

            // Track dropped transactions due to exceptions
            _droppedTransactionsCounter?.Add(1);
        }
    }

    public void Dispose()
    {
        _writer.Complete();
    }
}
