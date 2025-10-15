using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Janitor.CostManagementAggregation;

public sealed class MetricsAggregationService
{
    private readonly ILogger<MetricsAggregationService> _logger;
    private readonly CostCoefficients _costCoefficients;

    public MetricsAggregationService(ILogger<MetricsAggregationService> logger, CostCoefficients costCoefficients)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _costCoefficients = costCoefficients ?? throw new ArgumentNullException(nameof(costCoefficients));
    }

    public List<AggregatedMetricsRecord> AggregateFeatureMetrics(List<FeatureMetricRecord> rawMetrics)
    {
        _logger.LogInformation("Aggregating {RecordCount} raw feature metric records", rawMetrics.Count);

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
            _logger.LogInformation("Excluded {ExcludedCount} feature metrics with no transaction type mapping from cost aggregation", excludedCount);
        }

        // TODO: Add lookup for org short name
        var aggregated = mappedMetrics
            .GroupBy(x => new
            {
                x.Record.Environment,
                x.Record.CallerOrg,
                x.Record.OwnerOrg,
                x.Record.ServiceResource,
                TransactionType = x.TransactionType!.Value,
                Failed = string.Equals(x.Record.Status, "failure", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(x.Record.Status, "error", StringComparison.OrdinalIgnoreCase)
            })
            .Select(group => new AggregatedMetricsRecord
            {
                Miljø = ConvertToUserFacingEnvironmentName(group.Key.Environment),
                Tjeneste = group.Key.ServiceResource,
                Konsumentorgnr = group.Key.CallerOrg,
                Tjenesteeierorgnr = group.Key.OwnerOrg,
                Transaksjonstype = CostCoefficients.GetNorwegianName(group.Key.TransactionType),
                Feilet = group.Key.Failed ? "Yes" : "No",
                Antall = group.Sum(x => x.Record.Count),
                RelativRessursbruk = group.Sum(x => x.Record.Count * _costCoefficients.GetCoefficient(group.Key.TransactionType))
            })
            .OrderBy(r => r.Miljø)
            .ThenBy(r => r.Tjenesteeierorgnr)
            .ThenBy(r => r.Tjeneste)
            .ThenBy(r => r.Transaksjonstype)
            .ToList();

        _logger.LogInformation("Aggregated into {AggregatedCount} records", aggregated.Count);
        return aggregated;
    }

    private static string ConvertToUserFacingEnvironmentName(string azureEnvironmentName)
    {
        return azureEnvironmentName.ToLowerInvariant() switch
        {
            "staging" => "TT02",
            "prod" => "PROD",
            "test" => "TEST",
            "yt01" => "YT01",
            _ => azureEnvironmentName // Return as-is if unknown
        };
    }
}

public sealed class AggregatedMetricsRecord
{
    public required string Miljø { get; init; }
    public required string Tjeneste { get; init; }
    public required string Konsumentorgnr { get; init; }
    public required string Tjenesteeierorgnr { get; init; }
    public required string Transaksjonstype { get; init; }
    public required string Feilet { get; init; }
    public required long Antall { get; init; }
    public required decimal RelativRessursbruk { get; init; }
}
