using FluentValidation;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.SearchDialogs;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class SearchDialogInputValidator : AbstractValidator<SearchDialogInput>
{
    public SearchDialogInputValidator()
    {
        RuleFor(x => x.OrderBy)
            .NotNull()
            .When(x => x.ContinuationToken != null)
            .WithMessage("OrderBy must be set when ContinuationToken is set.");

        RuleFor(x => x.ContinuationToken)
            .NotNull()
            .When(x => x.OrderBy != null)
            .WithMessage("ContinuationToken must be set when OrderBy is set.");

        RuleFor(x => x.OrderBy)
            .NotEmpty()
            .When(x => x.OrderBy != null);

        RuleForEach(x => x.OrderBy)
            .Must(order =>
                new[] { order.CreatedAt.HasValue, order.UpdatedAt.HasValue, order.DueAt.HasValue }
                    .Count(x => x) == 1)
            .WithMessage("Exactly one property must be set on each OrderBy object.");
    }
}
