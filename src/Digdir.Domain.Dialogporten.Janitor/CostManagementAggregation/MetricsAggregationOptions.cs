using System.ComponentModel.DataAnnotations;

namespace Digdir.Domain.Dialogporten.Janitor.CostManagementAggregation;

public sealed class MetricsAggregationOptions
{
    public const string SectionName = "MetricsAggregation";

    [Required]
    public string StagingSubscriptionId { get; set; } = string.Empty;

    [Required]
    public string ProdSubscriptionId { get; set; } = string.Empty;

    public string TestSubscriptionId { get; set; } = string.Empty;

    public string Yt01SubscriptionId { get; set; } = string.Empty;

    [Required]
    public string StorageConnectionString { get; set; } = string.Empty;

    [Required]
    public string StorageContainerName { get; set; } = string.Empty;
}
