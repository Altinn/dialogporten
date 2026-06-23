using System.Text.Json;
using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.Abstractions;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Sql;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.Selection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Strategies;

internal sealed class MultiPartyFtsStrategy : IQueryStrategy<EndUserSearchContext>
{
    private readonly IOptionsSnapshot<ApplicationSettings> _applicationSettings;
    private readonly ILogger<MultiPartyFtsStrategy> _logger;

    public MultiPartyFtsStrategy(
        IOptionsSnapshot<ApplicationSettings> applicationSettings,
        ILogger<MultiPartyFtsStrategy> logger)
    {
        ArgumentNullException.ThrowIfNull(applicationSettings);
        ArgumentNullException.ThrowIfNull(logger);

        _applicationSettings = applicationSettings;
        _logger = logger;
    }

    // Multi-party FTS for a manageable number of parties (the single-party strategy generalized, looped
    // per authorized party). No range → per-party GIN probe (`Party=p AND @@term`, fast-scans rare terms)
    // then join Dialog for auth/filters/order. Range → per-party Dialog range-index scan + @@ recheck,
    // early-terminating at the page limit. Service-driven (MultiServiceFtsStrategy) is preferred when
    // the party set is huge and the service set is small; this handles the rest of the multi-party space.
    public string Name => nameof(MultiPartyFtsStrategy);

