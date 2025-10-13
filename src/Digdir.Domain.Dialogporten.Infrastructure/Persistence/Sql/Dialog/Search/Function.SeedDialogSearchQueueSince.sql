-- Enqueues dialogs touched after a given timestamp to support incremental rebuild workflows.
CREATE OR REPLACE FUNCTION search."SeedDialogSearchQueueSince"(since timestamptz, reset_matching boolean DEFAULT false)
RETURNS int
LANGUAGE plpgsql AS $$
DECLARE n int;
BEGIN
  IF reset_matching THEN
    -- Clearing stale attempts allows partial runs to resume cleanly when re-seeding the same window.
    UPDATE search."DialogSearchRebuildQueue" q
       SET "Status" = 0, "Attempts" = 0, "LastError" = NULL, "UpdatedAt" = now()
      WHERE q."DialogId" IN (SELECT d."Id" FROM "Dialog" d WHERE d."UpdatedAt" >= since);
  END IF;

  INSERT INTO search."DialogSearchRebuildQueue" ("DialogId")
  SELECT d."Id"
  FROM "Dialog" d
  WHERE d."UpdatedAt" >= since
  ON CONFLICT ("DialogId") DO NOTHING;

  GET DIAGNOSTICS n = ROW_COUNT;
  RETURN n;
END; $$;
