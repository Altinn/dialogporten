using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.SearchTerms.Commands.GenerateSearchTerms;

internal sealed class GenerateSearchTermsCommandValidator : AbstractValidator<GenerateSearchTermsCommand>
{
    public GenerateSearchTermsCommandValidator()
    {
        RuleFor(x => x.SampleSize).InclusiveBetween(3, 100).When(x => x.SampleSize.HasValue);
        RuleFor(x => x.PoolRows).GreaterThan(0).When(x => x.PoolRows.HasValue);
        RuleFor(x => x.MinLength).GreaterThan(0).When(x => x.MinLength.HasValue);
        RuleFor(x => x.Languages)
            .Must(x => x is null || x.Count > 0)
            .WithMessage("Languages must be non-empty when specified.");
        RuleFor(x => x.ExcludedOrgs)
            .Must(x => x is null || x.All(o => !string.IsNullOrWhiteSpace(o)))
            .WithMessage("ExcludedOrgs must not contain empty entries.");
        RuleFor(x => x.OutputPath)
            .Must(x => !string.IsNullOrWhiteSpace(x))
            .When(x => x.OutputPath is not null)
            .WithMessage("OutputPath must be non-empty when specified.");
    }
}
