using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;

internal sealed partial class LoggingFeatureMetricDeliveryContext : IFeatureMetricDeliveryContext
{
    private readonly FeatureMetricRecorder _recorder;
    private readonly ILogger<LoggingFeatureMetricDeliveryContext> _logger;
    private readonly IHostEnvironment? _hostEnvironment;

    public LoggingFeatureMetricDeliveryContext(
        FeatureMetricRecorder recorder,
        ILogger<LoggingFeatureMetricDeliveryContext> logger,
        IHostEnvironment? hostEnvironment = null)
    {
        _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _hostEnvironment = hostEnvironment;
    }

    public void Ack(string presentationTag, params IEnumerable<KeyValuePair<string, object>> additionalTags)
    {
        if (string.IsNullOrWhiteSpace(presentationTag))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(presentationTag));
        }

        var additionalTagsDic = additionalTags as Dictionary<string, object>
                                ?? new Dictionary<string, object>(additionalTags);
        foreach (var record in _recorder.Records.DefaultIfEmpty(new(
             FeatureName: "NoFeatureRecorded",
             Environment: _hostEnvironment?.EnvironmentName)))
        {
            LogFeatureMetric(_logger,
                record.FeatureName,
                record.Environment,
                record.PerformerOrg,
                record.OwnerOrg,
                record.ServiceResource,
                presentationTag,
                additionalTagsDic);
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
                  "AdditionalTags={@AdditionalTags}")]
    private static partial void LogFeatureMetric(
        ILogger logger,
        string featureType,
        string environment,
        string tokenOrg,
        string serviceOrg,
        string serviceResource,
        string presentationTag,
        Dictionary<string, object> additionalTags);
}

