using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Janitor.CostManagementAggregation;

public sealed partial class MetricsAggregationService
{
    // Used by source-generated logging partials; analyzers don't see the generated usage.
#pragma warning disable IDE0052
    private readonly ILogger<MetricsAggregationService> _logger;
#pragma warning restore IDE0052

    private readonly CostCoefficients _costCoefficients;

    public MetricsAggregationService(ILogger<MetricsAggregationService> logger, CostCoefficients costCoefficients)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _costCoefficients = costCoefficients ?? throw new ArgumentNullException(nameof(costCoefficients));
    }

    public List<AggregatedCostMetricsRecord> AggregateFeatureMetrics(List<CostMetricRecord> rawMetrics)
    {
        ArgumentNullException.ThrowIfNull(rawMetrics);
        LogAggregatingRawFeatureMetrics(rawMetrics.Count);

        // Map feature types to transaction types and filter out unmapped ones
        var mappedMetrics = rawMetrics
            .Select(r => new
            {
                Record = r,
                TransactionType = TransactionTypeMapper.MapFeatureTypeToTransactionType(r.FeatureType, r.PresentationTag)
            })
            .Where(x => x.TransactionType.HasValue)
            .ToList();

        var excludedCount = rawMetrics.Count - mappedMetrics.Count;
        if (excludedCount > 0)
        {
            LogExcludedFeatureMetrics(excludedCount);
        }

        // TODO: Add lookup for org short name
        var aggregated = mappedMetrics
            .GroupBy(x => new
            {
                x.Record.Environment,
                x.Record.CallerOrg,
                x.Record.OwnerOrg,
                x.Record.HasAdminScope,
                x.Record.ServiceResource,
                TransactionType = x.TransactionType!.Value,
                Failed = string.Equals(x.Record.Status, "failure", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(x.Record.Status, "error", StringComparison.OrdinalIgnoreCase)
            })
            .Select(group => new AggregatedCostMetricsRecord
            {
                Environment = ConvertToUserFacingEnvironmentName(group.Key.Environment),
                HasAdminScope = group.Key.HasAdminScope ? "Ja" : "Nei",
                Service = group.Key.ServiceResource,
                ConsumerOrgNumber = group.Key.CallerOrg,
                OwnerOrgNumber = group.Key.OwnerOrg,
                TransactionType = CostCoefficients.GetNorwegianName(group.Key.TransactionType),
                Failed = group.Key.Failed ? "Ja" : "Nei",
                Count = group.Sum(x => x.Record.Count),
                RelativeResourceUsage = group.Sum(x => x.Record.Count * _costCoefficients.GetCoefficient(group.Key.TransactionType))
            })
            .OrderBy(r => r.Environment)
            .ThenBy(r => r.OwnerOrgNumber)
            .ThenBy(r => r.Service)
            .ThenBy(r => r.TransactionType)
            .ToList();

        LogAggregatedFeatureMetrics(aggregated.Count);
        return aggregated;
    }

    private static string ConvertToUserFacingEnvironmentName(string azureEnvironmentName) =>
        azureEnvironmentName.ToLowerInvariant() switch
        {
            "staging" => "TT02",
            "prod" => "PROD",
            "test" => "TEST",
            "yt01" => "YT01",
            _ => azureEnvironmentName // Return as-is if unknown
        };

    [LoggerMessage(Level = LogLevel.Information, Message = "Aggregating {RecordCount} raw feature metric records")]
    private partial void LogAggregatingRawFeatureMetrics(int recordCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Excluded {ExcludedCount} feature metrics with no transaction type mapping from cost aggregation")]
    private partial void LogExcludedFeatureMetrics(int excludedCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Aggregated into {AggregatedCount} records")]
    private partial void LogAggregatedFeatureMetrics(int aggregatedCount);
}

public sealed class AggregatedCostMetricsRecord
{
    public required string Environment { get; init; }
    public required string HasAdminScope { get; init; }
    public required string Service { get; init; }
    public required string ConsumerOrgNumber { get; init; }
    public required string OwnerOrgNumber { get; init; }
    public required string TransactionType { get; init; }
    public required string Failed { get; init; }
    public required long Count { get; init; }
    public required decimal RelativeResourceUsage { get; init; }
}
