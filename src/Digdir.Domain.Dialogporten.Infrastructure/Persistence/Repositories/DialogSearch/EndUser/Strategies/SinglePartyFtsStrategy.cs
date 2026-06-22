using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.Abstractions;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Models;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Sql;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.Selection;
using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Strategies;

internal sealed class SinglePartyFtsStrategy : IQueryStrategy<EndUserSearchContext>
{
    private readonly ILogger<SinglePartyFtsStrategy> _logger;

    public SinglePartyFtsStrategy(ILogger<SinglePartyFtsStrategy> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    // GIN-first FTS strategy for a single effective party. Unlike the dialog-first approach it replaces
    // (which capped an ordered Dialog window BEFORE applying FTS and therefore missed matches outside the
    // window), this finds matches without a recency cap, and never lets the planner drive from the
    // Dialog party scan (catastrophic for whale parties). It emits one of two shapes:
    //   * No ContentUpdatedAfter: term-driven. The GIN(Party, SearchVector) probe is forced via a
    //     `fts AS MATERIALIZED` CTE (GIN isn't recency-ordered, so it can't drive an ordered LIMIT) ->
    //     covering-index filter/sort/limit -> late-materialize the page. Fast for rare terms; a common
    //     term with no range is bounded by statement_timeout (see DialogSearchRepository) -> 422.
    //   * ContentUpdatedAfter present: range-driven. Dialog's recency-ordered range index drives the
    //     candidate scan (NOT materialized) and the nested-loop @@ recheck early-terminates at the page
    //     LIMIT. Cost bounded by the page, not the range width.
    public string Name => nameof(SinglePartyFtsStrategy);

    public int Score(EndUserSearchContext context) =>
        context.Query.Search is not null
        && context.EffectivePartyCount == 1
            ? QueryStrategyScores.HighlyPreferred
            : QueryStrategyScores.Ineligible;

    public PostgresFormattableStringBuilder BuildSql(EndUserSearchContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Query.Search is null)
        {
            throw new InvalidOperationException("Free-text search is required by this strategy.");
        }

        if (!DialogEndUserSearchSqlHelpers.TryGetSinglePartyAuthorization(
            context.AuthorizedResources,
            out var authorization))
        {
            throw new InvalidOperationException("Single-party authorization is required for this strategy.");
        }

        var query = context.Query;
        var delegatedDialogIds = context.AuthorizedResources.DialogIds.ToArray();
        DialogEndUserSearchSqlHelpers.LogPartiesAndServicesCount(_logger, authorization, Name);

        var ftsQuery = DialogFreeTextSearchSqlHelpers.CreateFreeTextSearchQuery(query);
        var ftsPredicate = DialogFreeTextSearchSqlHelpers.BuildFreeTextSearchPredicate(ftsQuery);
        var dialogFilters = DialogEndUserSearchSqlHelpers.BuildDialogFilters(query);
        var orderColumnProjection = DialogEndUserSearchSqlHelpers.BuildOrderColumnProjection(query.OrderBy!, alias: "d");
        var orderColumnSelection = DialogEndUserSearchSqlHelpers.BuildOrderColumnSelection(query.OrderBy!);

        // A ContentUpdatedAfter lower bound makes the Dialog range index selective, so we drive from it
        // ("range-driven"). Otherwise we must drive from the GIN ("term-driven").
        var builder = query.ContentUpdatedAfter is not null
            ? BuildRangeDrivenCandidates(query, authorization, ftsPredicate, dialogFilters, orderColumnProjection)
            : BuildTermDrivenCandidates(query, authorization, ftsPredicate, dialogFilters, orderColumnProjection);

        // Late-materialize the page (optionally UNION'ing instance-delegated dialogs, also @@-rechecked).
        return builder.AppendCandidatesDelegatedTail(
            query, dialogFilters, orderColumnProjection, orderColumnSelection, delegatedDialogIds, ftsPredicate);
    }

    // Term-driven: GIN(Party, SearchVector) gives the party's term matches (fast-scans from a rare
    // term's small posting list), then the covering index IX_Dialog_Id_Covering_V2 applies the
    // auth/filters/sort index-only (no heap) for the page. Produces CTE `candidates(Id, <order cols>)`.
    private static PostgresFormattableStringBuilder BuildTermDrivenCandidates(
        GetDialogsQuery query,
        SinglePartyAndServices authorization,
        PostgresFormattableStringBuilder ftsPredicate,
        PostgresFormattableStringBuilder dialogFilters,
        PostgresFormattableStringBuilder orderColumnProjection) =>
        new PostgresFormattableStringBuilder()
            .Append(
                $"""
                WITH fts AS MATERIALIZED (
                    SELECT ds."DialogId" AS "Id"
                    FROM search."DialogSearch" ds
                    WHERE ds."Party" = {authorization.Party}
                      AND {ftsPredicate}
                )
                ,candidates AS (
                    SELECT d."Id", {orderColumnProjection}
                    FROM fts
                    JOIN "Dialog" d ON d."Id" = fts."Id"
                    WHERE TRUE
                """)
            // No d."Party" predicate here: Party is not in IX_Dialog_Id_Covering_V2, and the GIN probe
            // already scoped to the party -- re-asserting it would force heap fetches.
            .AppendManyFilter([.. authorization.Services], nameof(GetDialogsQuery.ServiceResource))
            .Append($"{dialogFilters}")
            .ApplyPaginationOrder(query.OrderBy!, alias: "d")
            .ApplyPaginationLimit(query.Limit)
            .Append(
                """
                )
                """);

    // Range-driven: Dialog's IX_Dialog_Party_ServiceResource_ContentUpdatedAt_Id_NotDeleted produces the
    // range-bounded candidate set (index-only, recency-ordered), then we PK-probe DialogSearch to recheck
    // @@. Produces CTE `candidates(Id, <order cols>)`. `cand` is NOT materialized (consistent with
    // SingleServiceFtsStrategy): inlining lets the planner drive the recency-ordered index and, for a
    // single authorized service, early-terminate the nested-loop @@ recheck at the page LIMIT. With
    // multiple authorized services the ServiceResource = ANY(...) keeps the scan bounded by the range (the
    // planner sorts the in-range candidates before the page LIMIT).
    private static PostgresFormattableStringBuilder BuildRangeDrivenCandidates(
        GetDialogsQuery query,
        SinglePartyAndServices authorization,
        PostgresFormattableStringBuilder ftsPredicate,
        PostgresFormattableStringBuilder dialogFilters,
        PostgresFormattableStringBuilder orderColumnProjection)
    {
        var candOrderColumnProjection = DialogEndUserSearchSqlHelpers.BuildOrderColumnProjection(query.OrderBy!, alias: "cand");
        return new PostgresFormattableStringBuilder()
            .Append(
                $"""
                WITH cand AS (
                    SELECT d."Id", {orderColumnProjection}
                    FROM "Dialog" d
                    WHERE d."Party" = {authorization.Party}
                """)
            .AppendManyFilter([.. authorization.Services], nameof(GetDialogsQuery.ServiceResource))
            .Append($"{dialogFilters}")
            .Append(
                $"""
                )
                ,candidates AS (
                    SELECT cand."Id", {candOrderColumnProjection}
                    FROM cand
                    JOIN search."DialogSearch" ds ON ds."DialogId" = cand."Id"
                    WHERE {ftsPredicate}
                """)
            .ApplyPaginationOrder(query.OrderBy!, alias: "cand")
            .ApplyPaginationLimit(query.Limit)
            .Append(
                """
                )
                """);
    }
}
