using System.ComponentModel.DataAnnotations;

namespace Digdir.Domain.Dialogporten.Janitor.CostManagementAggregation;

public sealed class CostCoefficientsOptions
{
    public const string SectionName = "CostCoefficients";

    [Required]
    public decimal CreateDialog { get; set; }

    [Required]
    public decimal UpdateDialog { get; set; }

    [Required]
    public decimal SoftDeleteDialog { get; set; }

    [Required]
    public decimal HardDeleteDialog { get; set; }

    [Required]
    public decimal GetDialogServiceOwner { get; set; }

    [Required]
    public decimal GetDialogEndUser { get; set; }

    [Required]
    public decimal SearchDialogsServiceOwner { get; set; }

    [Required]
    public decimal SearchDialogsServiceOwnerWithEndUser { get; set; }

    [Required]
    public decimal SearchDialogsEndUser { get; set; }

    [Required]
    public decimal SetDialogLabel { get; set; }

    [Required]
    public decimal BulkSetLabelsServiceOwnerWithEndUser { get; set; }

    [Required]
    public decimal BulkSetLabelsEndUser { get; set; }
}