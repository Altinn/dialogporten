namespace Digdir.Domain.Dialogporten.Janitor.CostManagementAggregation;

public sealed class CostCoefficientsOptions
{
    public const string SectionName = "CostCoefficients";

    public decimal CreateDialog { get; set; } = 1.5m;
    public decimal UpdateDialog { get; set; } = 1.8m;
    public decimal SoftDeleteDialog { get; set; } = 0.7m;
    public decimal HardDeleteDialog { get; set; } = 0.7m;
    public decimal GetDialogServiceOwner { get; set; } = 1.0m;
    public decimal GetDialogEndUser { get; set; } = 1.2m;
    public decimal SearchDialogsServiceOwner { get; set; } = 1.5m;
    public decimal SearchDialogsServiceOwnerWithEndUser { get; set; } = 2.5m;
    public decimal SearchDialogsEndUser { get; set; } = 2.5m;
    public decimal SetDialogLabel { get; set; } = 1.3m;
    public decimal BulkSetLabelsServiceOwnerWithEndUser { get; set; } = 2.0m;
    public decimal BulkSetLabelsEndUser { get; set; } = 2.0m;
}