-- Runs a single DialogSearch rebuild batch, leaving caller to loop and orchestrate back-off.
CREATE OR REPLACE FUNCTION search."RebuildDialogSearchOnce"(
    strategy       text   DEFAULT 'standard',       -- 'standard'|'stale_first'
    batch_size     int    DEFAULT 1000,
    work_mem_bytes bigint DEFAULT 268435456
) RETURNS int
LANGUAGE plpgsql AS $$
DECLARE
  claimed_ids uuid[];
  processed   int := 0;
BEGIN
  PERFORM set_config('work_mem', work_mem_bytes::text || 'B', true); -- Tight loops benefit from elevated work_mem.

  IF strategy = 'stale_first' THEN
    SELECT search."ClaimDialogsStaleFirst"(batch_size) INTO claimed_ids;
  ELSE
    SELECT search."ClaimDialogsStandard"(batch_size) INTO claimed_ids;
  END IF;

  IF array_length(claimed_ids, 1) IS NULL THEN
    RETURN 0;
  END IF;

  WITH upsert AS (
    INSERT INTO search."DialogSearch" ("DialogId","UpdatedAt","Party","SearchVector")
    SELECT "DialogId", now(), "Party", COALESCE("Document",''::tsvector)
    FROM search."VDialogDocument"
    WHERE "DialogId" = ANY (claimed_ids)
    ON CONFLICT ("DialogId") DO UPDATE
    SET "UpdatedAt"   = EXCLUDED."UpdatedAt",
        "Party"       = EXCLUDED."Party",
        "SearchVector"= EXCLUDED."SearchVector"
    RETURNING "DialogId"
  )
  UPDATE search."DialogSearchRebuildQueue" q
     SET "Status" = 2, "UpdatedAt" = now()
   WHERE q."DialogId" IN (SELECT "DialogId" FROM upsert);

  GET DIAGNOSTICS processed = ROW_COUNT;

  RETURN processed;

END;
$$;
