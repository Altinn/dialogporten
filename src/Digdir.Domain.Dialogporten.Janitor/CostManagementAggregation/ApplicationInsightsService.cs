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

    private string GetSubscriptionId(string environment) =>
        environment.ToLowerInvariant() switch
        {
            "staging" => _options.StagingSubscriptionId,
            "prod" => _options.ProdSubscriptionId,
            "test" => _options.TestSubscriptionId,
            "yt01" => _options.Yt01SubscriptionId,
            _ => throw new ArgumentException($"Unknown environment: {environment}. Valid values are: staging, prod, test, yt01")
        };

    public async Task<List<FeatureMetricRecord>> QueryFeatureMetricsAsync(DateOnly targetDate, string environment, CancellationToken cancellationToken = default)
    {
        var (startTime, endTime) = NorwegianTimeConverter.GetDayRangeInUtc(targetDate);

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
            | extend CallerOrg = tostring(customDimensions["CallerOrg"])
            | extend OwnerOrg = tostring(customDimensions["OwnerOrg"])
            | extend ServiceResource = tostring(customDimensions["ServiceResource"])
            | extend PresentationTag = tostring(customDimensions["PresentationTag"])
            | extend AdditionalTags = tostring(customDimensions["AdditionalTags"])
            | extend AdditionalTagsParsed = parse_json(AdditionalTags)
            | extend Status = tostring(AdditionalTagsParsed["Status"])
            | extend StatusCode = tostring(AdditionalTagsParsed["StatusCode"])
            | summarize count() by FeatureType, HasAdminScope, Environment, CallerOrg, OwnerOrg, ServiceResource, PresentationTag, Status, StatusCode
            | where isnotempty(FeatureType)
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

    private List<FeatureMetricRecord> ParseQueryResponse(LogsQueryResult result, string environment)
    {
        var records = new List<FeatureMetricRecord>();
        var failedCount = 0;
        var totalRows = result.Table.Rows.Count;

        for (var i = 0; i < totalRows; i++)
        {
            var row = result.Table.Rows[i];
            try
            {
                var record = new FeatureMetricRecord
                {
                    Environment = environment,
                    FeatureType = GetStringOrDefault(row[0]),
                    HasAdminScope = GetStringOrDefault(row[1]).Equals("true", StringComparison.OrdinalIgnoreCase),
                    EnvironmentFromLog = GetStringOrDefault(row[2]),
                    CallerOrg = GetStringOrDefault(row[3]),
                    OwnerOrg = GetStringOrDefault(row[4]),
                    ServiceResource = GetStringOrDefault(row[5]),
                    PresentationTag = GetStringOrDefault(row[6]),
                    Status = GetStringOrDefault(row[7]),
                    StatusCode = GetStringOrDefault(row[8]),
                    Count = Convert.ToInt64(row[9] ?? 0, System.Globalization.CultureInfo.InvariantCulture)
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

    private string GetResourceId(string environment)
    {
        var subscriptionId = GetSubscriptionId(environment);

        // Construct resource ID using the Azure environment name directly
        var resourceId = $"/subscriptions/{subscriptionId}/resourceGroups/dp-be-{environment}-rg/providers/microsoft.insights/components/dp-be-{environment}-applicationInsights";

        return resourceId;
    }
}

public sealed class FeatureMetricRecord
{
    public required string Environment { get; init; }
    public required string FeatureType { get; init; }
    public required bool HasAdminScope { get; init; }
    public required string EnvironmentFromLog { get; init; }
    public required string CallerOrg { get; init; }
    public required string OwnerOrg { get; init; }
    public required string ServiceResource { get; init; }
    public required string PresentationTag { get; init; }
    public required string Status { get; init; }
    public required string StatusCode { get; init; }
    public required long Count { get; init; }
}
