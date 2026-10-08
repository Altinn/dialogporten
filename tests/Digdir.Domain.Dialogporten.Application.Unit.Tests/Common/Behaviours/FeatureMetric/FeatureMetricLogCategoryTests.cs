using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Common.Behaviours.FeatureMetric;

public class FeatureMetricLogCategoryTests
{
    // Hosts route the cost-allocation records by FeatureMetricLogCategory.Name. If the delivery context logged
    // under any other category, those records would silently miss their route.
    [Fact]
    public void Feature_metrics_are_logged_under_the_feature_metric_log_category()
    {
        var loggerFactory = new CategoryRecordingLoggerFactory();
        var deliveryContext = new LoggingFeatureMetricDeliveryContext(
            new FeatureMetricRecorder(),
            new Logger<LoggingFeatureMetricDeliveryContext>(loggerFactory));

        deliveryContext.ReportOutcome("GET_api/v1/enduser/dialogs");

        loggerFactory.LoggedCategories.Should().Equal(FeatureMetricLogCategory.Name);
    }

    private sealed class CategoryRecordingLoggerFactory : ILoggerFactory
    {
        public List<string> LoggedCategories { get; } = [];
        public ILogger CreateLogger(string categoryName) => new CategoryRecordingLogger(categoryName, LoggedCategories);
        public void AddProvider(ILoggerProvider provider) { }
        public void Dispose() { }
    }

    private sealed class CategoryRecordingLogger(string categoryName, List<string> loggedCategories) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) => loggedCategories.Add(categoryName);
    }
}
