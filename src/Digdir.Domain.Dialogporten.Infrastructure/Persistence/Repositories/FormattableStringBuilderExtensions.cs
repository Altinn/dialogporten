using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Continuation;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Order;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.OrderOption;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

internal static class PostgresFormattableStringBuilderExtensions
{
    public static Task<PaginatedList<TTarget>> ToPaginatedListAsync<TOrderDefinition, TTarget>
    (this DbContext db,
        PostgresFormattableStringBuilder query,
        SortablePaginationParameter<TOrderDefinition, TTarget> parameter,
        CancellationToken cancellationToken = default)
        where TOrderDefinition : IOrderDefinition<TTarget>
        => CreateAsync(
            db,
            query,
            parameter.OrderBy.DefaultIfNull(),
            parameter.ContinuationToken,
            parameter.Limit!.Value,
            cancellationToken);

    public static Task<PaginatedList<TTarget>> ToPaginatedListAsync<TOrderDefinition, TTarget>
    (this DbContext db,
        PostgresFormattableStringBuilder query,
        PaginationParameter<TOrderDefinition, TTarget> parameter,
        CancellationToken cancellationToken = default)
        where TOrderDefinition : IOrderDefinition<TTarget>
        => CreateAsync(
            db,
            query,
            OrderSet<TOrderDefinition, TTarget>.Default,
            parameter.ContinuationToken,
            parameter.Limit!.Value,
            cancellationToken);

    private static async Task<PaginatedList<T>> CreateAsync<T>(
        this DbContext db,
        PostgresFormattableStringBuilder query,
        IOrderSet<T> orderSet,
        IContinuationTokenSet? continuationTokenSet,
        int limit,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        const int oneMore = 1;

        query.ApplyPaginationCondition(orderSet, continuationTokenSet)
            .ApplyPaginationOrder(orderSet)
            .ApplyPaginationLimit(limit + oneMore);

        var items = await db.Database
            .SqlQuery<T>(query.ToFormattableString())
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

    [SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance")]
    internal static PostgresFormattableStringBuilder ApplyPaginationOrder<T>(this PostgresFormattableStringBuilder builder, IOrderSet<T> orderSet)
    {
        using var enumerator = orderSet.Orders.GetEnumerator();

        if (!enumerator.MoveNext()) return builder;

        builder.Append((string)$""" ORDER BY "{GetKey(enumerator.Current)}" {DirectionToSql(enumerator.Current.Direction)}""");

        while (enumerator.MoveNext())
        {
            builder.Append((string)$""", "{GetKey(enumerator.Current)}" {DirectionToSql(enumerator.Current.Direction)}""");
        }

        return builder.Append(" ");

        static string DirectionToSql(OrderDirection direction)
        {
            return direction switch
            {
                OrderDirection.Asc => "ASC",
                OrderDirection.Desc => "DESC",
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
            };
        }
    }

    [SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance")]
    internal static PostgresFormattableStringBuilder ApplyPaginationCondition<T>(
        this PostgresFormattableStringBuilder builder,
        IOrderSet<T> orderSet,
        IContinuationTokenSet? continuationTokenSet)
    {
        if (continuationTokenSet is null)
        {
            return builder;
        }

        var orderParts = orderSet.Orders
            .Join(continuationTokenSet.Tokens, x => x.Key, x => x.Key,
                (order, token) => new OrderPart(order.Direction, order.GetSelector().Body.Type, GetKey(order), token.Value),
                StringComparer.InvariantCultureIgnoreCase)
            .ToList();
        // x => x.a < an OR (x.a = an AND x.b < bn) OR (x.a = an AND x.b = bn AND x.c < cn) OR ...

        using var ltGtEnumerator = orderParts.Select((x, i) => (OrderPart: x, Index: i)).GetEnumerator();

        if (!ltGtEnumerator.MoveNext())
        {
            throw new InvalidOperationException("Missing token keys.");
        }

        CreateLessThanGreaterThanPart(builder, ltGtEnumerator.Current.OrderPart);

        while (ltGtEnumerator.MoveNext())
        {
            var (ltGtPart, ltGtIndex) = ltGtEnumerator.Current;
            builder.Append(" OR (");

            CreateLessThanGreaterThanPart(builder, ltGtPart);
            foreach (var eqPart in orderParts[..ltGtIndex])
            {
                builder.Append(" AND");
                CreateEqualsPart(builder, eqPart);
            }

            builder.Append(")");
        }

        return builder;
    }

    internal static PostgresFormattableStringBuilder ApplyPaginationLimit(this PostgresFormattableStringBuilder builder, int limit) =>
        builder.Append((string)$" LIMIT {limit} ");

    private static PostgresFormattableStringBuilder CreateLessThanGreaterThanPart(this PostgresFormattableStringBuilder builder, OrderPart orderPart) =>
        // Null values are excluded in greater/less than comparison in
        // postgres since both 'null==null' and 'null!=null' returns
        // false. Threfore we need to take null values into account
        // when creating the pagination condition. Null values are
        // default last in ascending order and first in descending
        // order in postgres. At the time of this writing it is not
        // posible to change where the nulls apair in the query result
        // through the npgsql ef core provider as one can through a
        // direct sql query (e.g. desc nulls last). The issue is
        // tracked here https://github.com/npgsql/efcore.pg/issues/627.
        // Both non default cases (asc nulls first / desc nulls last)
        // must be taken onto account should the issue be resolved and
        // the functionallity used in the future.
        orderPart.Direction switch
        {
            OrderDirection.Asc when orderPart.Type.IsNullableType() && orderPart.Value is not null
                => builder.Append($""" "{orderPart.Key}" IS NULL OR "{orderPart.Key}" > '{orderPart.Value}'"""),
            OrderDirection.Desc when orderPart.Type.IsNullableType() && orderPart.Value is null
                => builder.Append($""" "{orderPart.Key}" IS NOT NULL"""),

            OrderDirection.Asc => builder.Append($""" "{orderPart.Key}" > '{orderPart.Value}'"""),
            OrderDirection.Desc => builder.Append($""" "{orderPart.Key}" < '{orderPart.Value}'"""),
            _ => throw new InvalidOperationException()
        };

    private static PostgresFormattableStringBuilder CreateEqualsPart(this PostgresFormattableStringBuilder builder, OrderPart x) =>
        x.Value is not null
            ? builder.Append($""" "{x.Key}" = '{x.Value}'""")
            : builder.Append($""" "{x.Key}" IS NULL""");

    private sealed record OrderPart(OrderDirection Direction, Type Type, string Key, object? Value);

    private static string GetKey<T>(Order<T> orderSet) => ((MemberExpression)orderSet.GetSelector().Body).Member.Name;
}
