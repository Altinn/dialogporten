using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Order;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.OrderOption;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

internal sealed class DialogSearchRepository(DialogDbContext dbContext) : IDialogSearchRepository
{
    private readonly DialogDbContext _db = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private static readonly OrderSet<SearchDialogQueryOrderDefinition, DialogEntity> IdDescendingOrder = new(
    [
        new Order<DialogEntity>("id", new OrderSelector<DialogEntity>(x => x.Id))
    ]);

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
            return new PaginatedList<DialogEntity>([], false, null, IdDescendingOrder.GetOrderString());
        }

        var partiesAndServices = JsonSerializer.Serialize(authorizedResources.ResourcesByParties
            .Select(x => (party: x.Key, services: x.Value))
            .GroupBy(x => x.services, new HashSetEqualityComparer<string>())
            .Select(x => new
            {
                parties = x.Select(k => k.party).ToArray(),
                services = x.Key.ToArray()
            }));

        var paginationCondition = new PostgresFormattableStringBuilder()
            .ApplyPaginationCondition(query.OrderBy ?? IdDescendingOrder, query.ContinuationToken);
        var paginationOrder = new PostgresFormattableStringBuilder()
            .ApplyPaginationOrder(query.OrderBy ?? IdDescendingOrder);

        var queryBuilder = new PostgresFormattableStringBuilder()
            .Append(
                $"""
                 WITH searchString AS (
                    SELECT websearch_to_tsquery(coalesce(isomap."TsConfigName", 'simple')::regconfig, {query.Search}::text) searchVector
                        ,string_to_array({query.Search}::text, ' ') AS searchTerms
                    FROM (VALUES (coalesce({query.SearchLanguageCode}::text, 'simple'))) AS v(isoCode)
                    LEFT JOIN search."Iso639TsVectorMap" isomap ON v.isoCode = isomap."IsoCode"
                    LIMIT 1
                 ),accessiblePartyServiceTuple AS (
                    SELECT DISTINCT p.party, s.service
                    FROM jsonb_to_recordset({partiesAndServices}::jsonb) AS x(parties text[], services text[])
                    CROSS JOIN LATERAL unnest(x.services) AS s(service)
                    CROSS JOIN LATERAL unnest(x.parties) AS p(party)
                 ),accessibleDialogs AS (
                    SELECT d."Id" AS dialogId
                    FROM "Dialog" d
                    LEFT JOIN accessiblePartyServiceTuple ps ON d."ServiceResource" = ps.service AND d."Party" = ps.party
                    WHERE ps.party IS NOT NULL OR d."Id" = ANY({authorizedResources.DialogIds}::uuid[])
                 ),accessibleFilteredDialogs AS (
                    SELECT d."Id" AS dialogId
                    FROM "Dialog" d
                    INNER JOIN accessibleDialogs a ON d."Id" = a.dialogId
                    WHERE ({query.Deleted} IS NULL OR d."Deleted" = {query.Deleted}::boolean)
                       AND ({query.VisibleAfter} IS NULL OR d."VisibleFrom" IS NULL OR d."VisibleFrom" <= {query.VisibleAfter}::timestamptz)
                       AND ({query.ExpiresBefore} IS NULL OR d."ExpiresAt" IS NULL OR d."ExpiresAt" > {query.ExpiresBefore}::timestamptz)
                       AND ({query.Org} IS NULL OR d."Org" = ANY({query.Org}::text[]))
                       AND ({query.ServiceResource} IS NULL OR d."ServiceResource" = ANY({query.ServiceResource}::text[]))
                       AND ({query.Party} IS NULL OR d."Party" = ANY({query.Party}::text[]))
                       AND ({query.ExtendedStatus} IS NULL OR d."ExtendedStatus" = ANY({query.ExtendedStatus}::text[]))
                       AND ({query.ExternalReference} IS NULL OR d."ExternalReference" = {query.ExternalReference}::text)
                       AND ({query.Status} IS NULL OR d."StatusId" = ANY({query.Status}::int[]))
                       AND ({query.CreatedAfter} IS NULL OR {query.CreatedAfter}::timestamptz <= d."CreatedAt")
                       AND ({query.CreatedBefore} IS NULL OR d."CreatedAt" <= {query.CreatedBefore}::timestamptz)
                       AND ({query.UpdatedAfter} IS NULL OR {query.UpdatedAfter}::timestamptz <= d."UpdatedAt")
                       AND ({query.UpdatedBefore} IS NULL OR d."UpdatedAt" <= {query.UpdatedBefore}::timestamptz)
                       AND ({query.ContentUpdatedAfter} IS NULL OR {query.ContentUpdatedAfter}::timestamptz <= d."ContentUpdatedAt")
                       AND ({query.ContentUpdatedBefore} IS NULL OR d."ContentUpdatedAt" <= {query.ContentUpdatedBefore}::timestamptz)
                       AND ({query.DueAfter} IS NULL OR {query.DueAfter}::timestamptz <= d."DueAt")
                       AND ({query.DueBefore} IS NULL OR d."DueAt" <= {query.DueBefore}::timestamptz)
                       AND ({query.Process} IS NULL OR d."Process" = {query.Process}::text)
                       AND ({query.ExcludeApiOnly} IS NULL OR ({query.ExcludeApiOnly}::boolean = false OR {query.ExcludeApiOnly}::boolean = true AND d."IsApiOnly" = false))
                       AND ({query.SystemLabel} IS NULL OR NOT EXISTS (
                           SELECT 1
                           FROM unnest({query.SystemLabel}::int[]) as st(value)
                           LEFT JOIN "DialogEndUserContext" dec ON dec."DialogId" = d."Id"
                           LEFT JOIN "DialogEndUserContextSystemLabel" sl 
                               ON sl."DialogEndUserContextId" = dec."Id" 
                               AND sl."SystemLabelId" = st.value
                           WHERE sl."DialogEndUserContextId" IS NULL
                       ))
                       {paginationCondition}
                    {paginationOrder}
                 ),accessibleFilteredSearchSample AS (
                     SELECT a.dialogId, ds."SearchVector" as searchVector
                     FROM searchString ss, accessibleFilteredDialogs a 
                     LEFT JOIN search."DialogSearch" ds ON a.dialogId = ds."DialogId"
                     WHERE (ss.searchVector IS NULL OR ds."SearchVector" @@ ss.searchVector)
                        OR (ss.searchTerms IS NULL OR EXISTS (
                             SELECT 1
                             FROM "DialogSearchTag" dst
                             WHERE dst."DialogId" = a.dialogId 
                                AND dst."Value" = ANY(ss.searchTerms)
                         ))
                     LIMIT {searchSampleLimit}
                 )
                 SELECT d.*
                 FROM searchString ss, accessibleFilteredSearchSample ds
                 INNER JOIN "Dialog" d ON ds.dialogId = d."Id"
                 
                 """)
            // Ordering by full text search rank only works on the first page of results
            // because pagination requires both OrderBy and ContinuationToken to be set.
            .AppendIf(query.OrderBy is null, "ORDER BY ts_rank(ds.searchVector, ss.searchVector) DESC\n")
            .AppendIf(query.OrderBy is not null, $"{paginationOrder}\n");

        // DO NOT use Include here, as it will use the custom SQL above which is
        // much less efficient than querying further by the resulting dialogIds.
        // We only get dialogs here, and will later query related data as
        // needed based on the IDs.
        var efQuery = _db.Dialogs
            .FromSql(queryBuilder.ToFormattableString())
            .IgnoreQueryFilters()
            .AsNoTracking();

        var dialogs = await efQuery.ToPaginatedListAsync(
            query.OrderBy ?? IdDescendingOrder,
            query.ContinuationToken,
            query.Limit,
            applyOrder: false,
            applyContinuationToken: false,
            cancellationToken);

        return dialogs;
    }
}
