using System.Diagnostics;

namespace Digdir.Domain.Dialogporten.WebApi.Common.CostManagement;

/// <summary>
/// Abstraction for recording cost management metrics to any backend
/// </summary>
public interface IMetricsRecorder
{
    /// <summary>
    /// Records a counter metric with the specified tags
    /// </summary>
    /// <param name="name">Metric name</param>
    /// <param name="value">Value to add to the counter</param>
    /// <param name="tags">Tags to associate with the metric</param>
    void RecordCounter(string name, long value, TagList tags);
}