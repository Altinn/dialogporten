using Digdir.Domain.Dialogporten.Application.Common.Pagination.Continuation;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Order;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.OrderOption;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Application.Common.Pagination;

public static class PaginationExtensions
{
    public static Task<PaginatedList<TTarget>> ToPaginatedListAsync<TOrderDefinition, TTarget>
        (this IQueryable<TTarget> queryable,
        SortablePaginationParameter<TOrderDefinition, TTarget> parameter,
        CancellationToken cancellationToken = default)
        where TOrderDefinition : IOrderDefinition<TTarget>
        => CreateAsync(
            queryable,
            parameter.OrderBy.DefaultIfNull(),
            parameter.ContinuationToken,
            parameter.Limit!.Value,
            cancellationToken);

    public static Task<PaginatedList<TTarget>> ToPaginatedListAsync<TOrderDefinition, TTarget>
        (this IQueryable<TTarget> queryable,
        PaginationParameter<TOrderDefinition, TTarget> parameter,
        CancellationToken cancellationToken = default)
        where TOrderDefinition : IOrderDefinition<TTarget>
        => CreateAsync(
            queryable,
            OrderSet<TOrderDefinition, TTarget>.Default,
            parameter.ContinuationToken,
            parameter.Limit!.Value,
            cancellationToken);

    private static async Task<PaginatedList<T>> CreateAsync<T>(
        IQueryable<T> source,
        IOrderSet<T> orderSet,
        IContinuationTokenSet? continuationTokenSet,
        int limit,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        const int oneMore = 1;

        var items = await source
            .ApplyOrder(orderSet)
            .ApplyCondition(orderSet, continuationTokenSet)
            .Take(limit + oneMore)
            .ToArrayAsync(cancellationToken);

        // Fetch one more item than requested to determine if there is a next page
        var hasNextPage = items.Length > limit;
        if (hasNextPage)
        {
            Array.Resize(ref items, limit);
        }

        var nextContinuationToken =
            orderSet.GetContinuationTokenFrom(items.LastOrDefault())
            ?? continuationTokenSet?.Raw;

        return new PaginatedList<T>(
            items,
            hasNextPage,
            @continue: nextContinuationToken,
            orderBy: orderSet.GetOrderString());
    }
}
