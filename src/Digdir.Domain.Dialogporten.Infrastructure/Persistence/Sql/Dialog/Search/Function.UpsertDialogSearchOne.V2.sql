-- Refreshes a single dialog's search vector by reusing the canonical aggregated document view.
CREATE OR REPLACE FUNCTION search."UpsertDialogSearchOne"(p_dialog_id uuid)
RETURNS void
LANGUAGE sql
AS $$
  INSERT INTO search."DialogSearch" ("DialogId","UpdatedAt","Party","SearchVector")
  SELECT "DialogId", now(), "Party", COALESCE("Document",''::tsvector)
  FROM search."VDialogDocument"
  WHERE "DialogId" = p_dialog_id
  ON CONFLICT ("DialogId") DO UPDATE
  SET "UpdatedAt"    = EXCLUDED."UpdatedAt",
      "Party"        = EXCLUDED."Party",
      "SearchVector" = EXCLUDED."SearchVector";
$$;
