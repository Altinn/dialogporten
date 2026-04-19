using System.Text.Json;
using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.Abstractions;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Sql;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.Selection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Strategies;

internal sealed class DialogFirstFtsStrategy : IQueryStrategy<EndUserSearchContext>
{
    private readonly IOptionsSnapshot<ApplicationSettings> _applicationSettings;
    private readonly ILogger<DialogFirstFtsStrategy> _logger;

    public DialogFirstFtsStrategy(
        IOptionsSnapshot<ApplicationSettings> applicationSettings,
        ILogger<DialogFirstFtsStrategy> logger)
    {
        ArgumentNullException.ThrowIfNull(applicationSettings);
        ArgumentNullException.ThrowIfNull(logger);

        _applicationSettings = applicationSettings;
        _logger = logger;
    }

    // Drives lookup from the ordered, authorized Dialog side before probing DialogSearch. Candidate
    // counts are capped per party and globally to make broad terms predictable under whale parties,
    // but the cap means older/later matching dialogs outside the sampled ordered window can be missed.
    // This is intentional: search remains bounded and respects requested ordering/windowing for the
    // sampled candidates instead of letting common terms scan an unbounded authorization set.
    public string Name => nameof(DialogFirstFtsStrategy);

    public int Score(EndUserSearchContext context)
    {
        if (context.Query.Search is null)
        {
            return QueryStrategyScores.Ineligible;
        }

        var effectivePartyCount = DialogEndUserSearchSqlHelpers.CountEffectiveParties(context.AuthorizedResources);

        if (effectivePartyCount >
            _applicationSettings.Value.Limits.EndUserSearch.MaxDialogFirstFreeTextSearchPartyCount)
        {
            // Possible to run, but will be slow
            return QueryStrategyScores.Eligible;
        }

        // This works best for multiple parties; for single party scenarios, the SinglePartyFtsStrategy is a better fit
        return effectivePartyCount > 1
            ? QueryStrategyScores.HighlyPreferred
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
        var dialogCandidateOrderColumnProjection = DialogEndUserSearchSqlHelpers.BuildOrderColumnProjection(query.OrderBy!, alias: "d_inner");
        var ftsMatchOrderColumnProjection = DialogEndUserSearchSqlHelpers.BuildOrderColumnProjection(query.OrderBy!, alias: "dc");
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
                ,dialog_candidates AS (
                    SELECT d_inner."Id", {dialogCandidateOrderColumnProjection}
                    FROM party_permissions pp
                    CROSS JOIN LATERAL (
                        SELECT d."Id", {orderColumnProjection}
                        FROM "Dialog" d
                        WHERE d."Party" = pp.party
                          AND d."ServiceResource" = ANY(pp.allowed_services)
                          {dialogFilters}
                """)
            .ApplyPaginationOrder(query.OrderBy!, alias: "d")
            .Append(
                $"""
                        LIMIT {candidateCapPerParty}
                    ) d_inner
                """)
            .ApplyPaginationOrder(query.OrderBy!, alias: "d_inner")
            .Append(
                $"""
                    LIMIT {candidateCap}
                )
                ,fts_matches AS (
                    SELECT dc."Id", {ftsMatchOrderColumnProjection}
                    FROM dialog_candidates dc
                    JOIN search."DialogSearch" ds ON ds."DialogId" = dc."Id"
                    WHERE {ftsPredicate}
                """)
            .ApplyPaginationOrder(query.OrderBy!, alias: "dc")
            .ApplyPaginationLimit(query.Limit)
            .Append(
                """
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
