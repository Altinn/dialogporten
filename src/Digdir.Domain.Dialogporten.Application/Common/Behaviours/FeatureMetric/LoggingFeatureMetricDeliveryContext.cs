using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;

internal sealed partial class LoggingFeatureMetricDeliveryContext : IFeatureMetricDeliveryContext
{
    private readonly FeatureMetricRecorder _recorder;
    private readonly ILogger<LoggingFeatureMetricDeliveryContext> _logger;

    public LoggingFeatureMetricDeliveryContext(
        FeatureMetricRecorder recorder,
        ILogger<LoggingFeatureMetricDeliveryContext> logger)
    {
        _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Ack(string presentationTag, params IEnumerable<KeyValuePair<string, string>> additionalTags) =>
        LogMetrics(success: true, presentationTag, additionalTags);

    public void Nack(string presentationTag, params IEnumerable<KeyValuePair<string, string>> additionalTags) =>
        LogMetrics(success: false, presentationTag, additionalTags);

    private void LogMetrics(bool success, string presentationTag, IEnumerable<KeyValuePair<string, string>> additionalTags)
    {
        if (string.IsNullOrWhiteSpace(presentationTag))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(presentationTag));
        }

        var add = new Dictionary<string, string>(additionalTags);

        foreach (var record in _recorder.Records)
        {
            LogFeatureMetric(_logger,
                record.FeatureName,
                record.Environment,
                record.PerformerOrg,
                record.OwnerOrg,
                record.ServiceResource,
                presentationTag,
                success,
                add);
        }
    }

    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "Feature Metric Recorded: " +
                  "FeatureType={FeatureType}, " +
                  "Environment={Environment}, " +
                  "TokenOrg={TokenOrg}, " +
                  "ServiceOrg={ServiceOrg}, " +
                  "ServiceResource={ServiceResource}, " +
                  "PresentationTag={PresentationTag}, " +
                  "Success={Success}, " +
                  "AdditionalTags={AdditionalTags}")]
    private static partial void LogFeatureMetric(
        ILogger logger,
        string featureType,
        string environment,
        string tokenOrg,
        string serviceOrg,
        string serviceResource,
        string presentationTag,
        bool success,
        Dictionary<string, string> additionalTags);
}

