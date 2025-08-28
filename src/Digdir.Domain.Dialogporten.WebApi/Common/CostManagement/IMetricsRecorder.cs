using System.Diagnostics;

namespace Digdir.Domain.Dialogporten.WebApi.Common.CostManagement;

/// <summary>
/// Abstraction for recording cost management metrics to any backend
/// </summary>
internal interface IMetricsRecorder
{
    /// <summary>
    /// Records a transaction counter metric with the specified tags
    /// </summary>
    /// <param name="value">Value to add to the counter</param>
    /// <param name="tags">Tags to associate with the metric</param>
    void RecordTransactionCounter(long value, in TagList tags);
}