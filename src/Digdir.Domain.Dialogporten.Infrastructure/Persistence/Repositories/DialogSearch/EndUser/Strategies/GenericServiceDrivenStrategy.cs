using System.Text.Json;
using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;
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

    public string Name => "GenericServiceDriven";

    public int Score(EndUserSearchContext context)
    {
        var effectivePartyCount = DialogEndUserSearchSqlHelpers.CountEffectiveParties(
            context.Query,
            context.AuthorizedResources);

        return context.Query.Search is null
               && DialogEndUserSearchSqlHelpers.HasServiceResourceFilter(context.Query)
               && effectivePartyCount > _applicationSettings.Value.Limits.EndUserSearch.MinServiceDrivenStrategyPartyCount
            ? QueryStrategyScores.Preferred
            : QueryStrategyScores.Eligible;
    }

    public PostgresFormattableStringBuilder BuildSql(EndUserSearchContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
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
            .AppendIf(query.Search is not null,
                $"""
                searchString AS (
                   SELECT websearch_to_tsquery(coalesce(isomap."TsConfigName", 'simple')::regconfig, {query.Search}::text) searchVector
                    ,string_to_array({query.Search}::text, ' ') AS searchTerms
                    FROM (VALUES (coalesce({query.SearchLanguageCode}::text, 'simple'))) AS v(isoCode)
                    LEFT JOIN search."Iso639TsVectorMap" isomap ON v.isoCode = isomap."IsoCode"
                    LIMIT 1
                ),
                """)
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
        var searchJoin = DialogEndUserSearchSqlHelpers.BuildSearchJoin(query.Search is not null);

        return new PostgresFormattableStringBuilder()
            .Append(
                $"""
                SELECT d_inner."Id"
                FROM service_permissions sp
                CROSS JOIN LATERAL (
                    SELECT d."Id"
                    FROM "Dialog" d
                    {searchJoin}
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
        var searchJoin = DialogEndUserSearchSqlHelpers.BuildSearchJoin(query.Search is not null);

        return new PostgresFormattableStringBuilder()
            .Append(
                $"""
                SELECT d."Id"
                FROM unnest({dialogIds}::uuid[]) AS dd("Id")
                JOIN "Dialog" d ON d."Id" = dd."Id"
                {searchJoin}
                WHERE 1=1
                {permissionCandidateFilters}
                """);
    }

    private static PostgresFormattableStringBuilder BuildPostPermissionOrderAndLimit(GetDialogsQuery query) =>
        new PostgresFormattableStringBuilder()
            .ApplyPaginationOrder(query.OrderBy!, alias: "d")
            .ApplyPaginationLimit(query.Limit);
}