    public int Score(EndUserSearchContext context)
    {
        if (context.Query.Search is null)
        {
            return QueryStrategyScores.Ineligible;
        }

        if (context.EffectivePartyCount <= 1)
        {
            // Exactly one party is handled by SinglePartyFtsStrategy (HighlyPreferred, so it still wins
            // here). This strategy stays the explicit *eligible owner* of the 0/low-party FTS fallback —
            // e.g. a delegated-only search (empty ResourcesByParties, non-empty DialogIds) has
            // effectivePartyCount == 0 and no other FTS strategy is eligible; its per-party CTEs then
            // produce no permission candidates and the delegated UNION branch carries the results.
            return QueryStrategyScores.Eligible;
        }

        // When the party set is large and the service set is small, the service-driven FTS strategies are
        // the right driver; defer to them. Otherwise this is the preferred multi-party strategy.
        var limits = _applicationSettings.Value.Limits.EndUserSearch;
        return DialogEndUserSearchSqlHelpers.IsServiceDrivenTerritory(
            context.EffectivePartyCount, context.EffectiveServiceCount, limits)
            ? QueryStrategyScores.Eligible
            : QueryStrategyScores.Preferred;
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
        var partiesAndServices = DialogEndUserSearchSqlHelpers.BuildPartiesAndServices(authorizedResources);
        DialogEndUserSearchSqlHelpers.LogPartiesAndServicesCount(_logger, partiesAndServices, Name);

        var ftsQuery = DialogFreeTextSearchSqlHelpers.CreateFreeTextSearchQuery(query);
        var ftsPredicate = DialogFreeTextSearchSqlHelpers.BuildFreeTextSearchPredicate(ftsQuery);
        var delegatedDialogIds = authorizedResources.DialogIds.ToArray();
        var hasDelegatedDialogIds = delegatedDialogIds.Length > 0;
        var orderColumnSelection = DialogEndUserSearchSqlHelpers.BuildOrderColumnSelection(query.OrderBy!);

        var permissionCandidateIds = query.ContentUpdatedAfter is not null
            ? BuildRangeDrivenCandidates(query, ftsPredicate)
            : BuildTermDrivenCandidates(query, ftsPredicate);

        return new PostgresFormattableStringBuilder()
            .Append("WITH ")
            .Append(
                $"""
                permission_groups AS (
                    SELECT x."Parties" AS parties
                         , x."Services" AS services
                    FROM jsonb_to_recordset({JsonSerializer.Serialize(partiesAndServices)}::jsonb) AS x("Parties" text[], "Services" text[])
                )
                ,party_permissions AS (
                    SELECT p.party
                         , pg.services AS allowed_services
                    FROM permission_groups pg
                    CROSS JOIN LATERAL unnest(pg.parties) AS p(party)
                )
                ,permission_candidate_ids AS (
                    {permissionCandidateIds}
                )
                """)
            .AppendIf(hasDelegatedDialogIds,
                $"""
                ,delegated_dialogs AS (
                    {BuildDelegatedCandidateDialogs(query, ftsPredicate, delegatedDialogIds)}
                )
                """)
            .Append(
                $"""
                ,candidate_dialogs AS (
                    SELECT "Id", {orderColumnSelection}
                    FROM permission_candidate_ids
                """)
            .AppendIf(hasDelegatedDialogIds,
                $"""
                    UNION
                    SELECT "Id", {orderColumnSelection}
                    FROM delegated_dialogs
                """)
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

    // No range: per-party GIN probe of DialogSearch (Party=p AND @@), then join Dialog for the
    // authorized services + filters. Rare terms return few ids per party → fast.
    private static PostgresFormattableStringBuilder BuildTermDrivenCandidates(
        GetDialogsQuery query,
        PostgresFormattableStringBuilder ftsPredicate)
    {
        var dialogFilters = DialogEndUserSearchSqlHelpers.BuildDialogFilters(query);
        var orderColumnProjection = DialogEndUserSearchSqlHelpers.BuildOrderColumnProjection(query.OrderBy!, alias: "d");

        return new PostgresFormattableStringBuilder()
            .Append(
                $"""
                SELECT d."Id", {orderColumnProjection}
                FROM party_permissions pp
                CROSS JOIN LATERAL (
                    SELECT ds."DialogId"
                    FROM search."DialogSearch" ds
                    WHERE ds."Party" = pp.party
                      AND {ftsPredicate}
                ) fc
                JOIN "Dialog" d ON d."Id" = fc."DialogId"
                WHERE d."ServiceResource" = ANY(pp.allowed_services)
                  {dialogFilters}
                """)
            .ApplyPaginationOrder(query.OrderBy!, alias: "d")
            .ApplyPaginationLimit(query.Limit);
    }

    // Range: per-party Dialog range-index scan + @@ recheck, early-terminating at the page limit.
    private static PostgresFormattableStringBuilder BuildRangeDrivenCandidates(
        GetDialogsQuery query,
        PostgresFormattableStringBuilder ftsPredicate)
    {
        var dialogFilters = DialogEndUserSearchSqlHelpers.BuildDialogFilters(query);
        var orderColumnProjection = DialogEndUserSearchSqlHelpers.BuildOrderColumnProjection(query.OrderBy!, alias: "d");
        var innerOrderColumnProjection = DialogEndUserSearchSqlHelpers.BuildOrderColumnProjection(query.OrderBy!, alias: "d_inner");

        return new PostgresFormattableStringBuilder()
            .Append(
                $"""
                SELECT d_inner."Id", {innerOrderColumnProjection}
                FROM party_permissions pp
                CROSS JOIN LATERAL (
                    SELECT d."Id", {orderColumnProjection}
                    FROM "Dialog" d
                    JOIN search."DialogSearch" ds ON ds."DialogId" = d."Id"
                    WHERE d."Party" = pp.party
                      AND d."ServiceResource" = ANY(pp.allowed_services)
                      {dialogFilters}
                      AND {ftsPredicate}
                """)
            .ApplyPaginationOrder(query.OrderBy!, alias: "d")
            .ApplyPaginationLimit(query.Limit)
            .Append(
                """
                ) d_inner

                """);
    }

    private static PostgresFormattableStringBuilder BuildDelegatedCandidateDialogs(
        GetDialogsQuery query,
        PostgresFormattableStringBuilder ftsPredicate,
        Guid[] dialogIds)
    {
        var dialogFilters = DialogEndUserSearchSqlHelpers.BuildDialogFilters(query);
        var orderColumnProjection = DialogEndUserSearchSqlHelpers.BuildOrderColumnProjection(query.OrderBy!, alias: "d");

        return new PostgresFormattableStringBuilder()
            .Append(
                $"""
                SELECT d."Id", {orderColumnProjection}
                FROM unnest({dialogIds}::uuid[]) AS dd("Id")
                JOIN "Dialog" d ON d."Id" = dd."Id"
                JOIN search."DialogSearch" ds ON ds."DialogId" = d."Id"
                WHERE 1=1
                  {dialogFilters}
                  AND {ftsPredicate}
                """);
    }
}
