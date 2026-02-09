EXPLAIN (ANALYZE, BUFFERS, TIMING)
SELECT d."Id", d."ContentUpdatedAt", d."CreatedAt", d."Deleted", d."DeletedAt", d."DueAt", d."ExpiresAt", d."ExtendedStatus", d."ExternalReference", d."FromPartyTransmissionsCount", d."FromServiceOwnerTransmissionsCount", d."Frozen", d."HasUnopenedContent", d."IdempotentKey", d."IsApiOnly",
  d."Org", d."Party", d."PrecedingProcess", d."Process", d."Progress", d."Revision", d."ServiceResource", d."ServiceResourceType", d."StatusId", d."UpdatedAt", d."VisibleFrom"
FROM (
    WITH permission_groups AS (
        SELECT x."Parties" AS parties
             , x."Services" AS services
        FROM jsonb_to_recordset('--PARTIESANDSERVICESPLACEHOLDER--'::jsonb) AS x("Parties" text[], "Services" text[])
    )
    ,service_permissions AS (
        SELECT s.service
             , pg.parties AS allowed_parties
        FROM permission_groups pg
        CROSS JOIN LATERAL unnest(pg.services) AS s(service)
    )
    ,permission_candidate_ids AS (
        SELECT d."Id"
        FROM service_permissions sp
        JOIN "Dialog" d ON d."ServiceResource" = sp.service
                       AND d."Party" = ANY(sp.allowed_parties)
    )
    ,delegated_dialogs AS (
        -- Replace ARRAY[] with delegated dialog IDs if you want to test this path
        SELECT unnest(ARRAY[]::uuid[]) AS "Id"
    )
    ,candidate_dialogs AS (
        SELECT "Id" FROM permission_candidate_ids
        UNION
        SELECT "Id" FROM delegated_dialogs
    )
    SELECT d.*
    FROM candidate_dialogs cd
    JOIN "Dialog" d ON d."Id" = cd."Id"
    WHERE 1=1
      AND d."StatusId" = ANY(ARRAY[7, 2, 8]::int[])
      AND (d."VisibleFrom" IS NULL OR d."VisibleFrom" <= NOW())
      AND (d."ExpiresAt" IS NULL OR d."ExpiresAt" > NOW())
      AND d."Deleted" = false::boolean
      AND EXISTS (
        SELECT 1
        FROM "DialogEndUserContext" dec
        JOIN "DialogEndUserContextSystemLabel" sl ON dec."Id" = sl."DialogEndUserContextId"
        WHERE dec."DialogId" = d."Id"
           AND sl."SystemLabelId" = 1
      )
    ORDER BY d."CreatedAt" DESC, d."Id" DESC
    LIMIT 101
) AS d
ORDER BY d."CreatedAt" DESC, d."Id" DESC
LIMIT 101
