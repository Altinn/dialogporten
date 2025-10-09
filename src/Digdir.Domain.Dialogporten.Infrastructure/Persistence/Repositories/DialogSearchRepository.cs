using System.Runtime.CompilerServices;
using Digdir.Domain.Dialogporten.Application.Externals;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

internal sealed class DialogSearchRepository(DialogDbContext db) : IDialogSearchRepository
{
    private readonly DialogDbContext _db = db ?? throw new ArgumentNullException(nameof(db));

    public Task UpsertFreeTextSearchIndex(Guid dialogId, CancellationToken cancellationToken) =>
        _db.Database.ExecuteSqlAsync(FormattableStringFactory.Create(FreeTextSearchIndexerSql, dialogId), cancellationToken);

    //language=PostgreSQL
    private const string FreeTextSearchIndexerSql =
        """
        WITH dialocContent AS (
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
            
            -- Attachment description (can be linked to dialog or transmission)
            UNION ALL SELECT DISTINCT coalesce(a."DialogId", dt."DialogId") dialogId
                ,'D' weight
                ,l."LanguageCode" languageCode
                ,l."Value" value
            FROM "Attachment" a
            LEFT JOIN "DialogTransmission" dt ON a."TransmissionId" = dt."Id"
            INNER JOIN "LocalizationSet" dcls ON a."Id" = dcls."AttachmentId"
            INNER JOIN "Localization" l ON dcls."Id" = l."LocalizationSetId"
        ), aggregatedVectorizedDialogContent AS (
            SELECT d."Id" AS dialogId
                ,string_agg(
                    setweight(
                        to_tsvector(COALESCE(isomap."TsConfigName", 'simple')::regconfig, value),
                        weight::"char")::text,
                    ' ')::tsvector AS document
            FROM "Dialog" d -- ensure we get a row even if no content (only if dialog exists)
            LEFT JOIN dialocContent dc ON d."Id" = dc.dialogId
            LEFT JOIN search."Iso639TsVectorMap" isomap ON dc.languageCode = isomap."IsoCode"
            GROUP BY d."Id"
        )
        INSERT INTO search."DialogSearch" ("DialogId", "UpdatedAt", "SearchVector")
        SELECT dialogId, now(), coalesce(document,'')
        FROM aggregatedVectorizedDialogContent
        WHERE dialogId = {0}
        ON CONFLICT ("DialogId") DO UPDATE
        SET "UpdatedAt" = EXCLUDED."UpdatedAt",
            "SearchVector" = EXCLUDED."SearchVector";
        """;
}
