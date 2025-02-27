using System.Diagnostics;
using System.Globalization;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Configuration;
using Serilog.Sinks.OpenTelemetry;

namespace Digdir.Library.Utils.AspNet;

public static class OpenTelemetryExtensions
{
    private const string OtelExporterOtlpEndpoint = "OTEL_EXPORTER_OTLP_ENDPOINT";
    private const string OtelExporterOtlpProtocol = "OTEL_EXPORTER_OTLP_PROTOCOL";

    public static IServiceCollection AddDialogportenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        Action<TracerProviderBuilder>? additionalTracing = null,
        Action<MeterProviderBuilder>? additionalMetrics = null)
    {
        if (!Uri.IsWellFormedUriString(configuration[OtelExporterOtlpEndpoint], UriKind.Absolute))
            return services;

        var otlpProtocol = configuration[OtelExporterOtlpProtocol]?.ToLowerInvariant() switch
        {
            "grpc" => OtlpExportProtocol.Grpc,
            "http/protobuf" => OtlpExportProtocol.HttpProtobuf,
            _ => throw new ArgumentException($"Unsupported protocol: {configuration[OtelExporterOtlpProtocol]}")
        };

        var endpoint = new Uri(configuration[OtelExporterOtlpEndpoint]!);

        return services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(
                    serviceName: configuration["OTEL_SERVICE_NAME"] ?? environment.ApplicationName);
            })
            .WithTracing(tracing =>
            {
                if (environment.IsDevelopment())
                {
                    tracing.SetSampler(new AlwaysOnSampler());
                }

                tracing
                    .AddHttpClientInstrumentation(o =>
                    {
                        o.RecordException = true;
                        o.FilterHttpRequestMessage = message =>
                        {
                            var parentActivity = Activity.Current?.Parent;
                            if (parentActivity != null && parentActivity.Source.Name.Equals("Azure.Core.Http", StringComparison.Ordinal))
                            {
                                return false;
                            }

                            // Filter out Key Vault requests
                            if (message.RequestUri?.Host.EndsWith(".vault.azure.net", StringComparison.OrdinalIgnoreCase) == true)
                            {
                                return false;
                            }

                            return true;
                        };
                    })
                    .AddNpgsql()
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(endpoint, "/v1/traces");
                        options.Protocol = otlpProtocol;
                    });

                additionalTracing?.Invoke(tracing);
            })
            .WithMetrics(metrics =>
            {
                metrics.AddRuntimeInstrumentation()
                    .AddHttpClientInstrumentation();

                var appInsightsConnectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
                if (!string.IsNullOrEmpty(appInsightsConnectionString))
                {
                    metrics.AddAzureMonitorMetricExporter(options =>
                    {
                        options.ConnectionString = appInsightsConnectionString;
                    });
                }
                else
                {
                    metrics.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(endpoint, "/v1/metrics");
                        options.Protocol = otlpProtocol;
                    });
                }

                additionalMetrics?.Invoke(metrics);
            })
            .Services;
    }

    public static LoggerConfiguration OpenTelemetryOrConsole(this LoggerSinkConfiguration writeTo, HostBuilderContext context)
    {
        var otelEndpoint = context.Configuration[OtelExporterOtlpEndpoint];
        var otelProtocol = context.Configuration[OtelExporterOtlpProtocol];

        return otelEndpoint switch
        {
            null =>
                writeTo.Console(formatProvider: CultureInfo.InvariantCulture),
            not null when Uri.IsWellFormedUriString(otelEndpoint, UriKind.Absolute) =>
                writeTo.OpenTelemetry(ConfigureOtlpSink(otelEndpoint, ParseOtlpProtocol(otelProtocol))),
            _ => throw new InvalidOperationException($"Invalid otel endpoint: {otelEndpoint}")
        };
    }

    public static LoggerConfiguration TryWriteToOpenTelemetry(this LoggerConfiguration config)
    {
        var otelEndpoint = Environment.GetEnvironmentVariable(OtelExporterOtlpEndpoint);
        var otelProtocol = Environment.GetEnvironmentVariable(OtelExporterOtlpProtocol);

        if (otelEndpoint is null || !Uri.IsWellFormedUriString(otelEndpoint, UriKind.Absolute))
        {
            return config;
        }

        try
        {
            var protocol = ParseOtlpProtocol(otelProtocol);
            return config.WriteTo.OpenTelemetry(ConfigureOtlpSink(otelEndpoint, protocol));
        }
        catch (ArgumentException)
        {
            return config;
        }
    }

    private static OtlpProtocol ParseOtlpProtocol(string? protocol)
    {
        return protocol?.ToLowerInvariant() switch
        {
            "grpc" => OtlpProtocol.Grpc,
            "http/protobuf" => OtlpProtocol.HttpProtobuf,
            _ => throw new ArgumentException($"Unsupported OTLP protocol: {protocol}")
        };
    }

    private static Action<OpenTelemetrySinkOptions> ConfigureOtlpSink(string endpoint, OtlpProtocol protocol) =>
        options =>
        {
            options.Endpoint = endpoint;
            options.Protocol = protocol;
        };

    public static TracerProviderBuilder AddAspNetCoreInstrumentationExcludingHealthPaths(this TracerProviderBuilder builder)
    {
        return builder.AddAspNetCoreInstrumentation(opts =>
        {
            opts.RecordException = true;
            opts.Filter = httpContext => !httpContext.Request.Path.StartsWithSegments("/health");
        });
    }
}