using System.Text.Json;
using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.Abstractions;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Sql;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.Selection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Strategies;

internal sealed class GenericPartyDrivenStrategy : IQueryStrategy<EndUserSearchContext>
{
    private readonly IOptionsSnapshot<ApplicationSettings> _applicationSettings;
    private readonly ILogger<GenericPartyDrivenStrategy> _logger;

    public GenericPartyDrivenStrategy(
        IOptionsSnapshot<ApplicationSettings> applicationSettings,
        ILogger<GenericPartyDrivenStrategy> logger)
    {
        ArgumentNullException.ThrowIfNull(applicationSettings);
        ArgumentNullException.ThrowIfNull(logger);

        _applicationSettings = applicationSettings;
        _logger = logger;
    }

    // Generic party-driven fallback strategy for end-user search.
    // Drives candidate lookup by party, which is usually preferable when there is no service filter,
    // or when the effective party set is small enough that party-ordered indexes give stable top-N
    // pagination without service-driven fan-out.
    public string Name => "GenericPartyDriven";

    public int Score(EndUserSearchContext context)
    {
        if (context.Query.Search is not null)
        {
            return QueryStrategyScores.Ineligible;
        }

        var hasServiceResourceFilter = DialogEndUserSearchSqlHelpers.HasServiceResourceFilter(context.Query);
        var effectivePartyCount = DialogEndUserSearchSqlHelpers.CountEffectiveParties(
            context.Query,
            context.AuthorizedResources);

        return !hasServiceResourceFilter
               || effectivePartyCount <= _applicationSettings.Value.Limits.EndUserSearch.MinServiceDrivenStrategyPartyCount
            ? QueryStrategyScores.Preferred
            : QueryStrategyScores.Eligible;
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
        var partiesAndServices = DialogEndUserSearchSqlHelpers.BuildPartiesAndServices(
            query,
            authorizedResources);
        DialogEndUserSearchSqlHelpers.LogPartiesAndServicesCount(_logger, partiesAndServices, Name);
        var permissionCandidateDialogs = BuildPermissionCandidateDialogs(query);
        var delegatedCandidateDialogs = BuildDelegatedCandidateDialogs(query, authorizedResources.DialogIds.ToArray());
        var postPermissionOrderAndLimit = BuildPostPermissionOrderAndLimit(query);

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
                ,delegated_dialogs AS (
                    {delegatedCandidateDialogs}
                )
                ,candidate_dialogs AS (
                    SELECT "Id" FROM permission_candidate_ids
                    UNION
                    SELECT "Id" FROM delegated_dialogs
                )
                SELECT d.*
                FROM candidate_dialogs cd
                JOIN "Dialog" d ON d."Id" = cd."Id"
                {postPermissionOrderAndLimit}

                """);
    }

    private static PostgresFormattableStringBuilder BuildPermissionCandidateDialogs(GetDialogsQuery query)
    {
        var permissionCandidateFilters = DialogEndUserSearchSqlHelpers.BuildDialogFilters(query);

        return new PostgresFormattableStringBuilder()
            .Append(
                $"""
                SELECT d_inner."Id"
                FROM party_permissions pp
                CROSS JOIN LATERAL (
                    SELECT d."Id"
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

        return new PostgresFormattableStringBuilder()
            .Append(
                $"""
                SELECT d."Id"
                FROM unnest({dialogIds}::uuid[]) AS dd("Id")
                JOIN "Dialog" d ON d."Id" = dd."Id"
                WHERE 1=1
                {permissionCandidateFilters}
                """);
    }

    private static PostgresFormattableStringBuilder BuildPostPermissionOrderAndLimit(GetDialogsQuery query) =>
        new PostgresFormattableStringBuilder()
            .ApplyPaginationOrder(query.OrderBy!, alias: "d")
            .ApplyPaginationLimit(query.Limit);
}
