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
        => ToPaginatedListAsync(
            queryable,
            parameter.OrderBy.DefaultIfNull(),
            parameter.ContinuationToken,
            parameter.Limit!.Value,
            cancellationToken: cancellationToken);

    public static Task<PaginatedList<TTarget>> ToPaginatedListAsync<TOrderDefinition, TTarget>
        (this IQueryable<TTarget> queryable,
        PaginationParameter<TOrderDefinition, TTarget> parameter,
        CancellationToken cancellationToken = default)
        where TOrderDefinition : IOrderDefinition<TTarget>
        => ToPaginatedListAsync(
            queryable,
            OrderSet<TOrderDefinition, TTarget>.Default,
            parameter.ContinuationToken,
            parameter.Limit!.Value,
            cancellationToken: cancellationToken);

    /// <summary>
    /// Asynchronously converts a queryable source into a <see cref="PaginatedList{T}"/>.
    /// Applies ordering and continuation token conditions (when requested), fetches up to <paramref name="limit"/> + 1 items
    /// to determine whether a next page exists, and returns the requested page along with paging metadata.
    /// </summary>
    /// <typeparam name="T">The element type of the source sequence.</typeparam>
    /// <param name="source">The queryable source to paginate. Must not be <c>null</c>.</param>
    /// <param name="orderSet">Defines the ordering rules to apply to the source.</param>
    /// <param name="continuationTokenSet">Optional continuation token set used to apply filter conditions for paging.</param>
    /// <param name="limit">Maximum number of items to include in the returned page. Internally the method fetches <c>limit + 1</c> items to detect a next page.</param>
    /// <param name="applyOrder">If <c>true</c>, ordering from <paramref name="orderSet"/> will be applied before fetching items. Defaults to <c>true</c>. Only set to <c>false</c> when order is applied to the <see cref="IQueryable{T}"/> manually.</param>
    /// <param name="applyContinuationToken">If <c>true</c>, continuation token filtering from <paramref name="continuationTokenSet"/> will be applied before fetching items. Defaults to <c>true</c>. Only set to <c>false</c> when <paramref name="continuationTokenSet"/> is applied to the <see cref="IQueryable{T}"/> manually.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while awaiting the database operation.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that, when completed, yields a <see cref="PaginatedList{T}"/> containing:
    /// the page items, a flag indicating whether a next page exists, the continuation token for the next page (if any),
    /// and the order string applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is <c>null</c>.</exception>
    /// <remarks>
    /// - The method fetches one extra item beyond <paramref name="limit"/> to determine if a subsequent page exists.
    /// - If <paramref name="applyOrder"/> is <c>false</c>, the method may apply ordering temporarily to compute the continuation token for the last item.
    /// </remarks>
    public static async Task<PaginatedList<T>> ToPaginatedListAsync<T>(
        this IQueryable<T> source,
        IOrderSet<T> orderSet,
        IContinuationTokenSet? continuationTokenSet,
        int limit,
        bool applyOrder = true,
        bool applyContinuationToken = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        const int oneMore = 1;

        if (applyOrder)
        {
            source = source.ApplyOrder(orderSet);
        }

        if (applyContinuationToken)
        {
            source = source.ApplyCondition(orderSet, continuationTokenSet);
        }

        var items = await source
            .Take(limit + oneMore)
            .ToArrayAsync(cancellationToken);

        // Fetch one more item than requested to determine if there is a next page
        var hasNextPage = items.Length > limit;
        if (hasNextPage)
        {
            Array.Resize(ref items, limit);
        }

        var lastItem = applyOrder
            ? items.LastOrDefault()
            : items.AsQueryable()
                .ApplyOrder(orderSet)
                .LastOrDefault();

        var nextContinuationToken = orderSet
            .GetContinuationTokenFrom(lastItem)
            ?? continuationTokenSet?.Raw;

        return new PaginatedList<T>(
            items,
            hasNextPage,
            @continue: nextContinuationToken,
            orderBy: orderSet.GetOrderString());
    }
}
