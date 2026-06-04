using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Wordlist.Commands.GenerateWordlist;

internal sealed class GenerateWordlistCommandValidator : AbstractValidator<GenerateWordlistCommand>
{
    public GenerateWordlistCommandValidator()
    {
        RuleFor(x => x.SampleSize).InclusiveBetween(3, 100).When(x => x.SampleSize.HasValue);
        RuleFor(x => x.PoolRows).GreaterThan(0).When(x => x.PoolRows.HasValue);
        RuleFor(x => x.MinLength).GreaterThan(0).When(x => x.MinLength.HasValue);
        RuleFor(x => x.OutputPath).NotEmpty().When(x => x.OutputPath is not null);
        RuleFor(x => x.Languages)
            .Must(x => x is null || x.Count > 0)
            .WithMessage("Languages must be non-empty when specified.");
    }
}
