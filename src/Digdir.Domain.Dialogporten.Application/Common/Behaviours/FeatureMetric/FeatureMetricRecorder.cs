using System.Collections.ObjectModel;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;

/// <summary>
/// Value used when organization or service resource cannot be determined
/// </summary>
internal static class FeatureMetricConstants
{
    public const string UnknownValue = "unknown";
}

internal sealed class FeatureMetricRecorder
{
    private readonly List<FeatureMetricRecord> _records = [];
    public ReadOnlyCollection<FeatureMetricRecord> Records => _records.AsReadOnly();
    public void Record(FeatureMetricRecord record) => _records.Add(record);
}

internal sealed record FeatureMetricRecord(
    string FeatureName,
    string? Environment = FeatureMetricConstants.UnknownValue,
    string? PerformerOrg = FeatureMetricConstants.UnknownValue,
    string? OwnerOrg = FeatureMetricConstants.UnknownValue,
    string? ServiceResource = FeatureMetricConstants.UnknownValue)
{
    public string Environment { get; } = Environment ?? FeatureMetricConstants.UnknownValue;
    public string PerformerOrg { get; } = PerformerOrg ?? FeatureMetricConstants.UnknownValue;
    public string OwnerOrg { get; } = OwnerOrg ?? FeatureMetricConstants.UnknownValue;
    public string ServiceResource { get; } = ServiceResource ?? FeatureMetricConstants.UnknownValue;
}
