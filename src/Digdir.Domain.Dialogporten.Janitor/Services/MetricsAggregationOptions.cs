using System.ComponentModel.DataAnnotations;

namespace Digdir.Domain.Dialogporten.Janitor.Services;

public class MetricsAggregationOptions
{
    public const string SectionName = "MetricsAggregation";

    public string TT02WorkspaceId { get; set; } = string.Empty;

    public string ProdWorkspaceId { get; set; } = string.Empty;

    public string StorageConnectionString { get; set; } = string.Empty;

    public string StorageContainerName { get; set; } = string.Empty;

    public List<string> Environments { get; set; } = ["TT02", "PROD"];

    public bool UsePrometheus { get; set; }

    public string PrometheusBaseUrl { get; set; } = "http://localhost:9090";

    public bool SkipUpload { get; set; }
}
