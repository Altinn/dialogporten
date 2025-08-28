using System.Threading.Channels;

namespace Digdir.Domain.Dialogporten.WebApi.Common.CostManagement;

/// <summary>
/// Background service that processes cost management metrics from a queue
/// </summary>
public sealed class CostManagementBackgroundService : BackgroundService
{
    private readonly ChannelReader<TransactionRecord> _reader;
    private readonly ICostManagementTransactionRecorder _transactionRecorder;
    private readonly ILogger<CostManagementBackgroundService> _logger;

    public CostManagementBackgroundService(
        ChannelReader<TransactionRecord> reader,
        ICostManagementTransactionRecorder transactionRecorder,
        ILogger<CostManagementBackgroundService> logger)
    {
        _reader = reader;
        _transactionRecorder = transactionRecorder;
        _logger = logger;
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
        await base.StopAsync(cancellationToken);
    }
}
