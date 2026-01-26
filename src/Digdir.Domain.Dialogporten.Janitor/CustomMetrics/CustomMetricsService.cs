using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;

namespace Digdir.Domain.Dialogporten.Janitor.CustomMetrics;

/// <summary>
/// Service that orchestrates the collection of custom metrics.
/// Each collector is responsible for its own instrument creation and recording.
/// </summary>
public sealed partial class CustomMetricsService
{
    private readonly IEnumerable<IMetricCollector> _collectors;
    private readonly ILogger<CustomMetricsService> _logger;
    private readonly MeterProvider? _meterProvider;

    public CustomMetricsService(
        IEnumerable<IMetricCollector> collectors,
        ILogger<CustomMetricsService> logger,
        MeterProvider? meterProvider = null)
    {
        _collectors = collectors ?? throw new ArgumentNullException(nameof(collectors));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _meterProvider = meterProvider ?? throw new ArgumentNullException(nameof(meterProvider));
    }

    /// <summary>
    /// Invokes all registered metric collectors to collect and record their metrics.
    /// </summary>
    public async Task CollectAllMetricsAsync(CancellationToken cancellationToken)
    {
        var collectorList = _collectors.ToList();
        LogStartingCustomMetricsCollection(collectorList.Count);

        foreach (var collector in collectorList)
        {
            try
            {
                await collector.CollectAndRecordAsync(cancellationToken);
                LogCollectedMetric(collector.MetricName);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to collect metric {MetricName}", collector.MetricName);
            }
        }

        _meterProvider?.ForceFlush();
        _logger.LogDebug("Custom metrics collection completed.");
    }

    [LoggerMessage(LogLevel.Debug, "Starting custom metrics collection with {CollectorCount} registered collectors")]
    partial void LogStartingCustomMetricsCollection(int CollectorCount);

    [LoggerMessage(LogLevel.Debug, "Collected metric: {MetricName}")]
    partial void LogCollectedMetric(string MetricName);
}
