using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;

internal sealed partial class LoggingFeatureMetricDeliveryContext(
    IFeatureMetricRecorder recorder,
    IAlwaysOnLogger<LoggingFeatureMetricDeliveryContext> logger)
    : IFeatureMetricDeliveryContext
{
    private readonly IFeatureMetricRecorder _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
    private readonly IAlwaysOnLogger<LoggingFeatureMetricDeliveryContext> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public void Ack(string presentationTag) => LogMetrics(success: true);

    public void Nack(string presentationTag) => LogMetrics(success: false);

    private void LogMetrics(bool success)
    {
        foreach (var record in _recorder.Records)
        {
            var (featureType, environment, tokenOrg, serviceOrg, serviceResource) = record;
            LogFeatureMetric(_logger,
                featureType,
                environment,
                tokenOrg ?? FeatureMetricConstants.UnknownValue,
                serviceOrg ?? FeatureMetricConstants.UnknownValue,
                serviceResource ?? FeatureMetricConstants.UnknownValue,
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
                  "Success={Success}")]
    private static partial void LogFeatureMetric(
        ILogger logger,
        string featureType,
        string environment,
        string tokenOrg,
        string serviceOrg,
        string serviceResource,
        bool success);
}

// TODO: Must be added to DI container
// services.AddSingleton(typeof(IAlwaysOnLogger<>), typeof(AlwaysOnLogger<>));
internal interface IAlwaysOnLogger<out TCategoryName> : ILogger<TCategoryName>;

internal sealed class AlwaysOnLogger<T>(ILogger<T> inner) : IAlwaysOnLogger<T>
{
    private readonly ILogger _inner = inner ?? throw new ArgumentNullException(nameof(inner));

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _inner.BeginScope(state);

    // 🔥 Always enabled for all levels
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId,
        TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => _inner.Log(logLevel, eventId, state, exception, formatter);
}

