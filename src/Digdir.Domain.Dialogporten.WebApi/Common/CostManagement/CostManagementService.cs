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
    private readonly Gauge<int>? _queueDepthGauge;
    private readonly Counter<long>? _droppedTransactionsCounter;
    private readonly Timer? _monitoringTimer;

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

        // Set up monitoring if enabled
        if (options.EnableQueueMonitoring && meter != null)
        {
            _queueDepthGauge = meter.CreateGauge<int>(
                "dialogporten_cost_queue_depth",
                description: "Current number of transactions waiting in the cost management queue");

            _droppedTransactionsCounter = meter.CreateCounter<long>(
                "dialogporten_cost_dropped_transactions_total",
                description: "Total number of cost management transactions dropped due to queue overflow");

            // Start monitoring timer
            _monitoringTimer = new Timer(ReportQueueDepth, null,
                TimeSpan.FromMilliseconds(options.MonitoringIntervalMs),
                TimeSpan.FromMilliseconds(options.MonitoringIntervalMs));
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

    private void ReportQueueDepth(object? state)
    {
        try
        {
            // Get approximate queue depth (items waiting to be read)
            var queueDepth = _reader.CanCount ? _reader.Count : -1;
            if (queueDepth >= 0)
            {
                _queueDepthGauge?.Record(queueDepth);

                // Log warning if queue is getting full (above 80% capacity)
                if (queueDepth > 0)
                {
                    _logger.LogDebug("Cost management queue depth: {QueueDepth}", queueDepth);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to report queue depth metrics");
        }
    }

    public void Dispose()
    {
        _monitoringTimer?.Dispose();
        _writer.Complete();
    }
}
