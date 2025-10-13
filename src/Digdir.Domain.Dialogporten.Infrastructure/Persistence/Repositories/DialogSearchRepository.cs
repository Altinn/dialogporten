using System.Runtime.CompilerServices;
using System.Text.Json;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

internal sealed class DialogSearchRepository(DialogDbContext db) : IDialogSearchRepository
{
    private readonly DialogDbContext _db = db ?? throw new ArgumentNullException(nameof(db));

    public Task UpsertFreeTextSearchIndex(Guid dialogId, CancellationToken cancellationToken) =>
        _db.Database.ExecuteSqlAsync(FormattableStringFactory.Create(FreeTextSearchIndexerSql, dialogId), cancellationToken);


    public IQueryable<DialogEntity> Prefilter(SearchDialogQuery2 query, DialogSearchAuthorizationResult authorizedResources)
    {
        const int searchSampleLimit = 10000;
        var partiesAndServices = JsonSerializer.Serialize(authorizedResources.ResourcesByParties
            .Select(x => (party: x.Key, services: x.Value))
            .GroupBy(x => x.services, new HashSetEqualityComparer<string>())
            .Select(x => new
            {
                parties = x.Select(k => k.party).ToArray(),
                services = x.Key.ToArray()
            }));

        var queryBuilder = new PostgresFormattableStringBuilder()
            .Append(
                $"""
                 WITH partyResourceAccess AS (
                     SELECT DISTINCT s.service, p.party
                     FROM jsonb_to_recordset({partiesAndServices}::jsonb) AS x(parties text[], services text[])
                     CROSS JOIN LATERAL unnest(x.services) AS s(service)
                     CROSS JOIN LATERAL unnest(x.parties) AS p(party))
                 ,accessibleDialogs AS (
                     SELECT DISTINCT d."Id" AS dialogId
                     FROM "Dialog" d
                     INNER JOIN partyResourceAccess c ON d."ServiceResource" = c.service AND d."Party" = c.party)
                 ,accessibleSearchSample AS (
                     SELECT ds."DialogId" as dialogId
                          ,ts_rank(ds."SearchVector", websearch_to_tsquery({query.Search})) rank
                     FROM accessibleDialogs a 
                     LEFT JOIN search."DialogSearch" ds ON a.dialogId = ds."DialogId"
                     WHERE ds."SearchVector" @@ websearch_to_tsquery({query.Search})
                     LIMIT {searchSampleLimit})
                 SELECT d.*
                 FROM "Dialog" d
                 INNER JOIN accessibleDialogs a ON d."Id" = a.dialogId
                 LEFT JOIN accessibleSearchSample ds ON ds.dialogId = d."Id"
                 ORDER BY ds.rank DESC NULLS LAST
                 """);

        return _db.Dialogs.FromSql(queryBuilder.ToFormattableString());
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
