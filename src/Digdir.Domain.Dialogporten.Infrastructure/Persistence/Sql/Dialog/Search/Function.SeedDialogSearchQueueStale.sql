-- Enqueues only dialogs whose search vectors are missing or older than the source dialog data.
CREATE OR REPLACE FUNCTION search."SeedDialogSearchQueueStale"(reset_matching boolean DEFAULT false)
RETURNS int
LANGUAGE plpgsql AS $$
DECLARE n int;
BEGIN
  IF reset_matching THEN
    -- Reset only the affected dialogs so retries pick up immediately without clearing the entire queue.
    UPDATE search."DialogSearchRebuildQueue" q
       SET "Status" = 0, "Attempts" = 0, "LastError" = NULL, "UpdatedAt" = now()
      WHERE q."DialogId" IN (
        SELECT d."Id"
        FROM "Dialog" d
        LEFT JOIN search."DialogSearch" ds ON ds."DialogId" = d."Id"
        WHERE ds."DialogId" IS NULL
           OR d."UpdatedAt" > ds."UpdatedAt"
      );
  END IF;

  INSERT INTO search."DialogSearchRebuildQueue" ("DialogId")
  SELECT d."Id"
  FROM "Dialog" d
  LEFT JOIN search."DialogSearch" ds ON ds."DialogId" = d."Id"
  WHERE ds."DialogId" IS NULL
     OR d."UpdatedAt" > ds."UpdatedAt"
  ON CONFLICT ("DialogId") DO NOTHING;

  GET DIAGNOSTICS n = ROW_COUNT;
  RETURN n;
END; $$;
