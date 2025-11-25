using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

internal sealed class DialogSearchRepository(DialogDbContext dbContext) : IDialogSearchRepository
{
    private readonly DialogDbContext _db = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task UpsertFreeTextSearchIndex(Guid dialogId, CancellationToken cancellationToken)
    {
        await _db.Database.ExecuteSqlAsync($@"SELECT search.""UpsertDialogSearchOne""({dialogId})", cancellationToken);
    }

    public async Task<int> SeedFullAsync(bool resetExisting, CancellationToken ct) =>
        await _db.Database
            .SqlQuery<int>($@"SELECT search.""SeedDialogSearchQueueFull""({resetExisting}) AS ""Value""")
            .SingleAsync(ct);

    public async Task<int> SeedSinceAsync(DateTimeOffset since, bool resetMatching, CancellationToken ct) =>
        await _db.Database
            .SqlQuery<int>($@"SELECT search.""SeedDialogSearchQueueSince""({since}, {resetMatching}) AS ""Value""")
            .SingleAsync(ct);

    public async Task<int> SeedStaleAsync(bool resetMatching, CancellationToken ct) =>
        await _db.Database
            .SqlQuery<int>($@"SELECT search.""SeedDialogSearchQueueStale""({resetMatching}) AS ""Value""")
            .SingleAsync(ct);

    public async Task<int> WorkBatchAsync(int batchSize, long workMemBytes, bool staleFirst, CancellationToken ct) =>
        await _db.Database
            .SqlQuery<int>($@"SELECT search.""RebuildDialogSearchOnce""({(staleFirst ? "stale_first" : "standard")}, {batchSize}, {workMemBytes}) AS ""Value""")
            .SingleAsync(ct);

    public async Task<DialogSearchReindexProgress> GetProgressAsync(CancellationToken ct) =>
        await _db.Database
            .SqlQuery<DialogSearchReindexProgress>(
                $"""
                 SELECT "Total", "Pending", "Processing", "Done"
                 FROM search."DialogSearchRebuildProgress"
                 """)
            .SingleAsync(ct);

    public async Task OptimizeIndexAsync(CancellationToken ct)
    {
        await _db.Database.ExecuteSqlAsync($@"VACUUM ANALYZE search.""DialogSearch""", ct);
    }

