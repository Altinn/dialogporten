using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.Abstractions;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Models;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Sql;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.Selection;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Strategies;

internal sealed class SinglePartyNoFtsStrategy : IQueryStrategy<EndUserSearchContext>
{
    // Direct single-party lookup for the common case with no FTS.
    // Uses an ID-only limited candidate subquery before fetching full rows, so the candidate scan
    // can use covering indexes and avoid heap lookups until the final page-sized result set.
    public string Name => nameof(SinglePartyNoFtsStrategy);

    public int Score(EndUserSearchContext context) =>
        context.Query.Search is null
        && DialogEndUserSearchSqlHelpers.TryGetSinglePartyAuthorization(
            context.Query,
            context.AuthorizedResources,
            out _)
            ? QueryStrategyScores.HighlyPreferred
            : QueryStrategyScores.Ineligible;

    public PostgresFormattableStringBuilder BuildSql(EndUserSearchContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Query.Search is not null)
        {
            throw new InvalidOperationException("Free-text search is not supported by this strategy.");
        }

        if (!DialogEndUserSearchSqlHelpers.TryGetSinglePartyAuthorization(
                context.Query,
                context.AuthorizedResources,
                out var authorization))
        {
            throw new InvalidOperationException("Single-party authorization is required for this strategy.");
        }

        var query = context.Query;
        var delegatedDialogIds = context.AuthorizedResources.DialogIds.ToArray();

        return delegatedDialogIds.Length == 0
            ? BuildWithoutDelegatedDialogIds(query, authorization)
            : BuildWithDelegatedDialogIds(query, authorization, delegatedDialogIds);
    }

    private static PostgresFormattableStringBuilder BuildWithoutDelegatedDialogIds(
        GetDialogsQuery query,
        SinglePartyAndServices authorization) =>
        new PostgresFormattableStringBuilder()
            .Append(
                $"""
                SELECT d.*
                FROM "Dialog" d
                JOIN (
                    SELECT d."Id"
                    FROM "Dialog" d
                    WHERE d."Party" = {authorization.Party}
                """)
            .AppendManyFilter([.. authorization.Services], nameof(GetDialogsQuery.ServiceResource))
            .Append($"{DialogEndUserSearchSqlHelpers.BuildDialogFilters(query)}")
            .ApplyPaginationOrder(query.OrderBy!, alias: "d")
            .ApplyPaginationLimit(query.Limit)
            .Append(
                """
                ) AS sub ON d."Id" = sub."Id"
                """)
            .ApplyPaginationOrder(query.OrderBy!, alias: "d")
            .ApplyPaginationLimit(query.Limit);

    private static PostgresFormattableStringBuilder BuildWithDelegatedDialogIds(
        GetDialogsQuery query,
        SinglePartyAndServices authorization,
        Guid[] delegatedDialogIds)
    {
        var dialogFilters = DialogEndUserSearchSqlHelpers.BuildDialogFilters(query);
        var orderColumnSelection = DialogEndUserSearchSqlHelpers.BuildOrderColumnSelection(query.OrderBy!);
        var orderColumnProjection = DialogEndUserSearchSqlHelpers.BuildOrderColumnProjection(query.OrderBy!, alias: "d");

        return new PostgresFormattableStringBuilder()
            .Append(
                $"""
                WITH permission_candidate_ids AS (
                    SELECT d."Id", {orderColumnProjection}
                    FROM "Dialog" d
                    WHERE d."Party" = {authorization.Party}
                """)
            .AppendManyFilter([.. authorization.Services], nameof(GetDialogsQuery.ServiceResource))
            .Append($"{dialogFilters}")
            .ApplyPaginationOrder(query.OrderBy!, alias: "d")
            .ApplyPaginationLimit(query.Limit)
            .Append(
                $"""
                )
                ,delegated_dialogs AS (
                    SELECT d."Id", {orderColumnProjection}
                    FROM unnest({delegatedDialogIds}::uuid[]) AS dd("Id")
                    JOIN "Dialog" d ON d."Id" = dd."Id"
                    WHERE 1=1
                      {dialogFilters}
                )
                ,candidate_dialogs AS (
                    SELECT "Id", {orderColumnSelection}
                    FROM permission_candidate_ids
                    UNION
                    SELECT "Id", {orderColumnSelection}
                    FROM delegated_dialogs
                )
                SELECT d.*
                FROM (
                    SELECT cd."Id"
                    FROM candidate_dialogs cd
                """)
            .ApplyPaginationOrder(query.OrderBy!, alias: "cd")
            .ApplyPaginationLimit(query.Limit)
            .Append(
                $"""
                ) cd
                JOIN "Dialog" d ON d."Id" = cd."Id"
                """)
            .ApplyPaginationOrder(query.OrderBy!, alias: "d")
            .ApplyPaginationLimit(query.Limit);
    }
}
