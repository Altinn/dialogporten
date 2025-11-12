-- Refreshes a single dialog's search vector by reusing the canonical aggregated document view.
CREATE OR REPLACE FUNCTION search."UpsertDialogSearchOne"(p_dialog_id uuid)
RETURNS void
LANGUAGE sql
AS $$
  INSERT INTO search."DialogSearch" ("DialogId","UpdatedAt","SearchVector")
  SELECT "DialogId", now(), COALESCE("Document",''::tsvector)
  FROM search."VDialogDocument"
  WHERE "DialogId" = p_dialog_id
  ON CONFLICT ("DialogId") DO UPDATE
  SET "UpdatedAt"    = EXCLUDED."UpdatedAt",
      "SearchVector" = EXCLUDED."SearchVector";
$$;
