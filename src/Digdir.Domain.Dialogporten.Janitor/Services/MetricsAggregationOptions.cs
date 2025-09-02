using System.ComponentModel.DataAnnotations;

namespace Digdir.Domain.Dialogporten.Janitor.Services;

public class MetricsAggregationOptions
{
    public const string SectionName = "MetricsAggregation";

    [Required]
    public string TT02WorkspaceId { get; set; } = string.Empty;

    [Required]
    public string ProdWorkspaceId { get; set; } = string.Empty;

    [Required]
    public string StorageConnectionString { get; set; } = string.Empty;

    [Required]
    public string StorageContainerName { get; set; } = string.Empty;

    public List<string> Environments { get; set; } = ["TT02", "PROD"];
}
