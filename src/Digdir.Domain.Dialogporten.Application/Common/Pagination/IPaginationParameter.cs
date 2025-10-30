using Digdir.Domain.Dialogporten.Application.Common.Pagination.Continuation;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Order;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.OrderOption;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Common.Pagination;

public class PaginationParameter<TOrderDefinition, TTarget>
    where TOrderDefinition : IOrderDefinition<TTarget>
{
    private readonly int _limit = PaginationConstants.DefaultLimit;

    public ContinuationTokenSet<TOrderDefinition, TTarget>? ContinuationToken { get; set; }

    public int? Limit
    {
        get => _limit;
        init => _limit = value ?? PaginationConstants.DefaultLimit;
    }
}

public class SortablePaginationParameter<TOrderDefinition, TTarget> : PaginationParameter<TOrderDefinition, TTarget>
    where TOrderDefinition : IOrderDefinition<TTarget>
{
    public virtual OrderSet<TOrderDefinition, TTarget>? OrderBy { get; set; } = OrderSet<TOrderDefinition, TTarget>.Default;
}

internal sealed class PaginationParameterValidator<TOrderDefinition, TTarget> : AbstractValidator<PaginationParameter<TOrderDefinition, TTarget>>
    where TOrderDefinition : IOrderDefinition<TTarget>
{
    public PaginationParameterValidator()
    {
        RuleFor(x => x.Limit).InclusiveBetween(PaginationConstants.MinLimit, PaginationConstants.MaxLimit);
        RuleFor(x => x.ContinuationToken)
            .Must((paginationParameter, continuationTokenSet, ctx) =>
            {
                if (continuationTokenSet is null)
                {
                    return true;
                }

                var orders = OrderSet<TOrderDefinition, TTarget>.Default.Orders;
                if (paginationParameter is SortablePaginationParameter<TOrderDefinition, TTarget> sortable
                    && sortable.OrderBy?.Orders is not null)
                {
                    orders = sortable.OrderBy.Orders;
                }

                var missingTokenKeys = orders.Select(x => x.Key)
                    .Except(continuationTokenSet.Tokens.Select(x => x.Key), StringComparer.InvariantCultureIgnoreCase)
                    .ToList();
                ctx.MessageFormatter.AppendArgument("MissingTokenKeys", string.Join(',', missingTokenKeys));
                return missingTokenKeys.Count == 0;
            })
            .WithMessage("{PropertyName} does not match OrderBy. Missing token keys: [{MissingTokenKeys}].");
    }
}
