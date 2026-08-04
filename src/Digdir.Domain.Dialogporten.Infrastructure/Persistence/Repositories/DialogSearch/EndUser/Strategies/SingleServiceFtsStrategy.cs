using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.Abstractions;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Sql;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.Selection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Strategies;

internal sealed class SingleServiceFtsStrategy : IQueryStrategy<EndUserSearchContext>
{
    private readonly IOptionsSnapshot<ApplicationSettings> _applicationSettings;
    private readonly ILogger<SingleServiceFtsStrategy> _logger;

    public SingleServiceFtsStrategy(
        IOptionsSnapshot<ApplicationSettings> applicationSettings,
        ILogger<SingleServiceFtsStrategy> logger)
    {
        ArgumentNullException.ThrowIfNull(applicationSettings);
        ArgumentNullException.ThrowIfNull(logger);

        _applicationSettings = applicationSettings;
        _logger = logger;
    }

    // FTS for a single effective service resource with a large authorized party set (#4128: e.g. a
    // serviceResource filter and no party filter -> authorization fans out to tens of thousands of
    // parties). Per-party GIN probing that many parties is the catastrophe; instead we drive the
    // recency-ordered service index (IX_Dialog_ServiceResource_ContentUpdatedAt_Id_NotDeleted) with a
    // SCALAR ServiceResource equality, recheck the FTS predicate per row by PK-probing DialogSearch, and
    // stop at the page limit -- so a term dense within the service touches only a page's worth of rows,
    // not the whole service. The party set is passed as a BOUND text[] parameter (not inlined as a
    // constant), which keeps the SQL text small for logging; the scalar service equality plus the param
    // array lead the planner to the service index with Party applied as a filter (validated on prod). A
    // date range bounds the scan window for sparse-within-service terms; statement_timeout bounds the rest.
    public string Name => nameof(SingleServiceFtsStrategy);

    public int Score(EndUserSearchContext context)
    {
        if (context.Query.Search is null)
        {
            return QueryStrategyScores.Ineligible;
        }

        var limits = _applicationSettings.Value.Limits.EndUserSearch;
        return DialogEndUserSearchSqlHelpers.IsSingleServiceEligible(
            context.EffectivePartyCount, context.EffectiveServiceCount, limits)
            ? QueryStrategyScores.Preferred
            : QueryStrategyScores.Ineligible;
    }

    public PostgresFormattableStringBuilder BuildSql(EndUserSearchContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Query.Search is null)
        {
            throw new InvalidOperationException("Free-text search is required by this strategy.");
        }

        if (!DialogEndUserSearchSqlHelpers.TryGetSingleServiceAuthorization(
                context.AuthorizedResources,
                out var authorization))
        {
            throw new InvalidOperationException("Single-service authorization is required for this strategy.");
        }

        var query = context.Query;
        var delegatedDialogIds = context.AuthorizedResources.DialogIds.ToArray();
        DialogEndUserSearchSqlHelpers.LogPartiesAndServicesCount(_logger, authorization, Name);

        var ftsQuery = DialogFreeTextSearchSqlHelpers.CreateFreeTextSearchQuery(query);
        var ftsPredicate = DialogFreeTextSearchSqlHelpers.BuildFreeTextSearchPredicate(ftsQuery);
        var dialogFilters = DialogEndUserSearchSqlHelpers.BuildDialogFilters(query);
        var orderColumnProjection = DialogEndUserSearchSqlHelpers.BuildOrderColumnProjection(query.OrderBy!, alias: "d");
        var orderColumnSelection = DialogEndUserSearchSqlHelpers.BuildOrderColumnSelection(query.OrderBy!);

        // `candidates` drives the recency-ordered service index and rechecks @@ per row (NOT materialized,
        // so the ORDER BY + LIMIT push into the ordered nested loop -> early termination). It selects only
        // Id + order columns; full Dialog rows are late-materialized for the page in the tail.
        var builder = new PostgresFormattableStringBuilder()
            .Append(
                $"""
                WITH candidates AS (
                    SELECT d."Id", {orderColumnProjection}
                    FROM "Dialog" d
                    JOIN search."DialogSearch" ds ON ds."DialogId" = d."Id"
                    WHERE d."ServiceResource" = {authorization.Service}
                      AND d."Party" = ANY({authorization.Parties})
                      {dialogFilters}
                      AND {ftsPredicate}
                """)
            .ApplyPaginationOrder(query.OrderBy!, alias: "d")
            .ApplyPaginationLimit(query.Limit)
            .Append(
                """
                )
                """);

        // Late-materialize the page (optionally UNION'ing instance-delegated dialogs, also @@-rechecked).
        return builder.AppendCandidatesDelegatedTail(
            query, dialogFilters, orderColumnProjection, orderColumnSelection, delegatedDialogIds, ftsPredicate);
    }
}
