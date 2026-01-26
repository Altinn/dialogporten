using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;

namespace Digdir.Domain.Dialogporten.Janitor.CustomMetrics;

/// <summary>
/// Service that orchestrates the collection of custom metrics.
/// Each collector is responsible for its own instrument creation and recording.
/// </summary>
public sealed class CustomMetricsService
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
        _meterProvider = meterProvider;
    }

    /// <summary>
    /// Invokes all registered metric collectors to collect and record their metrics.
    /// </summary>
    public async Task CollectAllMetricsAsync(CancellationToken cancellationToken)
    {
        var collectorList = _collectors.ToList();
        _logger.LogDebug(
            "Starting custom metrics collection with {CollectorCount} registered collectors",
            collectorList.Count);

        foreach (var collector in collectorList)
        {
            try
            {
                await collector.CollectAndRecordAsync(cancellationToken);
                _logger.LogDebug("Collected metric: {MetricName}", collector.MetricName);
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
}
