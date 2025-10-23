using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
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

internal sealed class DialogSearchRepository(DialogDbContext db) : IDialogSearchRepository
{
    private readonly DialogDbContext _db = db ?? throw new ArgumentNullException(nameof(db));
    private static readonly OrderSet<SearchDialogQueryOrderDefinition, DialogEntity> IdDescendingOrder = new(
    [
        new Order<DialogEntity>("id", new OrderSelector<DialogEntity>(x => x.Id))
    ]);

    public Task UpsertFreeTextSearchIndex(Guid dialogId, CancellationToken cancellationToken) =>
        _db.Database.ExecuteSqlAsync(FormattableStringFactory.Create(FreeTextSearchIndexerSql, dialogId), cancellationToken);


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
        var searchTags = query.Search?.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var queryBuilder = new PostgresFormattableStringBuilder()
            .Append(
                $"""
                 WITH vectorizedSearchString AS (
                    SELECT websearch_to_tsquery(coalesce(isomap."TsConfigName", 'simple')::regconfig, {query.Search}::text) value
                    FROM (VALUES (coalesce({query.SearchLanguageCode}, 'simple'))) AS v(isoCode)
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
                       AND ({searchTags} IS NULL OR EXISTS (
                           SELECT 1
                           FROM unnest({searchTags}::text[]) as st(value)
                           INNER JOIN "DialogSearchTag" dst
                               ON dst."Value" = st.value
                               AND dst."DialogId" = d."Id"
                       ))
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
                     SELECT a.dialogId, ds."SearchVector" searchVector
                     FROM vectorizedSearchString ss, accessibleFilteredDialogs a 
                     LEFT JOIN search."DialogSearch" ds ON a.dialogId = ds."DialogId"
                     WHERE {query.Search} IS NULL OR ds."SearchVector" @@ ss.value
                     LIMIT {searchSampleLimit}
                 )
                 SELECT d.*
                 FROM vectorizedSearchString ss, accessibleFilteredSearchSample ds
                 INNER JOIN "Dialog" d ON ds.dialogId = d."Id"
                 
                 """)
            // Ordering by full text search rank only works on the first page of results
            // because pagination requires both OrderBy and ContinuationToken to be set.
            .AppendIf(query.OrderBy is null, "ORDER BY ts_rank(ds.searchVector, ss.value) DESC\n")
            .AppendIf(query.OrderBy is not null, $"{paginationOrder}\n");

        // DO NOT use Include here, as it will use the custom SQL above which is
        // much less efficient than querying further by the resulting dialogIds.
        // We only get dialogs here, and will later query related data as
        // needed based on the IDs.
        var efQuery = _db.Dialogs
            .FromSql(queryBuilder.ToFormattableString())
            .IgnoreQueryFilters()
            .AsNoTracking();

        // TODO: Add content
        // TODO: Add DialogEndUserContextSystemLabels
        // TODO: Add SeenLog

        var dialogs = await efQuery.ToPaginatedListAsync(
            query.OrderBy ?? IdDescendingOrder,
            query.ContinuationToken,
            query.Limit,
            applyOrder: false,
            applyContinuationToken: false,
            cancellationToken);

        return dialogs;

        // var dialogIds = dialogs.Select(x => x.Id).ToArray();
        // var guiAttachmentCountByDialogId = await _db.DialogAttachments
        //     .Where(x => dialogIds.Contains(x.DialogId))
        //     .Where(x => x.Urls.Any(url => url.ConsumerTypeId == AttachmentUrlConsumerType.Values.Gui))
        //     .GroupBy(x => x.DialogId)
        //     .Select(g => new { DialogId = g.Key, GuiAttachmentCount = g.Count() })
        //     .ToDictionaryAsync(x => x.DialogId, x => x.GuiAttachmentCount, cancellationToken);

