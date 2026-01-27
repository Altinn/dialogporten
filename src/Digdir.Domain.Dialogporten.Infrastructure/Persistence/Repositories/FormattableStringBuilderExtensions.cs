using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Continuation;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Order;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

internal static class PostgresFormattableStringBuilderExtensions
{
    extension(PostgresFormattableStringBuilder builder)
    {
        [SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance")]
        internal PostgresFormattableStringBuilder ApplyPaginationOrder<T>(IOrderSet<T> orderSet, string alias)
        {
            using var enumerator = orderSet.Orders.GetEnumerator();

            if (!enumerator.MoveNext()) return builder;

            builder.Append((string)$" ORDER BY {GetKey(enumerator.Current, alias)} {DirectionToSql(enumerator.Current.Direction)}");

            while (enumerator.MoveNext())
            {
                builder.Append((string)$", {GetKey(enumerator.Current, alias)} {DirectionToSql(enumerator.Current.Direction)}");
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

        internal PostgresFormattableStringBuilder AppendSystemLabelFilterCondition(IEnumerable<SystemLabel.Values>? endUserLabels)
        {
            if (endUserLabels is null)
            {
                return builder;
            }

            foreach (var endUserLabel in endUserLabels)
            {
                builder.Append(
                    $"""
                     AND EXISTS (
                         SELECT 1
                         FROM "DialogEndUserContext" dec 
                         JOIN "DialogEndUserContextSystemLabel" sl ON dec."Id" = sl."DialogEndUserContextId"
                         WHERE dec."DialogId" = d."Id"
                            AND sl."SystemLabelId" = {endUserLabel} 
                         )
                     """);
            }

            return builder;
        }

        internal PostgresFormattableStringBuilder AppendServiceOwnerLabelFilterCondition(IEnumerable<string>? serviceOwnerLabels)
        {
            if (!TryGetProcessedServiceOwnerLabels(serviceOwnerLabels, out var processedLabels))
            {
                return builder;
            }

            foreach (var (pattern, isPrefix) in processedLabels)
            {
                if (isPrefix)
                {
                    builder.Append($"""
                                    AND EXISTS (
                                        SELECT 1
                                        FROM "DialogServiceOwnerLabel" sl 
                                        WHERE sl."DialogServiceOwnerContextId" = d."Id"
                                           AND sl."Value" LIKE {pattern} 
                                    )
                                    """);
                }
                else
                {
                    builder.Append($"""
                                    AND EXISTS (
                                        SELECT 1
                                        FROM "DialogServiceOwnerLabel" sl 
                                        WHERE sl."DialogServiceOwnerContextId" = d."Id"
                                           AND sl."Value" = {pattern} 
                                    )
                                    """);
                }
            }

            return builder;
        }

        internal PostgresFormattableStringBuilder AppendManyFilter<T>(List<T>? values,
            string field,
            string pgType = "text")
        {
            if (values is null || values.Count == 0)
            {
                return builder;
            }

            return values.Count == 1
                // Optimize for single value case by making it easier for the query planner to use index scans
                ? builder.Append(" AND d.\"" + field + "\"").Append($""" = {values.First()} """)
                : builder.Append(" AND d.\"" + field + "\"").Append($""" = ANY({values}::""").Append(pgType + "[]) ");
        }

        [SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance")]
        internal PostgresFormattableStringBuilder ApplyPaginationCondition<T>(IOrderSet<T> orderSet,
            IContinuationTokenSet? continuationTokenSet,
            string alias)
        {
            if (continuationTokenSet is null)
            {
                return builder;
            }

            var orderParts = orderSet.Orders
                .Join(continuationTokenSet.Tokens, x => x.Key, x => x.Key,
                    (order, token) => new OrderPart(order.Direction, order.GetSelector().Body.Type, GetKey<T>(order, alias), token.Value),
                    StringComparer.InvariantCultureIgnoreCase)
                .ToList();
            // x => x.a < an OR (x.a = an AND x.b < bn) OR (x.a = an AND x.b = bn AND x.c < cn) OR ...

            using var ltGtEnumerator = orderParts.Select((x, i) => (OrderPart: x, Index: i)).GetEnumerator();

            if (!ltGtEnumerator.MoveNext())
            {
                throw new InvalidOperationException("Missing token keys.");
            }

            builder.Append(" AND (");

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

            builder.Append(")");

            return builder;
        }

        internal PostgresFormattableStringBuilder ApplyPaginationLimit(int limit) =>
            builder.Append((string)$" LIMIT {limit + 1} ");
    }

    private static bool TryGetProcessedServiceOwnerLabels(
        IEnumerable<string>? inputLabels,
        out List<(string Pattern, bool IsPrefix)> results)
    {
        results = [];
        if (inputLabels == null)
        {
            return false;
        }

        foreach (var rawLabel in inputLabels.Where(s => !string.IsNullOrWhiteSpace(s)))
        {
            var label = rawLabel.Trim().ToLowerInvariant();
            var asteriskIndex = label.IndexOf('*');
            if (asteriskIndex == -1)
            {
                results.Add((label, false));
            }
            else if (asteriskIndex == label.Length - 1)
            {
                results.Add((string.Concat(label.AsSpan(0, label.Length - 1), "%"), true));
            }
            else
            {
                throw new ArgumentException($"Invalid label format: '{label}'. Wildcard '*' is only allowed at the very end of the string.");
            }
        }

        return results.Count > 0;
    }

    extension(PostgresFormattableStringBuilder builder)
    {
        private PostgresFormattableStringBuilder CreateLessThanGreaterThanPart(OrderPart orderPart) =>
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
                    => builder.Append((string)$" {orderPart.Key} IS NULL OR {orderPart.Key} > ").Append($"{orderPart.Value}"),
                OrderDirection.Desc when orderPart.Type.IsNullableType() && orderPart.Value is null
                    => builder.Append((string)$" {orderPart.Key} IS NOT NULL"),

                OrderDirection.Asc => builder.Append((string)$""" {orderPart.Key} > """).Append($"{orderPart.Value}"),
                OrderDirection.Desc => builder.Append((string)$""" {orderPart.Key} < """).Append($"{orderPart.Value}"),
                _ => throw new InvalidOperationException()
            };

        private PostgresFormattableStringBuilder CreateEqualsPart(OrderPart x) =>
            x.Value is not null
                ? builder.Append((string)$" {x.Key} = ").Append($"{x.Value}")
                : builder.Append((string)$" {x.Key} IS NULL");
    }

    private sealed record OrderPart(OrderDirection Direction, Type Type, string Key, object? Value);

    private static string GetKey<T>(Order<T> orderSet, string alias) =>
        $"\"{alias}\".\"{((MemberExpression)orderSet.GetSelector().Body).Member.Name}\"";
}
