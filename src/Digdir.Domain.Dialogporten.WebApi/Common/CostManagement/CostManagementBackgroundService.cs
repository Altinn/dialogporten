using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.WebApi.Common.CostManagement;

/// <summary>
/// Background service that processes cost management metrics from a queue
/// </summary>
internal sealed class CostManagementBackgroundService : BackgroundService
{
    private readonly ChannelReader<TransactionRecord> _reader;
    private readonly ICostManagementTransactionRecorder _transactionRecorder;
    private readonly ILogger<CostManagementBackgroundService> _logger;

    public CostManagementBackgroundService(
        ChannelReader<TransactionRecord> reader,
        ICostManagementTransactionRecorder transactionRecorder,
        ILogger<CostManagementBackgroundService> logger)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _transactionRecorder = transactionRecorder ?? throw new ArgumentNullException(nameof(transactionRecorder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Cost management background service started");

        try
        {
            await foreach (var transaction in _reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    _transactionRecorder.RecordTransaction(
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
        }
        catch (OperationCanceledException)
        {
            // Expected when shutting down
            _logger.LogInformation("Cost management background service is shutting down");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in cost management background service");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cost management background service stopping");

        // First, stop the background execute loop
        await base.StopAsync(cancellationToken);

        // Then, best-effort drain pending items (bounded by cancellation)
        while (_reader.TryRead(out var transaction) && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                _transactionRecorder.RecordTransaction(
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
}
