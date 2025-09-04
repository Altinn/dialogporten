using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Janitor.Services;

public class PrometheusService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PrometheusService> _logger;
    private readonly MetricsAggregationOptions _options;

    public PrometheusService(
        HttpClient httpClient,
        ILogger<PrometheusService> logger,
        IOptions<MetricsAggregationOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<List<MetricsRecord>> QueryCostMetricsAsync(DateOnly targetDate, string environment, CancellationToken cancellationToken = default)
    {
        var startTimestamp = new DateTimeOffset(targetDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)).ToUnixTimeSeconds();
        var endTimestamp = new DateTimeOffset(targetDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)).ToUnixTimeSeconds();

        var query = "dialogporten_transactions_total";
        var prometheusUrl = $"{_options.PrometheusBaseUrl}/api/v1/query_range?query={Uri.EscapeDataString(query)}&start={startTimestamp}&end={endTimestamp}&step=3600";

        _logger.LogInformation("Querying Prometheus for {Environment} from {StartTime} to {EndTime}",
            environment, DateTimeOffset.FromUnixTimeSeconds(startTimestamp), DateTimeOffset.FromUnixTimeSeconds(endTimestamp));

        try
        {
            var response = await _httpClient.GetStringAsync(prometheusUrl, cancellationToken);
            return ParsePrometheusResponse(response, environment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query Prometheus for environment {Environment}", environment);
            throw;
        }
    }

    private List<MetricsRecord> ParsePrometheusResponse(string response, string environment)
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
            _logger.LogWarning(ex, "Failed to parse Prometheus response");
        }

        _logger.LogInformation("Parsed {RecordCount} metrics records from Prometheus for environment {Environment}",
            records.Count, environment);

        return records;
    }
}
