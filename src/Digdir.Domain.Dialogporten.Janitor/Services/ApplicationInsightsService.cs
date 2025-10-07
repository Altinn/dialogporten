using System.Text.Json;
using Azure;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Janitor.Services;

public class ApplicationInsightsService
{
    private readonly LogsQueryClient _logsClient;
    private readonly ILogger<ApplicationInsightsService> _logger;
    private readonly MetricsAggregationOptions _options;

    public ApplicationInsightsService(
        LogsQueryClient logsClient,
        ILogger<ApplicationInsightsService> logger,
        IOptions<MetricsAggregationOptions> options)
    {
        _logsClient = logsClient ?? throw new ArgumentNullException(nameof(logsClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<List<FeatureMetricRecord>> QueryFeatureMetricsAsync(DateOnly targetDate, string environment, CancellationToken cancellationToken = default)
    {
        var startTime = targetDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endTime = targetDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddTicks(-1);

        var resourceId = GetResourceId(environment);

        _logger.LogInformation("Using Resource ID: {ResourceId}", resourceId);

        var kql = """
            traces
            | where timestamp >= datetime({0:yyyy-MM-dd HH:mm:ss.fff})
            | where timestamp <= datetime({1:yyyy-MM-dd HH:mm:ss.fff})
            | extend EventIdNum = toint(extractjson("$.Id", tostring(customDimensions.EventId)))
            | where isnotnull(EventIdNum) and EventIdNum == 1000
            | extend FeatureType = tostring(customDimensions["FeatureType"])
            | extend HasAdminScope = tobool(customDimensions["HasAdminScope"])
            | extend Environment = tostring(customDimensions["Environment"])
            | extend TokenOrg = tostring(customDimensions["TokenOrg"])
            | extend ServiceOrg = tostring(customDimensions["ServiceOrg"])
            | extend ServiceResource = tostring(customDimensions["ServiceResource"])
            | extend PresentationTag = tostring(customDimensions["PresentationTag"])
            | extend AdditionalTags = tostring(customDimensions["AdditionalTags"])
            | extend AdditionalTagsParsed = parse_json(AdditionalTags)
            | extend Status = tostring(AdditionalTagsParsed["Status"])
            | extend StatusCode = tostring(AdditionalTagsParsed["StatusCode"])
            | summarize count() by FeatureType, HasAdminScope, Environment, TokenOrg, ServiceOrg, ServiceResource, PresentationTag, Status, StatusCode
            | where isnotempty(FeatureType)
            """;

        var formattedKql = string.Format(System.Globalization.CultureInfo.InvariantCulture, kql, startTime, endTime);

        _logger.LogInformation("Querying Application Insights for {Environment} from {StartTime} to {EndTime}",
            environment, startTime, endTime);

        try
        {
            // Use QueryResourceAsync with Application Insights Resource ID
            var response = await _logsClient.QueryResourceAsync(
                new Azure.Core.ResourceIdentifier(resourceId),
                formattedKql,
                new QueryTimeRange(startTime, endTime),
                cancellationToken: cancellationToken);

            return ParseQueryResponse(response.Value, environment);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to query Application Insights for environment {Environment}", environment);
            throw;
        }
    }

    private List<FeatureMetricRecord> ParseQueryResponse(LogsQueryResult result, string environment)
    {
        var records = new List<FeatureMetricRecord>();

        foreach (var row in result.Table.Rows)
        {
            try
            {
                var record = new FeatureMetricRecord
                {
                    Environment = environment,
                    FeatureType = row[0]?.ToString() ?? "unknown",
                    HasAdminScope = row[1]?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false,
                    EnvironmentFromLog = row[2]?.ToString() ?? "unknown",
                    TokenOrg = row[3]?.ToString() ?? "unknown",
                    ServiceOrg = row[4]?.ToString() ?? "unknown",
                    ServiceResource = row[5]?.ToString() ?? "unknown",
                    PresentationTag = row[6]?.ToString() ?? "unknown",
                    Status = row[7]?.ToString() ?? "unknown",
                    StatusCode = row[8]?.ToString() ?? "unknown",
                    Count = Convert.ToInt64(row[9] ?? 0, System.Globalization.CultureInfo.InvariantCulture)
                };

                records.Add(record);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse row from Application Insights response");
            }
        }

        _logger.LogInformation("Parsed {RecordCount} feature metric records for environment {Environment}",
            records.Count, environment);

        return records;
    }

    private string GetResourceId(string environment)
    {
        var resourceId = environment.ToLowerInvariant() switch
        {
            "development" => _options.DevResourceId,
            "tt02" => _options.TT02ResourceId,
            "prod" => _options.ProdResourceId,
            _ => throw new ArgumentException($"Unknown environment: {environment}. Valid values are: Development, TT02, PROD")
        };

        if (string.IsNullOrEmpty(resourceId))
        {
            throw new InvalidOperationException($"Resource ID for environment '{environment}' is not configured. Please set 'MetricsAggregation:{char.ToUpperInvariant(environment[0]) + environment[1..].ToLowerInvariant()}ResourceId' in user secrets or configuration.");
        }

        return resourceId;
    }
}

public class FeatureMetricRecord
{
    public string Environment { get; set; } = string.Empty;
    public string FeatureType { get; set; } = string.Empty;
    public bool HasAdminScope { get; set; }
    public string EnvironmentFromLog { get; set; } = string.Empty;
    public string TokenOrg { get; set; } = string.Empty;
    public string ServiceOrg { get; set; } = string.Empty;
    public string ServiceResource { get; set; } = string.Empty;
    public string PresentationTag { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;
    public long Count { get; set; }
}