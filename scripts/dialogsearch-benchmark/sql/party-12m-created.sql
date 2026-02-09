EXPLAIN (ANALYZE, BUFFERS, TIMING)
SELECT d."Id", d."ContentUpdatedAt", d."CreatedAt", d."Deleted", d."DeletedAt", d."DueAt", d."ExpiresAt", d."ExtendedStatus", d."ExternalReference", d."FromPartyTransmissionsCount", d."FromServiceOwnerTransmissionsCount", d."Frozen", d."HasUnopenedContent", d."IdempotentKey", d."IsApiOnly", d."Org", d."Party", d."PrecedingProcess", d."Process", d."Progress", d."Revision", d."ServiceResource", d."ServiceResourceType", d."StatusId", d."UpdatedAt", d."VisibleFrom"
FROM (
    WITH raw_permissions AS (
       SELECT p.party, s.service
       FROM jsonb_to_recordset('--PARTIESANDSERVICESPLACEHOLDER--'::jsonb) AS x("Parties" text[], "Services" text[])
       CROSS JOIN LATERAL unnest(x."Services") AS s(service)
       CROSS JOIN LATERAL unnest(x."Parties") AS p(party)
    )
    ,party_permission_map AS (
        SELECT party
             , ARRAY_AGG(service) AS allowed_services
        FROM raw_permissions
        GROUP BY party
    )
    SELECT d.*
    FROM (
        SELECT d_inner."Id"
        FROM party_permission_map ppm
        CROSS JOIN LATERAL (
            SELECT d."Id"
    FROM "Dialog" d
    WHERE d."Party" = ppm.party
    AND d."ServiceResource" = ANY(ppm.allowed_services)
           AND d."StatusId" = ANY(ARRAY[7, 2, 8]::int[])
      AND (d."VisibleFrom" IS NULL OR d."VisibleFrom" <= NOW())
      AND (d."ExpiresAt" IS NULL OR d."ExpiresAt" > NOW())
      AND d."Deleted" = false::boolean
      AND d."CreatedAt" >= NOW() - INTERVAL '12 months'
     AND EXISTS (
        SELECT 1
        FROM "DialogEndUserContext" dec 
        JOIN "DialogEndUserContextSystemLabel" sl ON dec."Id" = sl."DialogEndUserContextId"
        WHERE dec."DialogId" = d."Id"
           AND sl."SystemLabelId" = 1 
        ) ORDER BY "d"."CreatedAt" DESC, "d"."Id" DESC  LIMIT 101 
        ) d_inner
    ) AS filtered_dialogs
    JOIN "Dialog" d ON d."Id" = filtered_dialogs."Id"
) AS d
ORDER BY d."CreatedAt" DESC, d."Id" DESC
LIMIT 101