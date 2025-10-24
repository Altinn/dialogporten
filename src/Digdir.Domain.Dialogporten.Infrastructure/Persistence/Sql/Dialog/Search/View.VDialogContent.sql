-- Collects every plain-text fragment per dialog so all rebuild workers share identical inputs.
CREATE OR REPLACE VIEW search."VDialogContent" AS
SELECT dc."DialogId" AS "DialogId",
       CASE dc."TypeId" WHEN 1 THEN 'B' ELSE 'D' END AS "Weight",
       l."LanguageCode" AS "LanguageCode",
       l."Value"        AS "Value"
FROM "DialogContent" dc
JOIN "LocalizationSet" dcls ON dc."Id" = dcls."DialogContentId"
JOIN "Localization"   l     ON dcls."Id" = l."LocalizationSetId"
WHERE dc."MediaType" = 'text/plain'

UNION ALL
SELECT dt."DialogId", 'D', l."LanguageCode", l."Value"
FROM "DialogTransmission" dt
JOIN "DialogTransmissionContent" dtc ON dt."Id" = dtc."TransmissionId"
JOIN "LocalizationSet" dcls          ON dtc."Id" = dcls."TransmissionContentId"
JOIN "Localization"   l              ON dcls."Id" = l."LocalizationSetId"
WHERE dtc."MediaType" = 'text/plain'

UNION ALL
SELECT da."DialogId", 'D', l."LanguageCode", l."Value"
FROM "DialogActivity" da
JOIN "LocalizationSet" dcls ON da."Id" = dcls."ActivityId"
JOIN "Localization"   l     ON l."LocalizationSetId" = dcls."Id"

UNION ALL
-- Dialog-linked attachments are discoverable alongside the root dialog text.
SELECT a."DialogId", 'D', l."LanguageCode", l."Value"
FROM "Attachment" a
JOIN "LocalizationSet" dcls ON dcls."AttachmentId" = a."Id"
JOIN "Localization"   l     ON l."LocalizationSetId" = dcls."Id"

UNION ALL
-- Transmission attachments inherit the enclosing dialog as their search scope.
SELECT dt."DialogId", 'D', l."LanguageCode", l."Value"
FROM "DialogTransmission" dt
JOIN "Attachment" a         ON a."TransmissionId" = dt."Id"
JOIN "LocalizationSet" dcls ON dcls."AttachmentId" = a."Id"
JOIN "Localization"   l     ON l."LocalizationSetId" = dcls."Id";
