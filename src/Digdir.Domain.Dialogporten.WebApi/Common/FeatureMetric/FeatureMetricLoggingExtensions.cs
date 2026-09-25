using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Library.Utils.AspNet;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Filters;
using Serilog.Sinks.OpenTelemetry;

namespace Digdir.Domain.Dialogporten.WebApi.Common.FeatureMetric;

internal static class FeatureMetricLoggingExtensions
{
    // Read from the process environment only, never from IConfiguration, so that only the deployment manifest
    // can set it. The Azure App Configuration store is shared with hosts that have no feature-metrics collector,
    // and a value there would send their feature metrics to an endpoint they cannot reach.
    internal const string OtlpEndpointVariable = "FEATURE_METRICS_OTLP_ENDPOINT";

    // Same scope as Serilog's MinimumLevel.Override (the category and the categories nested under it), so every
    // event that the appsettings.json override raises to Information is routed to the feature-metric endpoint.
    private static readonly Func<LogEvent, bool> IsFeatureMetricEvent = Matching.FromSource(FeatureMetricLogCategory.Name);

    extension(LoggerSinkConfiguration writeTo)
    {
        /// <summary>
        /// Writes log events as <see cref="OpenTelemetryExtensions.OpenTelemetryOrConsole"/> does. When
        /// <c>FEATURE_METRICS_OTLP_ENDPOINT</c> is set, feature-metric events go only to that OTLP endpoint instead.
        /// On DIS the platform collector drops logs below Warning, so the cost-allocation records need their own path.
        /// </summary>
        public LoggerConfiguration OpenTelemetryOrConsoleWithFeatureMetricEndpoint(HostBuilderContext context) =>
            writeTo.WithFeatureMetricEndpoint(
                events => events.OpenTelemetryOrConsole(context),
                Environment.GetEnvironmentVariable);

        internal LoggerConfiguration WithFeatureMetricEndpoint(
            Func<LoggerSinkConfiguration, LoggerConfiguration> writeEvents,
            Func<string, string?> getEnvironmentVariable)
        {
            var endpoint = getEnvironmentVariable(OtlpEndpointVariable);
            if (endpoint is null)
            {
                return writeEvents(writeTo);
            }

            if (!Uri.IsWellFormedUriString(endpoint, UriKind.Absolute))
            {
                throw new InvalidOperationException($"Invalid {OtlpEndpointVariable}: {endpoint}");
            }

            return writeTo.SplitFeatureMetrics(
                writeEvents,
                featureMetrics => featureMetrics.OpenTelemetry(
                    options =>
                    {
                        options.Endpoint = endpoint;
                        options.Protocol = OtlpProtocol.Grpc;
                    },
                    ResourceVariablesOnly(getEnvironmentVariable)));
        }

        internal LoggerConfiguration SplitFeatureMetrics(
            Func<LoggerSinkConfiguration, LoggerConfiguration> writeOtherEvents,
            Func<LoggerSinkConfiguration, LoggerConfiguration> writeFeatureMetrics) =>
            writeTo
                .Conditional(logEvent => !IsFeatureMetricEvent(logEvent), otherEvents => writeOtherEvents(otherEvents))
                .WriteTo.Conditional(IsFeatureMetricEvent, featureMetrics => writeFeatureMetrics(featureMetrics));
    }

    // Serilog.Sinks.OpenTelemetry applies the OTEL_EXPORTER_OTLP_* variables after the options callback, so the
    // pod's own OTEL_EXPORTER_OTLP_ENDPOINT would replace the feature-metric endpoint. Only let through the
    // variables that describe the resource.
    private static Func<string, string?> ResourceVariablesOnly(Func<string, string?> getVariable) =>
        name => name is "OTEL_SERVICE_NAME" or "OTEL_RESOURCE_ATTRIBUTES" ? getVariable(name) : null;
}
