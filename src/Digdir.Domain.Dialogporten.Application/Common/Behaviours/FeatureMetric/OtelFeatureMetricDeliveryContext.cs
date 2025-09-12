using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;

internal sealed class OtelFeatureMetricDeliveryContext : IFeatureMetricDeliveryContext, IFeatureMetricRecorder
{
    private static readonly Counter<long> TransactionCounter = Instrumentation.Meter.CreateCounter<long>(
        "cost_transactions_total",
        FeatureMetricConstants.TransactionCounterDescription);

    private readonly List<FeatureMetricRecord> _records = [];

    public void Record(FeatureMetricRecord record) => _records.Add(record);

    public void Ack(string presentationTag) => RecordMetrics(success: true);

    public void Nack(string presentationTag) => RecordMetrics(success: false);

    public void Abandon() => _records.Clear();

    private void RecordMetrics(bool success)
    {
        var records = _records.ToArray();
        _records.Clear();
        foreach (var record in records)
        {
            var tagList = ToTagList(record);
            tagList.Add(FeatureMetricConstants.StatusTag, success);
            tagList.Add(FeatureMetricConstants.PresentationTag, true);
            TransactionCounter.Add(1, tagList);
        }
    }

    private static TagList ToTagList(FeatureMetricRecord record)
    {
        const string unknown = "unknown";
        var (featureType, environment, tokenOrg, serviceOrg, serviceResource) = record;
        return new TagList
        {
            { FeatureMetricConstants.FeatureTypeTag, featureType },
            { FeatureMetricConstants.EnvironmentTag, environment },
            { FeatureMetricConstants.TokenOrgTag, tokenOrg ?? unknown },
            { FeatureMetricConstants.ServiceOrgTag, serviceOrg ?? unknown },
            { FeatureMetricConstants.ServiceResourceTag, serviceResource ?? unknown }
        };
    }
}


internal static class FeatureMetricConstants
{
    /// <summary>
    /// The name of the counter metric for transactions
    /// </summary>
    public const string TransactionCounterName = "transactions_total";

    /// <summary>
    /// Description of the transaction counter metric
    /// </summary>
    public const string TransactionCounterDescription = "Total number of feature metrics";

    /// <summary>
    /// Tag name for transaction type
    /// </summary>
    public const string TransactionTypeTag = "transaction_type";

    /// <summary>
    /// Tag name for organization short name from token
    /// </summary>
    public const string TokenOrgTag = "token_org";

    /// <summary>
    /// Tag name for organization short name from dialog entity
    /// </summary>
    public const string ServiceOrgTag = "service_org";

    /// <summary>
    /// Tag name for service resource type from dialog entity
    /// </summary>
    public const string ServiceResourceTag = "service_resource";

    /// <summary>
    /// Tag name for success/failure status
    /// </summary>
    public const string StatusTag = "status";

    /// <summary>
    /// Tag name for HTTP status code
    /// </summary>
    public const string HttpStatusCodeTag = "http_status_code";

    /// <summary>
    /// Tag name for environment
    /// </summary>
    public const string EnvironmentTag = "environment";

    /// <summary>
    /// Status value for successful operations (2xx)
    /// </summary>
    public const string StatusSuccess = "success";

    /// <summary>
    /// Status value for failed operations (4xx)
    /// </summary>
    public const string StatusFailed = "failed";

    /// <summary>
    /// Value used when organization or service resource cannot be determined
    /// </summary>
    public const string UnknownValue = "unknown";

    public const string PresentationTag = "presentation_tag";
}
