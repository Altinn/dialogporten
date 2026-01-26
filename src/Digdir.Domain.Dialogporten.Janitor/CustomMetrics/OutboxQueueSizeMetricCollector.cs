using System.Diagnostics.Metrics;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Digdir.Domain.Dialogporten.Janitor.CustomMetrics;

/// <summary>
/// Collects the estimated count of rows in the MassTransitOutboxState table.
/// Uses PostgreSQL statistics for fast estimation instead of COUNT(*).
/// A growing queue size may indicate issues with message delivery.
/// </summary>
public sealed class OutboxQueueSizeMetricCollector : IMetricCollector
{
    private readonly string _connectionString;
    private readonly ILogger<OutboxQueueSizeMetricCollector> _logger;
    private long _latestValue;

    public OutboxQueueSizeMetricCollector(
        IConfiguration configuration,
        ILogger<OutboxQueueSizeMetricCollector> logger)
    {
        _connectionString = configuration["Infrastructure:DialogDbConnectionString"]
            ?? throw new InvalidOperationException("Infrastructure:DialogDbConnectionString is not configured");
        _logger = logger;

        CustomMetrics.Meter.CreateObservableGauge(
            MetricName,
            () => _latestValue,
            unit: "items",
            description: "Estimated number of pending messages in the MassTransit outbox queue");
    }

    public string MetricName => "dialogporten.outbox.queue_size";

    public async Task CollectAndRecordAsync(CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(
            @"SELECT 
                (CASE 
                    WHEN relpages = 0 THEN 0 
                    ELSE (reltuples / relpages) * (pg_relation_size(oid) / (current_setting('block_size')::int)) 
                END)::bigint AS current_estimate
                FROM pg_class 
                WHERE oid = '""MassTransitOutboxState""'::regclass;",
            connection);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        var value = result is long count ? count : Convert.ToInt64(result, CultureInfo.InvariantCulture);

        _latestValue = value;

        _logger.LogDebug("Outbox queue size estimate: {Value}", value);
    }
}
