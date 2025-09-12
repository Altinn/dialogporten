using System.Collections.ObjectModel;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;

internal interface IFeatureMetricRecorder
{
    ReadOnlyCollection<FeatureMetricRecord> Records { get; }
    void Record(FeatureMetricRecord record);
}

internal sealed record FeatureMetricRecord(
    string FeatureName,
    string Environment,
    string? PerformerOrg = null,
    string? OwnerOrg = null,
    string? ServiceResource = null);

internal sealed class FeatureMetricRecorder : IFeatureMetricRecorder
{
    private readonly List<FeatureMetricRecord> _records = [];
    public ReadOnlyCollection<FeatureMetricRecord> Records => _records.AsReadOnly();
    public void Record(FeatureMetricRecord record) => _records.Add(record);
}
