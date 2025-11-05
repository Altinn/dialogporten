-- Prefers dialogs missing or with stale search vectors so catch-up jobs close the freshness gap first.
CREATE OR REPLACE FUNCTION search."ClaimDialogsStaleFirst"(batch_size int)
RETURNS uuid[] LANGUAGE sql AS $$
  WITH to_claim AS (
    SELECT q."DialogId"
    FROM search."DialogSearchRebuildQueue" q
    JOIN "Dialog" d ON d."Id" = q."DialogId"
    LEFT JOIN search."DialogSearch" ds ON ds."DialogId" = q."DialogId"
    WHERE q."Status" = 0
    ORDER BY (ds."DialogId" IS NULL) DESC, -- Brand-new dialogs always bubble to the front.
             (CASE WHEN ds."DialogId" IS NULL THEN interval '1000 years'
                   ELSE (d."UpdatedAt" - ds."UpdatedAt") END) DESC NULLS LAST, -- Older deltas first.
             q."DialogId"
    FOR UPDATE OF q SKIP LOCKED
    LIMIT batch_size
  ),
  mark_processing AS (
    UPDATE search."DialogSearchRebuildQueue" q
       SET "Status" = 1, "UpdatedAt" = now()
     WHERE q."DialogId" IN (SELECT "DialogId" FROM to_claim)
    RETURNING q."DialogId"
  )
  SELECT coalesce(array_agg("DialogId"), '{}') FROM mark_processing;
$$;
