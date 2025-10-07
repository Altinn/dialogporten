using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Janitor.Services;

public class MetricsAggregationService
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

        var aggregated = rawMetrics
            .GroupBy(r => new
            {
                r.Environment,
                r.ServiceOrg,
                r.ServiceResource,
                TransactionType = TransactionTypeMapper.MapFeatureTypeToTransactionType(r.FeatureType, r.PresentationTag),
                Failed = r.Status is "failure" or "error"
            })
            .Select(group => new AggregatedMetricsRecord
            {
                Miljø = group.Key.Environment,
                Tjeneste = group.Key.ServiceResource,
                Tjenesteeierkode = group.Key.ServiceOrg,
                Transaksjonstype = CostCoefficients.GetNorwegianName(group.Key.TransactionType),
                Feilet = group.Key.Failed ? "Yes" : "No",
                Antall = group.Sum(r => r.Count),
                RelativRessursbruk = group.Sum(r => r.Count * _costCoefficients.GetCoefficient(group.Key.TransactionType))
            })
            .OrderBy(r => r.Miljø)
            .ThenBy(r => r.Tjenesteeierkode)
            .ThenBy(r => r.Tjeneste)
            .ThenBy(r => r.Transaksjonstype)
            .ToList();

        _logger.LogInformation("Aggregated into {AggregatedCount} records", aggregated.Count);
        return aggregated;
    }
}

public class AggregatedMetricsRecord
{
    public string Miljø { get; set; } = string.Empty;
    public string Tjeneste { get; set; } = string.Empty;
    public string Tjenesteeierkode { get; set; } = string.Empty;
    public string Transaksjonstype { get; set; } = string.Empty;
    public string Feilet { get; set; } = string.Empty;
    public long Antall { get; set; }
    public decimal RelativRessursbruk { get; set; }
}
