namespace Digdir.Domain.Dialogporten.Janitor.Services;

public class CostCoefficientsOptions
{
    public const string SectionName = "CostCoefficients";

    public decimal CreateDialog { get; set; } = 1.5m;
    public decimal UpdateDialog { get; set; } = 1.2m;
    public decimal SoftDeleteDialog { get; set; } = 1.0m;
    public decimal HardDeleteDialog { get; set; } = 1.0m;
    public decimal GetDialogServiceOwner { get; set; } = 1.0m;
    public decimal GetDialogEndUser { get; set; } = 1.0m;
    public decimal SearchDialogsServiceOwner { get; set; } = 2.0m;
    public decimal SearchDialogsServiceOwnerWithEndUser { get; set; } = 2.2m;
    public decimal SearchDialogsEndUser { get; set; } = 2.5m;
    public decimal SetDialogLabel { get; set; } = 1.0m;
    public decimal BulkSetLabelsServiceOwnerWithEndUser { get; set; } = 3.0m;
    public decimal BulkSetLabelsEndUser { get; set; } = 3.5m;
}