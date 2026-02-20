CREATE OR REPLACE VIEW search."VDialogContent" AS
WITH ScopedContent AS (
    -- 1. Dialog Content (Direct)
    SELECT
        dc."DialogId",
        CASE dc."TypeId" WHEN 1 THEN 'B' ELSE 'D' END AS "Weight",
        l."LanguageCode",
        l."Value",
        1 AS "OuterPriority",
        dc."TypeId" AS "InnerPriority"
    FROM "DialogContent" dc
    JOIN "LocalizationSet" dcls ON dc."Id" = dcls."DialogContentId"
    JOIN "Localization" l ON dcls."Id" = l."LocalizationSetId"
    WHERE dc."MediaType" = 'text/plain'

    UNION ALL

    -- 2. Search Tags
    SELECT
        dst."DialogId",
        'D'::text,
        'simple'::varchar(15),
        dst."Value"::varchar(4095),
        2,
        0
    FROM "DialogSearchTag" dst

    UNION ALL

    -- 3. Attachments for both dialog and transmissions (Capped at 1000 per Dialog)
    -- We limit the IDs BEFORE joining the heavy Localization tables
    SELECT
        a_limited."DialogId",
        'D'::text,
        l."LanguageCode",
        l."Value",
        3,
        EXTRACT(EPOCH FROM a_limited."CreatedAt")::bigint
    FROM (
        SELECT "DialogId", "Id", "CreatedAt",
               ROW_NUMBER() OVER (PARTITION BY "DialogId" ORDER BY "CreatedAt" DESC) as rnk
        FROM "Attachment"
    ) a_limited
    JOIN "LocalizationSet" dcls ON dcls."AttachmentId" = a_limited."Id"
    JOIN "Localization" l ON l."LocalizationSetId" = dcls."Id"
    WHERE a_limited.rnk <= 1000

    UNION ALL

    -- 4. Transmissions (Capped at 1000 per Dialog)
    SELECT
        dt_limited."DialogId",
        'D'::text,
        l."LanguageCode",
        l."Value",
        4,
        EXTRACT(EPOCH FROM dt_limited."CreatedAt")::bigint
    FROM (
        SELECT "DialogId", "Id", "CreatedAt",
               ROW_NUMBER() OVER (PARTITION BY "DialogId" ORDER BY "CreatedAt" DESC) as rnk
        FROM "DialogTransmission"
    ) dt_limited
    JOIN "DialogTransmissionContent" dtc ON dt_limited."Id" = dtc."TransmissionId"
    JOIN "LocalizationSet" dcls ON dtc."Id" = dcls."TransmissionContentId"
    JOIN "Localization" l ON dcls."Id" = l."LocalizationSetId"
    WHERE dt_limited.rnk <= 1000 AND dtc."MediaType" = 'text/plain'

    UNION ALL

    -- 5. Activities (Capped at 1000 per Dialog)
    SELECT
        da_limited."DialogId",
        'D'::text,
        l."LanguageCode",
        l."Value",
        5,
        EXTRACT(EPOCH FROM da_limited."CreatedAt")::bigint
    FROM (
        SELECT "DialogId", "Id", "CreatedAt",
               ROW_NUMBER() OVER (PARTITION BY "DialogId" ORDER BY "CreatedAt" DESC) as rnk
        FROM "DialogActivity"
    ) da_limited
    JOIN "LocalizationSet" dcls ON da_limited."Id" = dcls."ActivityId"
    JOIN "Localization" l ON l."LocalizationSetId" = dcls."Id"
    WHERE da_limited.rnk <= 1000
)
SELECT
    "DialogId",
    "Weight",
    "LanguageCode",
    "Value"
FROM ScopedContent
ORDER BY "DialogId", "OuterPriority", "InnerPriority";

