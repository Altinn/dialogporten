-- Summarises queue state for dashboards and periodic progress logs.
CREATE OR REPLACE VIEW search."DialogSearchRebuildProgress" AS
SELECT
  count(*)                                         AS "Total",
  count(*) FILTER (WHERE "Status" = 0)             AS "Pending",
  count(*) FILTER (WHERE "Status" = 1)             AS "Processing",
  count(*) FILTER (WHERE "Status" = 2)             AS "Done",
  (count(*) FILTER (WHERE "Status" = 2))::numeric
    / NULLIF(count(*), 0)                          AS "DoneRatio" -- Handy for log formatting and monitoring thresholds.
FROM search."DialogSearchRebuildQueue";
