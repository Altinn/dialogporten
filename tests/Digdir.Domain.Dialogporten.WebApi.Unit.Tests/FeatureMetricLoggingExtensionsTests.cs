using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.WebApi.Common.FeatureMetric;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Digdir.Domain.Dialogporten.WebApi.Unit.Tests;

public class FeatureMetricLoggingExtensionsTests
{
    private const string CollectorEndpoint = "http://feature-metrics-collector:4317";

    private const string DialogSearchCategory =
        "Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser";

    // Shares the namespace of the feature-metric category without being it.
    private const string FeatureMetricNamespaceSibling =
        "Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric.FeatureMetricBehaviour";

    [Fact]
    public void Feature_metrics_and_all_other_events_go_to_separate_sinks()
    {
        var otherEvents = new CollectingSink();
        var featureMetrics = new CollectingSink();

        using var logger = new LoggerConfiguration()
            // Mirrors Program.cs and appsettings.json: Warning by default, Information for these two categories.
            .MinimumLevel.Warning()
            .MinimumLevel.Override(FeatureMetricLogCategory.Name, LogEventLevel.Information)
            .MinimumLevel.Override(DialogSearchCategory, LogEventLevel.Information)
            .WriteTo.SplitFeatureMetrics(
                writeTo => writeTo.Sink(otherEvents),
                writeTo => writeTo.Sink(featureMetrics))
            .CreateLogger();

        logger.ForContext(Constants.SourceContextPropertyName, FeatureMetricLogCategory.Name).Information("feature metric");
        logger.ForContext(Constants.SourceContextPropertyName, FeatureMetricLogCategory.Name + ".Nested").Information("nested category");
        logger.ForContext(Constants.SourceContextPropertyName, DialogSearchCategory).Information("dialog search");
        logger.ForContext(Constants.SourceContextPropertyName, FeatureMetricNamespaceSibling).Warning("namespace sibling");
        logger.ForContext(Constants.SourceContextPropertyName, FeatureMetricLogCategory.Name + "Lookalike").Warning("lookalike category");
        logger.ForContext(Constants.SourceContextPropertyName, "Microsoft.AspNetCore.Hosting.Diagnostics").Warning("framework warning");
        logger.Warning("no source context");

        // The level override also covers nested categories, so the routing must too.
        featureMetrics.Messages.Should().Equal("feature metric", "nested category");
        otherEvents.Messages.Should().Equal(
            "dialog search", "namespace sibling", "lookalike category", "framework warning", "no source context");
    }

    [Fact]
    public void Without_the_endpoint_feature_metrics_stay_on_the_usual_sink()
    {
        var events = new CollectingSink();
        List<string> requested = [];

        using var logger = new LoggerConfiguration()
            .WriteTo.WithFeatureMetricEndpoint(writeTo => writeTo.Sink(events), name =>
            {
                requested.Add(name);
                return null;
            })
            .CreateLogger();

        logger.ForContext(Constants.SourceContextPropertyName, FeatureMetricLogCategory.Name).Information("feature metric");

        events.Messages.Should().Equal("feature metric");
        // An OTLP sink reads the OTEL_* variables when it is created, so reading only the endpoint means none was added.
        requested.Should().Equal(FeatureMetricLoggingExtensions.OtlpEndpointVariable);
    }

    [Fact]
    public void Feature_metric_sink_reads_resource_variables_but_not_exporter_settings_from_the_environment()
    {
        List<string> requested = [];

        using var logger = new LoggerConfiguration()
            .WriteTo.WithFeatureMetricEndpoint(writeTo => writeTo.Sink(new CollectingSink()), name =>
            {
                requested.Add(name);
                return name == FeatureMetricLoggingExtensions.OtlpEndpointVariable ? CollectorEndpoint : null;
            })
            .CreateLogger();

        requested.Should().Contain(["OTEL_SERVICE_NAME", "OTEL_RESOURCE_ATTRIBUTES"])
            .And.NotContain(name => name.StartsWith("OTEL_EXPORTER_OTLP_", StringComparison.Ordinal));
    }

    [Fact]
    public void Invalid_endpoint_fails_at_startup()
    {
        var act = () => new LoggerConfiguration().WriteTo.WithFeatureMetricEndpoint(
            writeTo => writeTo.Sink(new CollectingSink()),
            name => name == FeatureMetricLoggingExtensions.OtlpEndpointVariable ? "not-a-uri" : null);

        act.Should().Throw<InvalidOperationException>();
    }

    private sealed class CollectingSink : ILogEventSink
    {
        private readonly List<string> _messages = [];
        public IReadOnlyList<string> Messages => _messages;
        public void Emit(LogEvent logEvent) => _messages.Add(logEvent.MessageTemplate.Text);
    }
}
