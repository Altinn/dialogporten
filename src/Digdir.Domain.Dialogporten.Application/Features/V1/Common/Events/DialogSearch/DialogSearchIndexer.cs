using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Events;
using Digdir.Domain.Dialogporten.Domain.Localizations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Events.DialogSearch;

internal sealed class DialogSearchIndexer(IDialogDbContext db) : INotificationHandler<DialogCreatedDomainEvent>, INotificationHandler<DialogUpdatedDomainEvent>
{
    private readonly IDialogDbContext _db = db ?? throw new ArgumentNullException(nameof(db));

    public Task Handle(DialogCreatedDomainEvent notification, CancellationToken cancellationToken) =>
        UpdateIndex(notification.DialogId, cancellationToken);

    public Task Handle(DialogUpdatedDomainEvent notification, CancellationToken cancellationToken) =>
        UpdateIndex(notification.DialogId, cancellationToken);

    private async Task UpdateIndex(Guid dialogId, CancellationToken cancellationToken)
    {
        Func<Localization, NpgsqlTsVector> toTsVector = loc => EF.Functions.ToTsVector(loc.Value);

        var dialog = await _db.Dialogs
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.Id == dialogId)
            .Select(x => new
            {
                Content = x.Content
                    .Where(c => c.MediaType == MediaTypes.PlainText)
                    .SelectMany(c => c.Value.Localizations, (content, loc) => new
                    {
                        content.TypeId,
                        loc.LanguageCode,
                        Value = toTsVector(loc)
                    })
                    .ToList(),
                AttachmentDisplayNames = x.Attachments
                    .SelectMany(a => a.DisplayName!.Localizations)
                    .ToList(),
                TransmissionContent = x.Transmissions
                    .SelectMany(t => t.Content)
                    .Where(c => c.MediaType == MediaTypes.PlainText)
                    .SelectMany(c => c.Value.Localizations, (content, loc) => new
                    {
                        content.TypeId,
                        loc.LanguageCode,
                        loc.Value
                    })
                    .ToList(),
                ActivityLala = x.Activities
                    .SelectMany(a => a.Description!.Localizations)
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);


        if (dialog is null)
        {
            return;
        }

    }

    // language=PostgreSQL
    private const string SQL =
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
        )
        INSERT INTO search."DialogSearch" ("DialogId", "UpdatedAt", "SearchVector")
        SELECT dialogId,
           now() "UpdatedAt",
           string_agg(
               setweight(
                   to_tsvector(COALESCE(ismap."TsConfigName", 'simple')::regconfig, value),
                   weight::"char"
               )::text,
           ' '
           )::tsvector document
        FROM dialocContent
        LEFT JOIN search."Iso639TsVectorMap" ismap ON dialocContent.languageCode = ismap."IsoCode"
        WHERE dialogId = :dialogId
        GROUP BY dialogId
        ON CONFLICT ("DialogId") DO UPDATE
        SET "UpdatedAt" = EXCLUDED."UpdatedAt",
           "SearchVector" = EXCLUDED."SearchVector";
        """;
}

