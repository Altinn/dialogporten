using System.Text.Json;
using Digdir.Domain.Dialogporten.Application.Externals;
using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch;

internal sealed class PartyDrivenDialogEndUserSearchStrategy(ILogger<PartyDrivenDialogEndUserSearchStrategy> logger)
    : IDialogEndUserSearchStrategy
{
    private const int ServiceCountThreshold = 5;
    private readonly ILogger<PartyDrivenDialogEndUserSearchStrategy> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public string Name => "PartyDriven";
    public EndUserSearchContext Context { get; private set; } = null!;

    public void SetContext(EndUserSearchContext context) =>
        Context = context ?? throw new ArgumentNullException(nameof(context));

    // Party-driven is preferred when service cardinality is low.
    public int Score(EndUserSearchContext context)
    {
        var totalServiceCount = DialogEndUserSearchSqlHelpers.GetTotalServiceCount(context);
        return totalServiceCount <= ServiceCountThreshold ? 100 : 1;
    }

    public PostgresFormattableStringBuilder BuildSql()
    {
        var context = Context;
        var query = context.Query;
        // Builds party/service groups once to keep lateral probing localized per party.
        var partiesAndServices = DialogEndUserSearchSqlHelpers.BuildPartiesAndServices(
            query,
            context.AuthorizedResources);
        DialogEndUserSearchSqlHelpers.LogPartiesAndServicesCount(_logger, partiesAndServices);
        var permissionCandidateDialogs = BuildPermissionCandidateDialogs(query);
        var postPermissionFilters = DialogEndUserSearchSqlHelpers.BuildPostPermissionFilters(
            query,
            includeSearchFilter: false,
            includePaginationCondition: false);

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
                ,party_permission_map AS (
                    SELECT p.party
                         , ARRAY_AGG(s.service) AS allowed_services
                    FROM permission_groups pg
                    CROSS JOIN LATERAL unnest(pg.services) AS s(service)
                    CROSS JOIN LATERAL unnest(pg.parties) AS p(party)
                    GROUP BY p.party
                )
                ,permission_candidate_ids AS (
                    SELECT d_inner."Id"
                    FROM party_permission_map pp
                    CROSS JOIN LATERAL (
                        {permissionCandidateDialogs}
                    ) d_inner
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
                {postPermissionFilters}

                """);
    }

    private static PostgresFormattableStringBuilder BuildPermissionCandidateDialogs(GetDialogsQuery query)
    {
        var builder = new PostgresFormattableStringBuilder();

        return builder
            .AppendIf(query.Search is null,
                """
                SELECT d."Id"
                FROM "Dialog" d
                WHERE d."Party" = pp.party

                """)
            .AppendIf(query.Search is not null,
                """
                SELECT d."Id"
                FROM search."DialogSearch" ds
                JOIN "Dialog" d ON d."Id" = ds."DialogId"
                CROSS JOIN searchString ss
                WHERE ds."Party" = pp.party AND ds."SearchVector" @@ ss.searchVector

                """)
            .Append(
                """
                AND d."ServiceResource" = ANY(pp.allowed_services)

                """);
    }

}
