CREATE TABLE IF NOT EXISTS partyresource."BackfillState"
(
    "LastDialogId" uuid NULL,
    "Completed" boolean NOT NULL DEFAULT false,
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT now()
);

INSERT INTO partyresource."BackfillState" ("LastDialogId", "Completed", "UpdatedAt")
SELECT
    NULL,
    false,
    now()
WHERE NOT EXISTS (
    SELECT 1
    FROM partyresource."BackfillState"
);
