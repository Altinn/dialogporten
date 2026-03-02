CREATE OR REPLACE VIEW search."VDialogContent" AS
SELECT
    d."Id" AS "DialogId",
    c."Weight",
    c."LanguageCode",
    c."Value",
    c."OuterPriority",
    c."InnerPriority"
FROM "Dialog" d
JOIN LATERAL (
    -- 1. Dialog Content (Direct)
    SELECT
        CASE dc."TypeId" WHEN 1 THEN 'B' ELSE 'D' END AS "Weight",
        l."LanguageCode",
        l."Value",
        1 AS "OuterPriority",
        dc."TypeId" AS "InnerPriority"
    FROM "DialogContent" dc
    JOIN "LocalizationSet" dcls ON dc."Id" = dcls."DialogContentId"
    JOIN "Localization" l ON dcls."Id" = l."LocalizationSetId"
    WHERE dc."DialogId" = d."Id"
      AND dc."MediaType" = 'text/plain'

    UNION ALL

    -- 2. Search Tags
    SELECT
        'D'::text,
        'simple'::varchar(15),
        dst."Value"::varchar(4095),
        2,
        0
    FROM "DialogSearchTag" dst
    WHERE dst."DialogId" = d."Id"

    UNION ALL

    -- 3. Transmission contents (Capped at 1000 per Dialog)
    SELECT
        'D'::text,
        l."LanguageCode",
        l."Value",
        3,
        EXTRACT(EPOCH FROM dtc_limited."CreatedAt")::bigint
    FROM (
        SELECT dtc."Id", dt."CreatedAt"
        FROM "DialogTransmission" dt
        JOIN "DialogTransmissionContent" dtc ON dtc."TransmissionId" = dt."Id"
        WHERE dt."DialogId" = d."Id"
          AND dtc."MediaType" = 'text/plain'
        ORDER BY dt."Id" DESC, dtc."Id" DESC
        LIMIT 1000
    ) dtc_limited
    JOIN "LocalizationSet" dcls ON dtc_limited."Id" = dcls."TransmissionContentId"
    JOIN "Localization" l ON dcls."Id" = l."LocalizationSetId"

    UNION ALL

    -- 4. Attachments for dialogs (Capped at 1000 per Dialog)
    SELECT
        'D'::text,
        l."LanguageCode",
        l."Value",
        4,
        EXTRACT(EPOCH FROM a_limited."CreatedAt")::bigint
    FROM (
        SELECT a."Id", a."CreatedAt"
        FROM "Attachment" a
        WHERE a."DialogId" = d."Id"
        ORDER BY a."Id" DESC
        LIMIT 1000
    ) a_limited
    JOIN "LocalizationSet" dcls ON dcls."AttachmentId" = a_limited."Id"
    JOIN "Localization" l ON l."LocalizationSetId" = dcls."Id"

    UNION ALL

    -- 5. Attachments for transmissions (Capped at 1000 per Dialog)
    SELECT
        'D'::text,
        l."LanguageCode",
        l."Value",
        5,
        EXTRACT(EPOCH FROM at_limited."CreatedAt")::bigint
    FROM (
        SELECT a."Id", a."CreatedAt"
        FROM "Attachment" a
        JOIN "DialogTransmission" dt ON a."TransmissionId" = dt."Id"
        WHERE dt."DialogId" = d."Id"
        ORDER BY a."Id" DESC
        LIMIT 1000
    ) at_limited
    JOIN "LocalizationSet" dcls ON dcls."AttachmentId" = at_limited."Id"
    JOIN "Localization" l ON l."LocalizationSetId" = dcls."Id"

    UNION ALL

    -- 6. Activities (Capped at 1000 per Dialog)
    SELECT
        'D'::text,
        l."LanguageCode",
        l."Value",
        6,
        EXTRACT(EPOCH FROM da_limited."CreatedAt")::bigint
    FROM (
        SELECT da."Id", da."CreatedAt"
        FROM "DialogActivity" da
        WHERE da."DialogId" = d."Id"
        ORDER BY da."Id" DESC
        LIMIT 1000
    ) da_limited
    JOIN "LocalizationSet" dcls ON da_limited."Id" = dcls."ActivityId"
    JOIN "Localization" l ON l."LocalizationSetId" = dcls."Id"
) c ON TRUE
;