        // .Include(x => x.Content.Where(c => c.Type.OutputInList))
        // .Include(x => x.EndUserContext.DialogEndUserContextSystemLabels)
        // .Include(x => x.SeenLog
        //     .Where(l => l.CreatedAt >= l.Dialog.CreatedAt ||
        //                 l.CreatedAt >= l.Dialog.ContentUpdatedAt)
        //     .OrderByDescending(sl => sl.CreatedAt))
        // .Include(x => x.Activities.OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id).Take(1));
    }

    public sealed class SearchDialogQueryOrderDefinition : IOrderDefinition<DialogEntity>
    {
        public static IOrderOptions<DialogEntity> Configure(IOrderOptionsBuilder<DialogEntity> options) =>
            options.AddId(x => x.Id)
                .AddDefault("createdAt", x => x.CreatedAt)
                .AddOption("updatedAt", x => x.UpdatedAt)
                .AddOption("contentUpdatedAt", x => x.ContentUpdatedAt)
                .AddOption("dueAt", x => x.DueAt)
                .Build();
    }

    //language=PostgreSQL
    private const string FreeTextSearchIndexerSql =
        """
        WITH dialogContent AS (
            -- Dialog Content
            SELECT dc."DialogId" dialogId
                ,CASE dc."TypeId"
                    WHEN 1 THEN 'B' -- title
                    ELSE 'D'
                END weight
                ,l."LanguageCode" languageCode
                ,l."Value" value
            FROM "DialogContent" dc
            INNER JOIN "LocalizationSet" dcls ON dc."Id" = dcls."DialogContentId"
            INNER JOIN "Localization" l ON dcls."Id" = l."LocalizationSetId"
            WHERE dc."MediaType" = 'text/plain'
            
            -- Transmission content
            UNION ALL SELECT dt."DialogId"
               ,'D' weight
               ,l."LanguageCode" languageCode
               ,l."Value" value
            FROM "DialogTransmission" dt
            INNER JOIN "DialogTransmissionContent" dtc ON dt."Id" = dtc."TransmissionId"
            INNER JOIN "LocalizationSet" dcls ON dtc."Id" = dcls."TransmissionContentId"
            INNER JOIN "Localization" l ON dcls."Id" = l."LocalizationSetId"
            WHERE dtc."MediaType" = 'text/plain'
            
            -- Activity description
            UNION ALL SELECT da."DialogId"
               ,'D' weight
               ,l."LanguageCode" languageCode
               ,l."Value" value
            FROM "DialogActivity" da
            INNER JOIN "LocalizationSet" dcls ON da."Id" = dcls."ActivityId"
            INNER JOIN "Localization" l ON dcls."Id" = l."LocalizationSetId"
            
            -- Attachment description (dialog-linked)
            UNION ALL SELECT a."DialogId"
                 ,'D'
                 ,l."LanguageCode"
                 ,l."Value"
            FROM "Attachment" a
            INNER JOIN "LocalizationSet" dcls ON dcls."AttachmentId" = a."Id"
            INNER JOIN "Localization" l ON l."LocalizationSetId" = dcls."Id"
            
            -- Attachment description (transmission-linked)
            UNION ALL SELECT dt."DialogId"
                ,'D'
                ,l."LanguageCode"
                ,l."Value"
            FROM "DialogTransmission" dt
            INNER JOIN "Attachment" a ON a."TransmissionId" = dt."Id"
            INNER JOIN "LocalizationSet" dcls ON dcls."AttachmentId" = a."Id"
            INNER JOIN "Localization" l ON l."LocalizationSetId" = dcls."Id"
        ), aggregatedVectorizedDialogContent AS (
            SELECT d."Id" AS dialogId
                ,string_agg(
                    setweight(
                        to_tsvector(COALESCE(isomap."TsConfigName", 'simple')::regconfig, value),
                        weight::"char")::text,
                    ' ')::tsvector AS document
            FROM "Dialog" d -- ensure we get a row even if no content (only if dialog exists)
            LEFT JOIN dialogContent dc ON d."Id" = dc.dialogId
            LEFT JOIN search."Iso639TsVectorMap" isomap ON dc.languageCode = isomap."IsoCode"
            GROUP BY d."Id"
        )
        INSERT INTO search."DialogSearch" ("DialogId", "UpdatedAt", "SearchVector")
        SELECT dialogId, now(), coalesce(document,''::tsvector)
        FROM aggregatedVectorizedDialogContent
        WHERE dialogId = {0}
        ON CONFLICT ("DialogId") DO UPDATE
        SET "UpdatedAt" = EXCLUDED."UpdatedAt",
            "SearchVector" = EXCLUDED."SearchVector";
        """;
}
