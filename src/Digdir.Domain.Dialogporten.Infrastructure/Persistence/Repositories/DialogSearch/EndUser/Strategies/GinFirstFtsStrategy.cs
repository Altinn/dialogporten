using System.Text.Json;
using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.Abstractions;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Sql;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.Selection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Strategies;

internal sealed class GinFirstFtsStrategy : IQueryStrategy<EndUserSearchContext>
{
    private readonly IOptionsSnapshot<ApplicationSettings> _applicationSettings;
    private readonly ILogger<GinFirstFtsStrategy> _logger;

    public GinFirstFtsStrategy(
        IOptionsSnapshot<ApplicationSettings> applicationSettings,
        ILogger<GinFirstFtsStrategy> logger)
    {
        ArgumentNullException.ThrowIfNull(applicationSettings);
        ArgumentNullException.ThrowIfNull(logger);

        _applicationSettings = applicationSettings;
        _logger = logger;
    }

    // FTS-specific strategy for large effective party sets. It drives candidate lookup through the
    // GIN index and caps matches per party and globally before applying dialog filters/final ordering.
    // This bounds broad-term searches across many parties, but FTS hits are unsorted at the cap point:
    // authorized/recent dialogs can be missed if they fall outside the capped GIN candidate set.
    public string Name => nameof(GinFirstFtsStrategy);

    public int Score(EndUserSearchContext context)
    {
        if (context.Query.Search is null)
        {
            return QueryStrategyScores.Ineligible;
        }

        var effectivePartyCount = DialogEndUserSearchSqlHelpers.CountEffectiveParties(context.AuthorizedResources);

        return effectivePartyCount >
               _applicationSettings.Value.Limits.EndUserSearch.MaxDialogFirstFreeTextSearchPartyCount
            ? QueryStrategyScores.Preferred
            : QueryStrategyScores.Eligible;
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
        var settings = _applicationSettings.Value.Limits.EndUserSearch;
        var partiesAndServices = DialogEndUserSearchSqlHelpers.BuildPartiesAndServices(authorizedResources);
        DialogEndUserSearchSqlHelpers.LogPartiesAndServicesCount(_logger, partiesAndServices, Name);

        var effectivePartyCount = DialogEndUserSearchSqlHelpers.CountEffectiveParties(authorizedResources);
        var candidateCap = settings.MaxFreeTextSearchCandidates;
        var candidateCapPerParty = DialogFreeTextSearchSqlHelpers.GetCandidateLimitPerParty(
            effectivePartyCount,
            candidateCap,
            settings.MinFreeTextSearchCandidatesPerParty);
        var ftsQuery = DialogFreeTextSearchSqlHelpers.CreateFreeTextSearchQuery(query);
        var ftsPredicate = DialogFreeTextSearchSqlHelpers.BuildFreeTextSearchPredicate(ftsQuery);
        var dialogFilters = DialogEndUserSearchSqlHelpers.BuildDialogFilters(query);
        var delegatedDialogIds = authorizedResources.DialogIds.ToArray();
        var hasDelegatedDialogIds = delegatedDialogIds.Length > 0;
        var orderColumnProjection = DialogEndUserSearchSqlHelpers.BuildOrderColumnProjection(query.OrderBy!, alias: "d");
        var orderColumnSelection = DialogEndUserSearchSqlHelpers.BuildOrderColumnSelection(query.OrderBy!);

        return new PostgresFormattableStringBuilder()
            .Append(
                $"""
                WITH permission_groups AS (
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
                ,authorized_parties AS (
                    SELECT DISTINCT party
                    FROM party_permissions
                )
                ,fts_candidates AS (
                    SELECT ds_inner."DialogId"
                         , ap.party
                    FROM authorized_parties ap
                    CROSS JOIN LATERAL (
                        SELECT ds."DialogId"
                        FROM search."DialogSearch" ds
                        WHERE ds."Party" = ap.party
                          AND {ftsPredicate}
                        LIMIT {candidateCapPerParty}
                    ) ds_inner
                    LIMIT {candidateCap}
                )
                ,fts_matches AS (
                    SELECT d."Id", {orderColumnProjection}
                    FROM fts_candidates fc
                    JOIN party_permissions pp ON pp.party = fc.party
                    JOIN "Dialog" d ON d."Id" = fc."DialogId"
                    WHERE d."ServiceResource" = ANY(pp.allowed_services)
                      {dialogFilters}
                """)
            .ApplyPaginationOrder(query.OrderBy!, alias: "d")
            .ApplyPaginationLimit(query.Limit)
            .Append(
                $"""
                )
                """)
            .AppendIf(hasDelegatedDialogIds,
                $"""
                ,delegated_dialogs AS (
                    SELECT d."Id", {orderColumnProjection}
                    FROM unnest({delegatedDialogIds}::uuid[]) AS dd("Id")
                    JOIN "Dialog" d ON d."Id" = dd."Id"
                    JOIN search."DialogSearch" ds ON ds."DialogId" = d."Id"
                    WHERE {ftsPredicate}
                      {dialogFilters}
                )
                """)
            .Append(
                $"""
                ,candidate_dialogs AS (
                    SELECT "Id", {orderColumnSelection}
                    FROM fts_matches
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
}
