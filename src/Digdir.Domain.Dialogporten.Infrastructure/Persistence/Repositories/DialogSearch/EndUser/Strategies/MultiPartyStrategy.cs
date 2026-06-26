using System.Text.Json;
using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.Abstractions;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Sql;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.Selection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Strategies;

internal sealed class MultiPartyStrategy : IQueryStrategy<EndUserSearchContext>
{
    private readonly IOptionsSnapshot<ApplicationSettings> _applicationSettings;
    private readonly ILogger<MultiPartyStrategy> _logger;

    public MultiPartyStrategy(
        IOptionsSnapshot<ApplicationSettings> applicationSettings,
        ILogger<MultiPartyStrategy> logger)
    {
        ArgumentNullException.ThrowIfNull(applicationSettings);
        ArgumentNullException.ThrowIfNull(logger);

        _applicationSettings = applicationSettings;
        _logger = logger;
    }

    // Non-FTS party-driven fallback strategy for end-user search.
    // Drives candidate lookup by party, which is usually preferable when the effective party set is
    // small or the effective service set is too broad for stable service-driven fan-out.
    public string Name => nameof(MultiPartyStrategy);

    public int Score(EndUserSearchContext context)
    {
        if (context.Query.Search is not null)
        {
            return QueryStrategyScores.Ineligible;
        }

        // Preferred unless this is service-driven territory (large party set + small service set), where
        // MultiServiceStrategy is the better driver and this stays an eligible fallback.
        var limits = _applicationSettings.Value.Limits.EndUserSearch;
        return DialogEndUserSearchSqlHelpers.IsServiceDrivenTerritory(
            context.EffectivePartyCount, context.EffectiveServiceCount, limits)
            ? QueryStrategyScores.Eligible
            : QueryStrategyScores.Preferred;
    }

    public PostgresFormattableStringBuilder BuildSql(EndUserSearchContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Query.Search is not null)
        {
            throw new InvalidOperationException("Free-text search is not supported by this strategy.");
        }

        var query = context.Query;
        var authorizedResources = context.AuthorizedResources;
        var partiesAndServices = DialogEndUserSearchSqlHelpers.BuildPartiesAndServices(authorizedResources);
        DialogEndUserSearchSqlHelpers.LogPartiesAndServicesCount(_logger, partiesAndServices, Name);
        var permissionCandidateDialogs = BuildPermissionCandidateDialogs(query);
        var delegatedDialogIds = authorizedResources.DialogIds.ToArray();
        var hasDelegatedDialogIds = delegatedDialogIds.Length > 0;
        var delegatedCandidateDialogs = BuildDelegatedCandidateDialogs(query, delegatedDialogIds);
        var orderColumnSelection = DialogEndUserSearchSqlHelpers.BuildOrderColumnSelection(query.OrderBy!);

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
                    {permissionCandidateDialogs}
                )
                """)
            .AppendIf(hasDelegatedDialogIds,
                $"""
                ,delegated_dialogs AS (
                    {delegatedCandidateDialogs}
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

    private static PostgresFormattableStringBuilder BuildPermissionCandidateDialogs(GetDialogsQuery query)
    {
        var permissionCandidateFilters = DialogEndUserSearchSqlHelpers.BuildDialogFilters(query);
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
                    WHERE d."Party" = pp.party
                      AND d."ServiceResource" = ANY(pp.allowed_services)
                """)
            .Append(
                $"""
                      {permissionCandidateFilters}
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
        Guid[] dialogIds)
    {
        var permissionCandidateFilters = DialogEndUserSearchSqlHelpers.BuildDialogFilters(query);
        var orderColumnProjection = DialogEndUserSearchSqlHelpers.BuildOrderColumnProjection(query.OrderBy!, alias: "d");

        return new PostgresFormattableStringBuilder()
            .Append(
                $"""
                SELECT d."Id", {orderColumnProjection}
                FROM unnest({dialogIds}::uuid[]) AS dd("Id")
                JOIN "Dialog" d ON d."Id" = dd."Id"
                WHERE 1=1
                {permissionCandidateFilters}
                """);
    }
}
