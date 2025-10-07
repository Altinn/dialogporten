using System.ComponentModel.DataAnnotations;

namespace Digdir.Domain.Dialogporten.Janitor.Services;

public class MetricsAggregationOptions
{
    public const string SectionName = "MetricsAggregation";

    public string DevResourceId { get; set; } = string.Empty;

    public string TT02ResourceId { get; set; } = string.Empty;

    public string ProdResourceId { get; set; } = string.Empty;

    public string StorageConnectionString { get; set; } = string.Empty;

    public string StorageContainerName { get; set; } = string.Empty;

    public List<string> Environments { get; set; } = ["TT02", "PROD"];

    public bool SkipUpload { get; set; }
}
