using System.Text.Json;
using Digdir.Domain.Dialogporten.Application.Externals;
using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch;

internal sealed class ServiceDrivenDialogEndUserSearchStrategy(ILogger<ServiceDrivenDialogEndUserSearchStrategy> logger)
    : IDialogEndUserSearchStrategy
{
    private readonly ILogger<ServiceDrivenDialogEndUserSearchStrategy> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public string Name => "ServiceDriven";
    public EndUserSearchContext Context { get; private set; } = null!;

    public void SetContext(EndUserSearchContext context) =>
        Context = context ?? throw new ArgumentNullException(nameof(context));

    // Service-driven is always preferred when branching logic is enabled.
    public int Score(EndUserSearchContext context)
    {
        _ = context;
        return 100;
    }

    public PostgresFormattableStringBuilder BuildSql()
    {
        var (query, dialogSearchAuthorizationResult) = Context;
        var partiesAndServices = DialogEndUserSearchSqlHelpers.BuildPartiesAndServices(
            query,
            dialogSearchAuthorizationResult);
        DialogEndUserSearchSqlHelpers.LogPartiesAndServicesCount(_logger, partiesAndServices);
        var permissionCandidateDialogs = BuildPermissionCandidateDialogs(query);
        var searchJoin = DialogEndUserSearchSqlHelpers.BuildSearchJoin(query.Search is not null);
        var postPermissionFilters = DialogEndUserSearchSqlHelpers.BuildPostPermissionFilters(query);

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
                    SELECT unnest({dialogSearchAuthorizationResult.DialogIds.ToArray()}::uuid[]) AS "Id"
                )
                ,candidate_dialogs AS (
                    SELECT "Id" FROM permission_candidate_ids
                    UNION
                    SELECT "Id" FROM delegated_dialogs
                )
                SELECT d.*
                FROM candidate_dialogs cd
                JOIN "Dialog" d ON d."Id" = cd."Id"
                {searchJoin}
                {postPermissionFilters}

                """);
    }

    private static PostgresFormattableStringBuilder BuildPermissionCandidateDialogs(GetDialogsQuery query)
    {
        var permissionCandidateFilters = BuildPermissionCandidateFilters(query);

        return new PostgresFormattableStringBuilder()
            .Append(
                $"""
                SELECT d_inner."Id"
                FROM service_permissions sp
                CROSS JOIN LATERAL (
                    SELECT d."Id"
                    FROM "Dialog" d
                    WHERE d."ServiceResource" = sp.service
                      AND d."Party" = ANY(sp.allowed_parties)
                      {permissionCandidateFilters}
                """)
            .ApplyPaginationOrder(query.OrderBy!, alias: "d")
            .Append(
                $"""
                    LIMIT {query.Limit + 1}
                ) d_inner

                """);
    }

    private static PostgresFormattableStringBuilder BuildPermissionCandidateFilters(GetDialogsQuery query) =>
        new PostgresFormattableStringBuilder()
            .AppendManyFilter(query.Org, nameof(query.Org))
            .AppendManyFilter(query.Status, "StatusId", "int")
            .AppendManyFilter(query.ExtendedStatus, nameof(query.ExtendedStatus))
            .AppendIf(query.VisibleAfter is not null, $""" AND (d."VisibleFrom" IS NULL OR d."VisibleFrom" <= {query.VisibleAfter}::timestamptz) """)
            .AppendIf(query.ExpiresAfter is not null, $""" AND (d."ExpiresAt" IS NULL OR d."ExpiresAt" > {query.ExpiresAfter}::timestamptz) """)
            .AppendIf(query.Deleted is not null, $""" AND d."Deleted" = {query.Deleted}::boolean """)
            .AppendIf(query.ExternalReference is not null, $""" AND d."ExternalReference" = {query.ExternalReference}::text """)
            .AppendIf(query.CreatedAfter is not null, $""" AND {query.CreatedAfter}::timestamptz <= d."CreatedAt" """)
            .AppendIf(query.CreatedBefore is not null, $""" AND d."CreatedAt" <= {query.CreatedBefore}::timestamptz """)
            .AppendIf(query.UpdatedAfter is not null, $""" AND {query.UpdatedAfter}::timestamptz <= d."UpdatedAt" """)
            .AppendIf(query.UpdatedBefore is not null, $""" AND d."UpdatedAt" <= {query.UpdatedBefore}::timestamptz """)
            .AppendIf(query.ContentUpdatedAfter is not null, $""" AND {query.ContentUpdatedAfter}::timestamptz <= d."ContentUpdatedAt" """)
            .AppendIf(query.ContentUpdatedBefore is not null, $""" AND d."ContentUpdatedAt" <= {query.ContentUpdatedBefore}::timestamptz """)
            .AppendIf(query.DueAfter is not null, $""" AND {query.DueAfter}::timestamptz <= d."DueAt" """)
            .AppendIf(query.DueBefore is not null, $""" AND d."DueAt" <= {query.DueBefore}::timestamptz """)
            .AppendIf(query.Process is not null, $""" AND d."Process" = {query.Process}::text """)
            .AppendIf(query.ExcludeApiOnly is not null, $""" AND ({query.ExcludeApiOnly}::boolean = false OR {query.ExcludeApiOnly}::boolean = true AND d."IsApiOnly" = false) """)
            .AppendSystemLabelFilterCondition(query.SystemLabel);
}
