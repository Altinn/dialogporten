using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Janitor.Services;

public class OpenTelemetryMetricsService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenTelemetryMetricsService> _logger;
    private readonly MetricsAggregationOptions _options;

    public OpenTelemetryMetricsService(
        HttpClient httpClient,
        ILogger<OpenTelemetryMetricsService> logger,
        IOptions<MetricsAggregationOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<List<MetricsRecord>> QueryCostMetricsAsync(DateOnly targetDate, string environment, CancellationToken cancellationToken = default)
    {
        if (_options.UsePrometheus)
        {
            return await QueryPrometheusMetricsAsync(targetDate, environment, cancellationToken);
        }
        else
        {
            return await QueryOtlpMetricsAsync(targetDate, environment, cancellationToken);
        }
    }

    private async Task<List<MetricsRecord>> QueryOtlpMetricsAsync(DateOnly targetDate, string environment, CancellationToken cancellationToken)
    {
        var startTime = new DateTimeOffset(targetDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        var endTime = new DateTimeOffset(targetDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

        _logger.LogInformation("Querying OTLP for {Environment} from {StartTime} to {EndTime}",
            environment, startTime, endTime);

        try
        {
            var query = new OtlpMetricsQuery
            {
                ResourceMetrics = new[]
                {
                    new OtlpResourceMetric
                    {
                        Resource = new OtlpResource
                        {
                            Attributes = new[]
                            {
                                new OtlpKeyValue { Key = "service.name", Value = new OtlpValue { StringValue = "dialogporten" } }
                            }
                        },
                        ScopeMetrics = new[]
                        {
                            new OtlpScopeMetric
                            {
                                Scope = new OtlpScope { Name = "dialogporten" },
                                Metrics = new[]
                                {
                                    new OtlpMetric
                                    {
                                        Name = "transactions_total",
                                        Description = "Total number of transactions",
                                        Unit = "1",
                                        Sum = new OtlpSum
                                        {
                                            DataPoints = new[]
                                            {
                                                new OtlpNumberDataPoint
                                                {
                                                    StartTimeUnixNano = (ulong)(startTime.ToUnixTimeMilliseconds() * 1_000_000),
                                                    TimeUnixNano = (ulong)(endTime.ToUnixTimeMilliseconds() * 1_000_000),
                                                    AsInt = 0, // Placeholder - this would be populated by the actual OTLP endpoint
                                                    Attributes = Array.Empty<OtlpKeyValue>()
                                                }
                                            },
                                            AggregationTemporality = 2, // CUMULATIVE
                                            IsMonotonic = true
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            // For now, fall back to Prometheus since OTLP query endpoints aren't standardized yet
            _logger.LogWarning("OTLP query endpoints not yet standardized, falling back to Prometheus for now");
            return await QueryPrometheusMetricsAsync(targetDate, environment, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query OTLP for environment {Environment}", environment);
            throw;
        }
    }

    private async Task<List<MetricsRecord>> QueryPrometheusMetricsAsync(DateOnly targetDate, string environment, CancellationToken cancellationToken)
    {
        var startTimestamp = new DateTimeOffset(targetDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)).ToUnixTimeSeconds();
        var endTimestamp = new DateTimeOffset(targetDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)).ToUnixTimeSeconds();

        // Use Prometheus increase() function to handle counter resets properly
        var query = "increase(dialogporten_transactions_total[1d])";
        var prometheusUrl = $"{_options.PrometheusBaseUrl}/api/v1/query_range?query={Uri.EscapeDataString(query)}&start={startTimestamp}&end={endTimestamp}&step=3600";

        _logger.LogInformation("Querying Prometheus with increase() function for {Environment} from {StartTime} to {EndTime}",
            environment, DateTimeOffset.FromUnixTimeSeconds(startTimestamp), DateTimeOffset.FromUnixTimeSeconds(endTimestamp));

        try
        {
            var response = await _httpClient.GetStringAsync(prometheusUrl, cancellationToken);
            return ParsePrometheusIncreaseResponse(response, environment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query Prometheus for environment {Environment}", environment);
            throw;
        }
    }

    private List<MetricsRecord> ParsePrometheusIncreaseResponse(string response, string environment)
    {
        var records = new List<MetricsRecord>();

        try
        {
            using var document = JsonDocument.Parse(response);
            var data = document.RootElement.GetProperty("data");
            var result = data.GetProperty("result");

            foreach (var series in result.EnumerateArray())
            {
                var metric = series.GetProperty("metric");
                var values = series.GetProperty("values");

                var transactionType = metric.TryGetProperty("transaction_type", out var tt) ? tt.GetString() ?? "unknown" : "unknown";
                var status = metric.TryGetProperty("status", out var s) ? s.GetString() ?? "unknown" : "unknown";
                var tokenOrg = metric.TryGetProperty("token_org", out var to) ? to.GetString() ?? "unknown" : "unknown";
                var serviceOrg = metric.TryGetProperty("service_org", out var so) ? so.GetString() ?? "unknown" : "unknown";
                var serviceResource = metric.TryGetProperty("service_resource", out var sr) ? sr.GetString() ?? "unknown" : "unknown";
                var httpStatusCode = metric.TryGetProperty("http_status_code", out var hsc) ? hsc.GetString() ?? "unknown" : "unknown";

                // Sum up all increase values across the day
                long totalCount = 0;
                foreach (var value in values.EnumerateArray())
                {
                    if (value.GetArrayLength() >= 2 &&
                        long.TryParse(value[1].GetString(), System.Globalization.CultureInfo.InvariantCulture, out var count))
                    {
                        totalCount += count;
                    }
                }

                if (totalCount > 0)
                {
                    records.Add(new MetricsRecord
                    {
                        Environment = environment,
                        TransactionType = transactionType,
                        Status = status,
                        TokenOrg = tokenOrg,
                        ServiceOrg = serviceOrg,
                        ServiceResource = serviceResource,
                        HttpStatusCode = httpStatusCode,
                        Count = totalCount
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse Prometheus increase() response");
        }

        _logger.LogInformation("Parsed {RecordCount} metrics records from Prometheus increase() for environment {Environment}",
            records.Count, environment);

        return records;
    }
}

// OTLP Data Transfer Objects for future use when OTLP query standards are established
public record OtlpMetricsQuery
{
    public OtlpResourceMetric[] ResourceMetrics { get; init; } = Array.Empty<OtlpResourceMetric>();
}

public record OtlpResourceMetric
{
    public OtlpResource Resource { get; init; } = new();
    public OtlpScopeMetric[] ScopeMetrics { get; init; } = Array.Empty<OtlpScopeMetric>();
}

public record OtlpResource
{
    public OtlpKeyValue[] Attributes { get; init; } = Array.Empty<OtlpKeyValue>();
}

public record OtlpScopeMetric
{
    public OtlpScope Scope { get; init; } = new();
    public OtlpMetric[] Metrics { get; init; } = Array.Empty<OtlpMetric>();
}

public record OtlpScope
{
    public string Name { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
}

public record OtlpMetric
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public OtlpSum? Sum { get; init; }
}

public record OtlpSum
{
    public OtlpNumberDataPoint[] DataPoints { get; init; } = Array.Empty<OtlpNumberDataPoint>();
    public int AggregationTemporality { get; init; }
    public bool IsMonotonic { get; init; }
}

public record OtlpNumberDataPoint
{
    public OtlpKeyValue[] Attributes { get; init; } = Array.Empty<OtlpKeyValue>();
    public ulong StartTimeUnixNano { get; init; }
    public ulong TimeUnixNano { get; init; }
    public long AsInt { get; init; }
}

public record OtlpKeyValue
{
    public string Key { get; init; } = string.Empty;
    public OtlpValue Value { get; init; } = new();
}

public record OtlpValue
{
    public string? StringValue { get; init; }
    public long? IntValue { get; init; }
    public double? DoubleValue { get; init; }
    public bool? BoolValue { get; init; }
}