using System.Collections.ObjectModel;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;

internal sealed class FeatureMetricRecorder
{
    private readonly List<FeatureMetricRecord> _records = [];
    public ReadOnlyCollection<FeatureMetricRecord> Records => _records.AsReadOnly();
    public void Record(FeatureMetricRecord record) => _records.Add(record);
}

internal sealed record FeatureMetricRecord(
    string FeatureName,
    bool HasAdminScope = false,
    string? Environment = FeatureMetricRecord.UnknownValue,
    string? PerformerOrg = FeatureMetricRecord.UnknownValue,
    string? ServiceResource = FeatureMetricRecord.UnknownValue)
{
    private const string UnknownValue = "unknown";
    public string Environment { get; } = Environment ?? UnknownValue;
    public string PerformerOrg { get; } = PerformerOrg ?? UnknownValue;
    public string ServiceResource { get; } = ServiceResource ?? UnknownValue;
}
