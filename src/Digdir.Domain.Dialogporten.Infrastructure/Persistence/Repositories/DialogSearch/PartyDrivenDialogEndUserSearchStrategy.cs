using System.Text.Json;
using Digdir.Domain.Dialogporten.Application.Externals;
using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch;

internal sealed class PartyDrivenDialogEndUserSearchStrategy(ILogger<PartyDrivenDialogEndUserSearchStrategy> logger)
    : IDialogEndUserSearchStrategy
{
    internal const string StrategyName = "PartyDriven";
    private readonly ILogger<PartyDrivenDialogEndUserSearchStrategy> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public string Name => StrategyName;
    public EndUserSearchContext Context { get; private set; } = null!;

    public void SetContext(EndUserSearchContext context) =>
        Context = context ?? throw new ArgumentNullException(nameof(context));

    // Party-driven is kept as fallback only when branching logic is disabled.
    public int Score(EndUserSearchContext context)
    {
        _ = context;
        return 0;
    }

    public PostgresFormattableStringBuilder BuildSql()
    {
        var (query, dialogSearchAuthorizationResult) = Context;
        var partiesAndServices = DialogEndUserSearchSqlHelpers.BuildPartiesAndServices(
            query,
            dialogSearchAuthorizationResult);
        DialogEndUserSearchSqlHelpers.LogPartiesAndServicesCount(_logger, partiesAndServices);
        var permissionCandidateDialogs = BuildPermissionCandidateDialogs(query);
        var delegatedCandidateDialogs = BuildDelegatedCandidateDialogs(query, dialogSearchAuthorizationResult.DialogIds.ToArray());
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
                raw_permissions AS (
                    SELECT p.party
                         , s.service
                    FROM jsonb_to_recordset({JsonSerializer.Serialize(partiesAndServices)}::jsonb) AS x("Parties" text[], "Services" text[])
                    CROSS JOIN LATERAL unnest(x."Services") AS s(service)
                    CROSS JOIN LATERAL unnest(x."Parties") AS p(party)
                )
                ,party_permission_map AS (
                    SELECT party
                         , ARRAY_AGG(service) AS allowed_services
                    FROM raw_permissions
                    GROUP BY party
                )
                ,permission_candidate_ids AS (
                    SELECT d_inner."Id"
                    FROM party_permission_map ppm
                    CROSS JOIN LATERAL (
                        {permissionCandidateDialogs}
                    ) d_inner
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
        var permissionCandidateFilters = BuildPermissionCandidateFilters(query, includeSearchFilter: false);

        return new PostgresFormattableStringBuilder()
            .AppendIf(query.Search is null,
                """
                SELECT d."Id"
                FROM "Dialog" d
                WHERE d."Party" = ppm.party

                """)
            .AppendIf(query.Search is not null,
                """
                SELECT d."Id"
                FROM search."DialogSearch" ds
                JOIN "Dialog" d ON d."Id" = ds."DialogId"
                CROSS JOIN searchString ss
                WHERE ds."Party" = ppm.party AND ds."SearchVector" @@ ss.searchVector

                """)
            .Append(
                """
                AND d."ServiceResource" = ANY(ppm.allowed_services)
                """)
            .Append($"{permissionCandidateFilters}")
            .ApplyPaginationOrder(query.OrderBy!, alias: "d")
            .Append(
                $"""
                 LIMIT {query.Limit + 1}

                """);
    }

    private static PostgresFormattableStringBuilder BuildDelegatedCandidateDialogs(
        GetDialogsQuery query,
        Guid[] dialogIds)
    {
        var searchJoin = DialogEndUserSearchSqlHelpers.BuildSearchJoin(query.Search is not null);
        var permissionCandidateFilters = BuildPermissionCandidateFilters(query);

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

    private static PostgresFormattableStringBuilder BuildPermissionCandidateFilters(
        GetDialogsQuery query,
        bool includeSearchFilter = true) =>
        new PostgresFormattableStringBuilder()
            .AppendIf(includeSearchFilter && query.Search is not null, """ AND ds."SearchVector" @@ ss.searchVector """)
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
            .AppendSystemLabelFilterCondition(query.SystemLabel)
            .ApplyPaginationCondition(query.OrderBy!, query.ContinuationToken, alias: "d");
}
