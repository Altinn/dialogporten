using Microsoft.Extensions.Options;
using System.Reflection;

namespace Digdir.Domain.Dialogporten.Janitor.CostManagementAggregation;

public sealed class CostCoefficientsOptionsValidator : IValidateOptions<CostCoefficientsOptions>
{
    public ValidateOptionsResult Validate(string? name, CostCoefficientsOptions options)
    {
        var props = typeof(CostCoefficientsOptions)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.Name != nameof(CostCoefficientsOptions.SectionName));

        var failures = props
            .Select(property => new
            {
                property.Name,
                Value = (decimal)property.GetValue(options)!
            })
            .Where(property => property.Value <= 0)
            .Select(invalidProperty =>
                $"Cost coefficient {invalidProperty.Name} must be greater than 0, but was {invalidProperty.Value}")
            .ToList();

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
