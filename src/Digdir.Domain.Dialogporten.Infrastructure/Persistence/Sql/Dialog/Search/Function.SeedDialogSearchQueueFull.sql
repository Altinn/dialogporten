-- Enqueues every dialog for rebuilding the search index; handy when bootstrapping a new environment.
CREATE OR REPLACE FUNCTION search."SeedDialogSearchQueueFull"(reset_existing boolean DEFAULT false)
RETURNS int
LANGUAGE plpgsql AS $$
DECLARE n int;
BEGIN
  IF reset_existing THEN
    -- Resetting keeps existing queue entries but clears transient error state.
    UPDATE search."DialogSearchRebuildQueue"
       SET "Status" = 0, "Attempts" = 0, "LastError" = NULL, "UpdatedAt" = now();
  END IF;

  INSERT INTO search."DialogSearchRebuildQueue" ("DialogId")
  SELECT d."Id" FROM "Dialog" d
  ON CONFLICT ("DialogId") DO NOTHING;

  GET DIAGNOSTICS n = ROW_COUNT;
  RETURN n;
END; $$;
