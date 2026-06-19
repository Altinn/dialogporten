using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.Abstractions;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Sql;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.Selection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Strategies;

internal sealed class MultiServiceFtsStrategy : IQueryStrategy<EndUserSearchContext>
{
    private readonly IOptionsSnapshot<ApplicationSettings> _applicationSettings;
    private readonly ILogger<MultiServiceFtsStrategy> _logger;

    public MultiServiceFtsStrategy(
        IOptionsSnapshot<ApplicationSettings> applicationSettings,
        ILogger<MultiServiceFtsStrategy> logger)
    {
        ArgumentNullException.ThrowIfNull(applicationSettings);
        ArgumentNullException.ThrowIfNull(logger);

        _applicationSettings = applicationSettings;
        _logger = logger;
    }

    // FTS for broad multi-party searches with a small effective service set (e.g. a serviceResource
    // filter and no party → authorization fans out to tens of thousands of parties). Per-party GIN
    // probing that many parties is the catastrophe; instead we drive by service resource. The candidate
    // lateral scans each service's dialogs **recency-first** (the
    // IX_Dialog_ServiceResource_ContentUpdatedAt_Party_Id_NotDeleted index), rechecks the FTS predicate
    // per row by PK-probing DialogSearch, and stops at the page limit — so for terms dense within the
    // service it touches only a page's worth, not the whole service. A date range bounds the scan window
    // for sparse-within-service terms; statement_timeout bounds the rest.
    public string Name => nameof(MultiServiceFtsStrategy);

    public int Score(EndUserSearchContext context)
    {
        if (context.Query.Search is null)
        {
            return QueryStrategyScores.Ineligible;
        }

        // effectiveServiceCount == 1 is handled by SingleServiceFtsStrategy (scalar service + bound
        // param + service index); this strategy is for genuinely multiple effective services.
        var limits = _applicationSettings.Value.Limits.EndUserSearch;
        return DialogEndUserSearchSqlHelpers.IsMultiServiceEligible(
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

        var query = context.Query;
        var authorizedResources = context.AuthorizedResources;
        var partiesByService = DialogEndUserSearchSqlHelpers.BuildPartiesByService(authorizedResources);
        if (partiesByService.Count == 0)
        {
            throw new InvalidOperationException(
                "Multi-service FTS strategy requires at least one authorized service with parties.");
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            DialogEndUserSearchSqlHelpers.LogPartiesAndServicesCount(_logger, partiesByService, Name);
        }

        var ftsQuery = DialogFreeTextSearchSqlHelpers.CreateFreeTextSearchQuery(query);
        var ftsPredicate = DialogFreeTextSearchSqlHelpers.BuildFreeTextSearchPredicate(ftsQuery);
        var dialogFilters = DialogEndUserSearchSqlHelpers.BuildDialogFilters(query);
        var orderColumnProjection = DialogEndUserSearchSqlHelpers.BuildOrderColumnProjection(query.OrderBy!, alias: "d");
        var delegatedDialogIds = authorizedResources.DialogIds.ToArray();
        var hasDelegatedDialogIds = delegatedDialogIds.Length > 0;

        // One scalar-service block per distinct service, UNION'd. Each block drives the recency-ordered
        // service index (scalar ServiceResource = bound param + bound text[] party filter) and
        // early-terminates at the page limit. The planner picks the service index per block because the
        // bound param arrays are visible under a custom plan (forced in DialogSearchRepository) -- it
        // estimates the party fan-out and applies the party set as a filter rather than probing the
        // party-first index once per party. The branches are ordered, so PostgreSQL merge-appends them and
        // the outer LIMIT early-terminates across services.
        var builder = new PostgresFormattableStringBuilder()
            .Append("WITH candidate_dialogs AS (");

        var isFirstBranch = true;
        foreach (var (service, parties) in partiesByService)
        {
            builder.AppendIf(!isFirstBranch,
                """

                UNION ALL
                """);
            builder
                .Append(
                    $"""
                    (
                        SELECT d."Id", {orderColumnProjection}
                        FROM "Dialog" d
                        JOIN search."DialogSearch" ds ON ds."DialogId" = d."Id"
                        WHERE d."ServiceResource" = {service}
                          AND d."Party" = ANY({parties})
                          {dialogFilters}
                          AND {ftsPredicate}
                    """)
                .ApplyPaginationOrder(query.OrderBy!, alias: "d")
                .ApplyPaginationLimit(query.Limit)
                .Append(")");
            isFirstBranch = false;
        }

        // Instance-delegated dialogs (also term-matched + filtered). UNION (not UNION ALL) so a delegated
        // dialog that also matches one of the service branches is not returned twice.
        builder.AppendIf(hasDelegatedDialogIds,
            $"""

            UNION
            (
                SELECT d."Id", {orderColumnProjection}
                FROM unnest({delegatedDialogIds}::uuid[]) AS dd("Id")
                JOIN "Dialog" d ON d."Id" = dd."Id"
                JOIN search."DialogSearch" ds ON ds."DialogId" = d."Id"
                WHERE {ftsPredicate}
                  {dialogFilters}
            )
            """);

        return builder
            .Append(
                """
                )
                SELECT d.*
                FROM (
                    SELECT cd."Id"
                    FROM candidate_dialogs cd
                """)
            .ApplyPaginationOrder(query.OrderBy!, alias: "cd")
            .ApplyPaginationLimit(query.Limit)
            .Append(
                """
                ) cd
                JOIN "Dialog" d ON d."Id" = cd."Id"
                """)
            .ApplyPaginationOrder(query.OrderBy!, alias: "d")
            .ApplyPaginationLimit(query.Limit);
    }
}
