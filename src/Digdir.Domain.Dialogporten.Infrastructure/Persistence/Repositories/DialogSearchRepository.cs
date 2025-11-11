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
                    .Where(p => query.Party is null || query.Party.Contains(p))
                    .ToArray(),
                services = x.Key
                    .Where(s => query.ServiceResource is null || query.ServiceResource.Contains(s))
                    .ToArray()
            })
            .Where(x => x.parties.Length > 0 && x.services.Length > 0));

        var accessibleFilteredDialogs = new PostgresFormattableStringBuilder()
            .Append($"""
                     SELECT d.* ,ds."SearchVector" as searchVector
                     FROM "Dialog" d
                     LEFT JOIN accessiblePartyServiceTuple ps ON d."ServiceResource" = ps.service AND d."Party" = ps.party
                     LEFT JOIN search."DialogSearch" ds ON d."Id" = ds."DialogId"
                     CROSS JOIN searchString ss
                     WHERE 1=1
                        AND (ps.party IS NOT NULL OR d."Id" = ANY({authorizedResources.DialogIds}::uuid[]))
                     """)
            .AppendIf(query.Search is not null,
                """
                AND (ds."SearchVector" @@ ss.searchVector OR EXISTS (
                    SELECT 1
                    FROM "DialogSearchTag" dst
                    WHERE dst."DialogId" = d."Id"
                    AND dst."Value" = ANY(ss.searchTerms)
                ))
                """)
            .AppendIf(query.Deleted is not null, $""" AND d."Deleted" = {query.Deleted}::boolean """)
            .AppendIf(query.VisibleAfter is not null, $""" AND (d."VisibleFrom" IS NULL OR d."VisibleFrom" <= {query.VisibleAfter}::timestamptz) """)
            .AppendIf(query.ExpiresBefore is not null, $""" AND (d."ExpiresAt" IS NULL OR d."ExpiresAt" > {query.ExpiresBefore}::timestamptz) """)
            .AppendIf(query.Org is not null, $""" AND d."Org" = ANY({query.Org}::text[]) """)
            .AppendIf(query.ServiceResource is not null, $""" AND d."ServiceResource" = ANY({query.ServiceResource}::text[]) """)
            .AppendIf(query.Party is not null, $""" AND d."Party" = ANY({query.Party}::text[]) """)
            .AppendIf(query.ExtendedStatus is not null, $""" AND d."ExtendedStatus" = ANY({query.ExtendedStatus}::text[]) """)
            .AppendIf(query.ExternalReference is not null, $""" AND d."ExternalReference" = {query.ExternalReference})::text """)
            .AppendIf(query.Status is not null, $""" AND d."StatusId" = ANY({query.Status}::int[]) """)
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
            .AppendIf(query.SystemLabel is not null,
                $"""
                AND NOT EXISTS (
                    SELECT 1
                    FROM unnest({query.SystemLabel}::int[]) as st(value)
                    LEFT JOIN "DialogEndUserContext" dec ON dec."DialogId" = d."Id"
                    LEFT JOIN "DialogEndUserContextSystemLabel" sl 
                        ON sl."DialogEndUserContextId" = dec."Id" 
                        AND sl."SystemLabelId" = st.value
                    WHERE sl."DialogEndUserContextId" IS NULL
                )
                """)
            .ApplyPaginationCondition(query.OrderBy!, query.ContinuationToken)
            .ApplyPaginationOrder(query.OrderBy!);

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
                 ),accessibleFilteredDialogs AS (
                    {accessibleFilteredDialogs}
                 )
                 SELECT ds.*
                 FROM accessibleFilteredDialogs ds
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
