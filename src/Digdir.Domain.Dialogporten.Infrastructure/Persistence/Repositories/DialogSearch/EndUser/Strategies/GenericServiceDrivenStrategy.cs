using System.Text.Json;
using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.Abstractions;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Sql;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.Selection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Strategies;

internal sealed class GenericServiceDrivenStrategy : IQueryStrategy<EndUserSearchContext>
{
    private readonly IOptionsSnapshot<ApplicationSettings> _applicationSettings;
    private readonly ILogger<GenericServiceDrivenStrategy> _logger;

    public GenericServiceDrivenStrategy(
        IOptionsSnapshot<ApplicationSettings> applicationSettings,
        ILogger<GenericServiceDrivenStrategy> logger)
    {
        ArgumentNullException.ThrowIfNull(applicationSettings);
        ArgumentNullException.ThrowIfNull(logger);

        _applicationSettings = applicationSettings;
        _logger = logger;
    }

    // Generic service-driven strategy for broad multi-party searches with a small effective service set.
    // Drives lookup by service resource to reduce party fan-out; each service group performs bounded
    // top-N probes before final merge.
    public string Name => nameof(GenericServiceDrivenStrategy);

    public int Score(EndUserSearchContext context)
    {
        if (context.Query.Search is not null)
        {
            return QueryStrategyScores.Ineligible;
        }

        var effectivePartyCount = DialogEndUserSearchSqlHelpers.CountEffectiveParties(
            context.Query,
            context.AuthorizedResources);
        var effectiveServiceCount = DialogEndUserSearchSqlHelpers.CountEffectiveServices(context.AuthorizedResources);

        return effectivePartyCount > _applicationSettings.Value.Limits.EndUserSearch.MinServiceDrivenStrategyPartyCount
               && effectiveServiceCount <= _applicationSettings.Value.Limits.EndUserSearch.MaxServiceResourceFilterValues
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
        var delegatedDialogIds = authorizedResources.DialogIds.ToArray();
        var hasDelegatedDialogIds = delegatedDialogIds.Length > 0;
        var delegatedCandidateDialogs = BuildDelegatedCandidateDialogs(query, delegatedDialogIds);
        var postPermissionOrderAndLimit = BuildPostPermissionOrderAndLimit(query);
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
                ,service_permissions AS (
                    SELECT s.service
                         , pg.parties AS allowed_parties
                    FROM permission_groups pg
                    CROSS JOIN LATERAL unnest(pg.services) AS s(service)
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
                $"""
                ) cd
                JOIN "Dialog" d ON d."Id" = cd."Id"
                {postPermissionOrderAndLimit}

                """);
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
                FROM service_permissions sp
                CROSS JOIN LATERAL (
                    SELECT d."Id", {orderColumnProjection}
                    FROM "Dialog" d
                    WHERE d."ServiceResource" = sp.service
                      AND d."Party" = ANY(sp.allowed_parties)
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

    private static PostgresFormattableStringBuilder BuildPostPermissionOrderAndLimit(GetDialogsQuery query) =>
        new PostgresFormattableStringBuilder()
            .ApplyPaginationOrder(query.OrderBy!, alias: "d")
            .ApplyPaginationLimit(query.Limit);
}
