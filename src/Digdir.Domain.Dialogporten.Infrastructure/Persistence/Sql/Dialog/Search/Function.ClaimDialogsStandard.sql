-- Claims pending dialogs in FIFO order so parallel workers share the queue deterministically.
CREATE OR REPLACE FUNCTION search."ClaimDialogsStandard"(batch_size int)
RETURNS uuid[] LANGUAGE sql AS $$
  WITH to_claim AS (
    SELECT q."DialogId"
    FROM search."DialogSearchRebuildQueue" q
    WHERE q."Status" = 0
    ORDER BY q."DialogId" -- Stable order avoids the same worker repeatedly claiming the tail.
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
