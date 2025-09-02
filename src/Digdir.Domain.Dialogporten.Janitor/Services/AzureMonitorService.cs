using Azure;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Janitor.Services;

public class AzureMonitorService
{
    private readonly LogsQueryClient _logsClient;
    private readonly ILogger<AzureMonitorService> _logger;
    private readonly MetricsAggregationOptions _options;

    public AzureMonitorService(
        LogsQueryClient logsClient,
        ILogger<AzureMonitorService> logger,
        IOptions<MetricsAggregationOptions> options)
    {
        _logsClient = logsClient ?? throw new ArgumentNullException(nameof(logsClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<List<MetricsRecord>> QueryCostMetricsAsync(DateOnly targetDate, string environment, CancellationToken cancellationToken = default)
    {
        var startTime = targetDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endTime = targetDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddTicks(-1);

        var workspaceId = GetWorkspaceId(environment);

        var kql = """
            customMetrics
            | where name == "dialogporten_transactions_total"
            | where timestamp >= datetime({0:yyyy-MM-dd HH:mm:ss.fff})
            | where timestamp <= datetime({1:yyyy-MM-dd HH:mm:ss.fff})
            | extend transaction_type = tostring(customDimensions["transaction_type"])
            | extend status = tostring(customDimensions["status"])
            | extend token_org = tostring(customDimensions["token_org"])
            | extend service_org = tostring(customDimensions["service_org"])
            | extend service_resource = tostring(customDimensions["service_resource"])
            | extend http_status_code = tostring(customDimensions["http_status_code"])
            | summarize count = sum(value) by transaction_type, status, token_org, service_org, service_resource, http_status_code
            | where isnotempty(transaction_type)
            """;

        var formattedKql = string.Format(System.Globalization.CultureInfo.InvariantCulture, kql, startTime, endTime);

        _logger.LogInformation("Querying Azure Monitor for {Environment} from {StartTime} to {EndTime}",
            environment, startTime, endTime);

        try
        {
            var response = await _logsClient.QueryWorkspaceAsync(
                workspaceId,
                formattedKql,
                new QueryTimeRange(startTime, endTime),
                cancellationToken: cancellationToken);

            return ParseQueryResponse(response.Value, environment);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to query Azure Monitor for environment {Environment}", environment);
            throw;
        }
    }

    private List<MetricsRecord> ParseQueryResponse(LogsQueryResult result, string environment)
    {
        var records = new List<MetricsRecord>();

        foreach (var row in result.Table.Rows)
        {
            try
            {
                var record = new MetricsRecord
                {
                    Environment = environment,
                    TransactionType = row[0]?.ToString() ?? "unknown",
                    Status = row[1]?.ToString() ?? "unknown",
                    TokenOrg = row[2]?.ToString() ?? "unknown",
                    ServiceOrg = row[3]?.ToString() ?? "unknown",
                    ServiceResource = row[4]?.ToString() ?? "unknown",
                    HttpStatusCode = row[5]?.ToString() ?? "unknown",
                    Count = Convert.ToInt64(row[6] ?? 0, System.Globalization.CultureInfo.InvariantCulture)
                };

                records.Add(record);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse row from Azure Monitor response");
            }
        }

        _logger.LogInformation("Parsed {RecordCount} metrics records for environment {Environment}",
            records.Count, environment);

        return records;
    }

    private string GetWorkspaceId(string environment)
    {
        return environment.ToLowerInvariant() switch
        {
            "tt02" => _options.TT02WorkspaceId,
            "prod" => _options.ProdWorkspaceId,
            _ => throw new ArgumentException($"Unknown environment: {environment}")
        };
    }
}

public class MetricsRecord
{
    public string Environment { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string TokenOrg { get; set; } = string.Empty;
    public string ServiceOrg { get; set; } = string.Empty;
    public string ServiceResource { get; set; } = string.Empty;
    public string HttpStatusCode { get; set; } = string.Empty;
    public long Count { get; set; }
}
