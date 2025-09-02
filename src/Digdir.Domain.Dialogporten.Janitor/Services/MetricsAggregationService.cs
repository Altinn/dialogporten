using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Janitor.Services;

public class MetricsAggregationService
{
    private readonly ILogger<MetricsAggregationService> _logger;

    public MetricsAggregationService(ILogger<MetricsAggregationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public List<AggregatedMetricsRecord> AggregateMetrics(List<MetricsRecord> rawMetrics)
    {
        _logger.LogInformation("Aggregating {RecordCount} raw metrics records", rawMetrics.Count);

        var aggregated = rawMetrics
            .GroupBy(r => new
            {
                r.Environment,
                r.ServiceOrg,
                r.ServiceResource,
                r.TransactionType,
                Failed = r.Status == "failed"
            })
            .Select(group => new AggregatedMetricsRecord
            {
                Environment = group.Key.Environment,
                Service = group.Key.ServiceResource,
                ServiceOwnerCode = group.Key.ServiceOrg,
                TransactionType = CostCoefficients.GetNorwegianName(ParseTransactionType(group.Key.TransactionType)),
                Failed = group.Key.Failed ? "Yes" : "No",
                Count = group.Sum(r => r.Count),
                RelativeResourceUsage = group.Sum(r => r.Count * CostCoefficients.GetCoefficient(ParseTransactionType(group.Key.TransactionType)))
            })
            .OrderBy(r => r.Environment)
            .ThenBy(r => r.ServiceOwnerCode)
            .ThenBy(r => r.Service)
            .ThenBy(r => r.TransactionType)
            .ToList();

        _logger.LogInformation("Aggregated into {AggregatedCount} records", aggregated.Count);
        return aggregated;
    }

    private static TransactionType ParseTransactionType(string transactionTypeString)
    {
        if (Enum.TryParse<TransactionType>(transactionTypeString, true, out var parsed))
        {
            return parsed;
        }

        return TransactionType.CreateDialog; // Default fallback
    }
}

public class AggregatedMetricsRecord
{
    public string Environment { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public string ServiceOwnerCode { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public string Failed { get; set; } = string.Empty;
    public long Count { get; set; }
    public decimal RelativeResourceUsage { get; set; }
}
