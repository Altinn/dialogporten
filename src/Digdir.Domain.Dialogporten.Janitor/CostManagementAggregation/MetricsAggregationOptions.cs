namespace Digdir.Domain.Dialogporten.Janitor.CostManagementAggregation;

public sealed class MetricsAggregationOptions
{
    public const string SectionName = "MetricsAggregation";

    public string StorageConnectionString { get; set; } = string.Empty;

    public string StorageAccountName { get; set; } = string.Empty;

    public string StorageContainerName { get; set; } = string.Empty;
    public string SubscriptionId { get; set; } = string.Empty;
}
