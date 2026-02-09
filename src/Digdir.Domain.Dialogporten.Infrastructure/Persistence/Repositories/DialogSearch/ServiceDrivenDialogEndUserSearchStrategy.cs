using System.Text.Json;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch;

internal sealed class ServiceDrivenDialogEndUserSearchStrategy(ILogger<ServiceDrivenDialogEndUserSearchStrategy> logger)
    : IDialogEndUserSearchStrategy
{
    private const int ServiceCountThreshold = 5;
    private readonly ILogger<ServiceDrivenDialogEndUserSearchStrategy> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public string Name => "ServiceDriven";
    public EndUserSearchContext Context { get; private set; } = null!;

    public void SetContext(EndUserSearchContext context) =>
        Context = context ?? throw new ArgumentNullException(nameof(context));

    // Service-driven is preferred when service cardinality is high.
    public int Score(EndUserSearchContext context)
    {
        var totalServiceCount = DialogEndUserSearchSqlHelpers.GetTotalServiceCount(context);
        return totalServiceCount > ServiceCountThreshold ? 100 : 1;
    }

    public PostgresFormattableStringBuilder BuildSql()
    {
        var context = Context;
        var query = context.Query;
        // Service-first candidate scan reduces per-party random I/O when services are limited.
        var partiesAndServices = DialogEndUserSearchSqlHelpers.BuildPartiesAndServices(
            query,
            context.AuthorizedResources);
        DialogEndUserSearchSqlHelpers.LogPartiesAndServicesCount(_logger, partiesAndServices);
        var permissionCandidateDialogs = BuildPermissionCandidateDialogs();
        var postPermissionFilters = DialogEndUserSearchSqlHelpers.BuildPostPermissionFilters(
            query,
            includeSearchFilter: query.Search is not null,
            includePaginationCondition: true);
        var searchJoin = DialogEndUserSearchSqlHelpers.BuildSearchJoin(query.Search is not null);

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
                    SELECT unnest({context.AuthorizedResources.DialogIds.ToArray()}::uuid[]) AS "Id"
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

    private static PostgresFormattableStringBuilder BuildPermissionCandidateDialogs() =>
        new PostgresFormattableStringBuilder()
            .Append(
                """
                SELECT d."Id"
                FROM service_permissions sp
                JOIN "Dialog" d ON d."ServiceResource" = sp.service
                               AND d."Party" = ANY(sp.allowed_parties)
                WHERE 1=1

                """);

}
