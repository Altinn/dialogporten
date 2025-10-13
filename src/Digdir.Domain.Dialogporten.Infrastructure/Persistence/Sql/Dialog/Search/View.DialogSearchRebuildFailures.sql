-- Lists failed DialogSearch rebuild attempts so operators can act on the newest issues first.
CREATE OR REPLACE VIEW search."DialogSearchRebuildFailures" AS
SELECT "DialogId", "Attempts", "UpdatedAt", "LastError"
FROM search."DialogSearchRebuildQueue"
WHERE "Status" = 3
ORDER BY "UpdatedAt" DESC;