    [SuppressMessage("Style", "IDE0037:Use inferred member name")]
    public async Task<PaginatedList<DialogEntity>> GetDialogs(
        GetDialogsQuery query,
        DialogSearchAuthorizationResult authorizedResources,
        CancellationToken cancellationToken)
    {
        const int searchSampleLimit = 10_000;

        if (query.Limit > searchSampleLimit)
        {
            throw new ArgumentOutOfRangeException(nameof(query.Limit),
                $"Limit cannot be greater than the search sample limit of {searchSampleLimit}.");
        }

        if (authorizedResources.HasNoAuthorizations)
        {
            return new PaginatedList<DialogEntity>([], false, null, query.OrderBy!.GetOrderString());
        }

        var partiesAndServices = JsonSerializer.Serialize(authorizedResources.ResourcesByParties
            .Select(x => (party: x.Key, services: x.Value))
            .GroupBy(x => x.services, new HashSetEqualityComparer<string>())
            .Select(x => new
            {
                parties = x.Select(k => k.party)
                    .Where(p => query.Party.IsNullOrEmpty() || query.Party.Contains(p))
                    .ToArray(),
                services = x.Key
                    .Where(s => query.ServiceResource.IsNullOrEmpty() || query.ServiceResource.Contains(s))
                    .ToArray()
            })
            .Where(x => x.parties.Length > 0 && x.services.Length > 0));

        // TODO: Respect instance delegated dialogs
        var accessibleFilteredDialogs = new PostgresFormattableStringBuilder()
            .AppendIf(query.Search is null,
                """
                SELECT d.* FROM "Dialog" d
                WHERE d."Party" = ppm.party
                
                """)
            .AppendIf(query.Search is not null,
                """
                SELECT d.*, ds."SearchVector"
                FROM search."DialogSearch" ds 
                JOIN "Dialog" d ON d."Id" = ds."DialogId"
                CROSS JOIN searchString ss
                WHERE ds."Party" = ppm.party AND ds."SearchVector" @@ ss.searchVector
                
                """)
            .Append(
                """
                AND (d."ServiceResource" || '') = ANY(ppm.allowed_services)
                
                """)
            .AppendIf(query.Deleted is not null, $""" AND d."Deleted" = {query.Deleted}::boolean """)
            .AppendIf(query.VisibleAfter is not null, $""" AND (d."VisibleFrom" IS NULL OR d."VisibleFrom" <= {query.VisibleAfter}::timestamptz) """)
            .AppendIf(query.ExpiresBefore is not null, $""" AND (d."ExpiresAt" IS NULL OR d."ExpiresAt" > {query.ExpiresBefore}::timestamptz) """)
            .AppendIf(!query.Org.IsNullOrEmpty(), $""" AND d."Org" = ANY({query.Org}::text[]) """)
            .AppendIf(!query.ExtendedStatus.IsNullOrEmpty(), $""" AND d."ExtendedStatus" = ANY({query.ExtendedStatus}::text[]) """)
            .AppendIf(query.ExternalReference is not null, $""" AND d."ExternalReference" = {query.ExternalReference}::text """)
            .AppendIf(!query.Status.IsNullOrEmpty(), $""" AND d."StatusId" = ANY({query.Status}::int[]) """)
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
            .AppendIf(!query.SystemLabel.IsNullOrEmpty(),
                $"""
                 AND (
                     SELECT COUNT(sl."SystemLabelId")
                     FROM "DialogEndUserContext" dec 
                     JOIN "DialogEndUserContextSystemLabel" sl ON dec."Id" = sl."DialogEndUserContextId"
                     WHERE dec."DialogId" = d."Id"
                        AND sl."SystemLabelId" = ANY({query.SystemLabel}::int[]) 
                     ) = {query.SystemLabel?.Count}::int
                 """)
            .ApplyPaginationCondition(query.OrderBy!, query.ContinuationToken, alias: "d")
            .ApplyPaginationOrder(query.OrderBy!, alias: "d")
            .ApplyPaginationLimit(query.Limit);

        var searchCte = "";
        if (query.Search is not null)
        {
            searchCte = $"""
            searchString AS (
                SELECT websearch_to_tsquery(coalesce(isomap."TsConfigName", 'simple')::regconfig, {query.Search}::text) searchVector
                ,string_to_array({query.Search}::text, ' ') AS searchTerms
                FROM (VALUES (coalesce({query.SearchLanguageCode}::text, 'simple'))) AS v(isoCode)
                LEFT JOIN search."Iso639TsVectorMap" isomap ON v.isoCode = isomap."IsoCode"
                LIMIT 1
                ),

            """;
        }

        var queryBuilder = new PostgresFormattableStringBuilder()
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
                    SELECT p.party, s.service
                    FROM jsonb_to_recordset({partiesAndServices}::jsonb) AS x(parties text[], services text[])
                    CROSS JOIN LATERAL unnest(x.services) AS s(service)
                    CROSS JOIN LATERAL unnest(x.parties) AS p(party)
                 )
                 ,party_permission_map AS (
                     SELECT party
                          , ARRAY_AGG(service) AS allowed_services
                     FROM raw_permissions
                     GROUP BY party
                 )
                 SELECT d_inner.*
                 FROM party_permission_map ppm
                 CROSS JOIN LATERAL (
                     {accessibleFilteredDialogs}
                 ) d_inner
                 """);

        // DO NOT use Include here, as it will use the custom SQL above which is
        // much less efficient than querying further by the resulting dialogIds.
        // We only get dialogs here, and will later query related data as
        // needed based on the IDs.
        var efQuery = _db.Dialogs
            .FromSql(queryBuilder.ToFormattableString())
            .IgnoreQueryFilters()
            .AsNoTracking();

        var dialogs = await efQuery.ToPaginatedListAsync(
            query.OrderBy!,
            query.ContinuationToken,
            query.Limit,
            applyOrder: true,
            applyContinuationToken: false,
            cancellationToken);

        return dialogs;
    }
}
