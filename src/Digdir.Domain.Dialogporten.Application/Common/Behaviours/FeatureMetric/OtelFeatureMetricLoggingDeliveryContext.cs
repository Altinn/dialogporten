using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;

/// <summary>
/// OpenTelemetry-based delivery context that logs feature metrics as structured logs using ILogger.
/// This approach uses Serilog → OpenTelemetry sink to get data into Loki/Grafana.
/// </summary>
internal sealed partial class OtelFeatureMetricLoggingDeliveryContext : IFeatureMetricDeliveryContext
{
    private readonly FeatureMetricRecorder _recorder;
    private readonly ILogger<OtelFeatureMetricLoggingDeliveryContext> _logger;

    public OtelFeatureMetricLoggingDeliveryContext(
        FeatureMetricRecorder recorder,
        ILogger<OtelFeatureMetricLoggingDeliveryContext> logger)
    {
        _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Ack(string presentationTag) => LogMetrics(success: true);

    public void Nack(string presentationTag) => LogMetrics(success: false);

    private void LogMetrics(bool success)
    {
        foreach (var record in _recorder.Records)
        {
            var (featureType, environment, tokenOrg, serviceOrg, serviceResource, httpStatusCode, presentationTag, audience, correlationId) = record;

            LogFeatureMetric(_logger,
                featureType,
                environment,
                tokenOrg ?? FeatureMetricConstants.UnknownValue,
                serviceOrg ?? FeatureMetricConstants.UnknownValue,
                serviceResource ?? FeatureMetricConstants.UnknownValue,
                httpStatusCode,
                presentationTag ?? FeatureMetricConstants.UnknownValue,
                audience ?? FeatureMetricConstants.UnknownValue,
                correlationId ?? FeatureMetricConstants.UnknownValue,
                success);
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
                  "HttpStatusCode={HttpStatusCode}, " +
                  "PresentationTag={PresentationTag}, " +
                  "Audience={Audience}, " +
                  "CorrelationId={CorrelationId}, " +
                  "Success={Success}")]
    private static partial void LogFeatureMetric(
        ILogger logger,
        string featureType,
        string environment,
        string tokenOrg,
        string serviceOrg,
        string serviceResource,
        int? httpStatusCode,
        string presentationTag,
        string audience,
        string correlationId,
        bool success);
}
