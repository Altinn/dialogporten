using System.Diagnostics.Metrics;

namespace Digdir.Domain.Dialogporten.Janitor.CustomMetrics;

/// <summary>
/// Shared meter for all custom metrics.
/// </summary>
public static class CustomMetrics
{
    public const string MeterName = "Dialogporten.CustomMetrics";
    public static readonly Meter Meter = new(MeterName, "1.0.0");
}

/// <summary>
/// Interface for custom metric collectors.
/// </summary>
public interface IMetricCollector
{
    /// <summary>
    /// The name of the metric.
    /// </summary>
    string MetricName { get; }

    /// <summary>
    /// Collects and records the metric value(s).
    /// </summary>
    Task CollectAndRecordAsync(CancellationToken cancellationToken);
}
