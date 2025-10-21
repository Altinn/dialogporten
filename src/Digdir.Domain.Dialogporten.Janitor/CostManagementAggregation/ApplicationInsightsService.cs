using Azure;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Janitor.CostManagementAggregation;

public sealed class ApplicationInsightsService
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

    private sealed class LogTableColumnNames
    {
        public const string FeatureType = "FeatureType";
        public const string HasAdminScope = "HasAdminScope";
        public const string CallerOrg = "CallerOrg";
        public const string OwnerOrg = "OwnerOrg";
        public const string ServiceResource = "ServiceResource";
        public const string PresentationTag = "PresentationTag";
        public const string Status = "Status";
        public const string StatusCode = "StatusCode";
        public const string Count = "count_";
    }

    public async Task<List<CostMetricRecord>> QueryFeatureMetricsAsync(DateTimeOffset startTime, DateTimeOffset endTime, string environment, CancellationToken cancellationToken = default)
    {
        var resourceId = GetResourceId(environment);

        _logger.LogInformation("Using Resource ID: {ResourceId}", resourceId);

        var kql = $$"""
             traces
             | where timestamp >= datetime({0:yyyy-MM-dd HH:mm:ss.fff})
             | where timestamp <= datetime({1:yyyy-MM-dd HH:mm:ss.fff})
             | extend EventIdNum = toint(extractjson("$.Id", tostring(customDimensions.EventId)))
             | where isnotnull(EventIdNum) and EventIdNum == 1000
             | extend {{LogTableColumnNames.FeatureType}} = tostring(customDimensions["{{LogTableColumnNames.FeatureType}}"])
             | extend {{LogTableColumnNames.HasAdminScope}} = tobool(customDimensions["{{LogTableColumnNames.HasAdminScope}}"])
             | extend {{LogTableColumnNames.CallerOrg}} = tostring(customDimensions["{{LogTableColumnNames.CallerOrg}}"])
             | extend {{LogTableColumnNames.OwnerOrg}} = tostring(customDimensions["{{LogTableColumnNames.OwnerOrg}}"])
             | extend {{LogTableColumnNames.ServiceResource}} = tostring(customDimensions["{{LogTableColumnNames.ServiceResource}}"])
             | extend {{LogTableColumnNames.PresentationTag}} = tostring(customDimensions["{{LogTableColumnNames.PresentationTag}}"])
             | extend AdditionalTags = tostring(customDimensions["AdditionalTags"])
             | extend AdditionalTagsParsed = parse_json(AdditionalTags)
             | extend {{LogTableColumnNames.Status}} = tostring(AdditionalTagsParsed["{{LogTableColumnNames.Status}}"])
             | extend {{LogTableColumnNames.StatusCode}} = tostring(AdditionalTagsParsed["{{LogTableColumnNames.StatusCode}}"])
             | summarize count() by {{LogTableColumnNames.FeatureType}}, {{LogTableColumnNames.HasAdminScope}}, {{LogTableColumnNames.CallerOrg}}, {{LogTableColumnNames.OwnerOrg}}, {{LogTableColumnNames.ServiceResource}}, {{LogTableColumnNames.PresentationTag}}, {{LogTableColumnNames.Status}}, {{LogTableColumnNames.StatusCode}}
             | where isnotempty({{LogTableColumnNames.FeatureType}})
             """;

        var formattedKql = string.Format(System.Globalization.CultureInfo.InvariantCulture, kql, startTime, endTime);

        _logger.LogInformation("Querying Application Insights for {Environment} from {StartTime} UTC to {EndTime} UTC",
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

    private static string GetStringOrDefault(object? value, string defaultValue = "unknown")
        => value?.ToString() ?? defaultValue;

    private List<CostMetricRecord> ParseQueryResponse(LogsQueryResult result, string environment)
    {
        var records = new List<CostMetricRecord>();
        var failedCount = 0;
        var totalRows = result.Table.Rows.Count;

        for (var i = 0; i < totalRows; i++)
        {
            var row = result.Table.Rows[i];
            try
            {
                var record = new CostMetricRecord
                {
                    Environment = environment,
                    FeatureType = GetStringOrDefault(row[LogTableColumnNames.FeatureType]),
                    HasAdminScope = GetStringOrDefault(row[LogTableColumnNames.HasAdminScope]).Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase),
                    CallerOrg = GetStringOrDefault(row[LogTableColumnNames.CallerOrg]),
                    OwnerOrg = GetStringOrDefault(row[LogTableColumnNames.OwnerOrg]),
                    ServiceResource = GetStringOrDefault(row[LogTableColumnNames.ServiceResource]),
                    PresentationTag = GetStringOrDefault(row[LogTableColumnNames.PresentationTag]),
                    Status = GetStringOrDefault(row[LogTableColumnNames.Status]),
                    StatusCode = GetStringOrDefault(row[LogTableColumnNames.StatusCode]),
                    Count = Convert.ToInt64(row[LogTableColumnNames.Count] ?? 0, System.Globalization.CultureInfo.InvariantCulture)
                };

                records.Add(record);
            }
            catch (Exception ex)
            {
                failedCount++;
                _logger.LogWarning(ex, "Failed to parse row {RowIndex} from Application Insights response", i);
            }
        }

        if (failedCount > 0)
        {
            var failureRate = (double)failedCount / totalRows;
            _logger.LogWarning("Failed to parse {FailedCount} of {TotalCount} rows ({FailureRate:P1}) for environment {Environment}",
                failedCount, totalRows, failureRate, environment);
        }

        _logger.LogInformation("Parsed {RecordCount} feature metric records for environment {Environment}",
            records.Count, environment);

        return records;
    }

    private string GetResourceId(string environment) =>
        $"/subscriptions/{_options.SubscriptionId.ToLowerInvariant()}/resourceGroups/dp-be-{environment.ToLowerInvariant()}-rg" +
        $"/providers/microsoft.insights/components/dp-be-{environment.ToLowerInvariant()}-applicationInsights";
}

public sealed class CostMetricRecord
{
    public required string Environment { get; init; }
    public required string FeatureType { get; init; }
    public required bool HasAdminScope { get; init; }
    public required string CallerOrg { get; init; }
    public required string OwnerOrg { get; init; }
    public required string ServiceResource { get; init; }
    public required string PresentationTag { get; init; }
    public required string Status { get; init; }
    public required string StatusCode { get; init; }
    public required long Count { get; init; }
}
