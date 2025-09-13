using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;

/// <summary>
/// Value used when organization or service resource cannot be determined
/// </summary>
internal static class FeatureMetricConstants
{
    public const string UnknownValue = "unknown";
}

public sealed record FeatureMetricRecord(
    string FeatureName,
    string Environment,
    string? PerformerOrg = null,
    string? OwnerOrg = null,
    string? ServiceResource = null,
    int? HttpStatusCode = null,
    string? PresentationTag = null,
    string? Audience = null,
    string? CorrelationId = null);

public sealed class FeatureMetricRecorder
{
    private readonly Lock _lock = new();
    private List<FeatureMetricRecord> _records = [];

    public ReadOnlyCollection<FeatureMetricRecord> Records
    {
        get
        {
            lock (_lock)
            {
                return _records.AsReadOnly();
            }
        }
    }

    public void Record(FeatureMetricRecord record)
    {
        lock (_lock)
        {
            _records.Add(record);
        }
    }

    public void UpdateRecord(int httpStatusCode, string presentationTag, string correlationId)
    {
        lock (_lock)
        {
            var newRecords = _records.Select(r => r with
            {
                HttpStatusCode = httpStatusCode,
                PresentationTag = presentationTag,
                CorrelationId = correlationId
            }).ToList();

            _records = newRecords;
        }
    }
}