using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Janitor.CostManagementAggregation;

public sealed class CostCoefficientsOptionsValidator : IValidateOptions<CostCoefficientsOptions>
{
    public ValidateOptionsResult Validate(string? name, CostCoefficientsOptions options)
    {
        var failures = new List<string>();

        if (options.CreateDialog <= 0)
            failures.Add($"{nameof(CostCoefficientsOptions.CreateDialog)} must be greater than 0, but was {options.CreateDialog}");

        if (options.UpdateDialog <= 0)
            failures.Add($"{nameof(CostCoefficientsOptions.UpdateDialog)} must be greater than 0, but was {options.UpdateDialog}");

        if (options.SoftDeleteDialog <= 0)
            failures.Add($"{nameof(CostCoefficientsOptions.SoftDeleteDialog)} must be greater than 0, but was {options.SoftDeleteDialog}");

        if (options.HardDeleteDialog <= 0)
            failures.Add($"{nameof(CostCoefficientsOptions.HardDeleteDialog)} must be greater than 0, but was {options.HardDeleteDialog}");

        if (options.GetDialogServiceOwner <= 0)
            failures.Add($"{nameof(CostCoefficientsOptions.GetDialogServiceOwner)} must be greater than 0, but was {options.GetDialogServiceOwner}");

        if (options.GetDialogEndUser <= 0)
            failures.Add($"{nameof(CostCoefficientsOptions.GetDialogEndUser)} must be greater than 0, but was {options.GetDialogEndUser}");

        if (options.SearchDialogsServiceOwner <= 0)
            failures.Add($"{nameof(CostCoefficientsOptions.SearchDialogsServiceOwner)} must be greater than 0, but was {options.SearchDialogsServiceOwner}");

        if (options.SearchDialogsServiceOwnerWithEndUser <= 0)
            failures.Add($"{nameof(CostCoefficientsOptions.SearchDialogsServiceOwnerWithEndUser)} must be greater than 0, but was {options.SearchDialogsServiceOwnerWithEndUser}");

        if (options.SearchDialogsEndUser <= 0)
            failures.Add($"{nameof(CostCoefficientsOptions.SearchDialogsEndUser)} must be greater than 0, but was {options.SearchDialogsEndUser}");

        if (options.SetDialogLabel <= 0)
            failures.Add($"{nameof(CostCoefficientsOptions.SetDialogLabel)} must be greater than 0, but was {options.SetDialogLabel}");

        if (options.BulkSetLabelsServiceOwnerWithEndUser <= 0)
            failures.Add($"{nameof(CostCoefficientsOptions.BulkSetLabelsServiceOwnerWithEndUser)} must be greater than 0, but was {options.BulkSetLabelsServiceOwnerWithEndUser}");

        if (options.BulkSetLabelsEndUser <= 0)
            failures.Add($"{nameof(CostCoefficientsOptions.BulkSetLabelsEndUser)} must be greater than 0, but was {options.BulkSetLabelsEndUser}");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
