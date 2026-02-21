CREATE TABLE IF NOT EXISTS partyresource."BackfillShardState"
(
    "ShardId" integer NOT NULL,
    "ShardCount" integer NOT NULL,
    "LastDialogId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'::uuid,
    "Completed" boolean NOT NULL DEFAULT false,
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT now(),

    CONSTRAINT "PK_BackfillShardState" PRIMARY KEY ("ShardId"),
    CONSTRAINT "CK_BackfillShardState_ShardId_NonNegative" CHECK ("ShardId" >= 0),
    CONSTRAINT "CK_BackfillShardState_ShardCount_Positive" CHECK ("ShardCount" > 0),
    CONSTRAINT "CK_BackfillShardState_ShardRange" CHECK ("ShardId" < "ShardCount")
);

-- Seed with 16 shards once; modify manually before starting backfill if a different shard count is desired.
INSERT INTO partyresource."BackfillShardState" ("ShardId", "ShardCount")
SELECT gs, 16
FROM generate_series(0, 15) AS gs
WHERE NOT EXISTS (
    SELECT 1
    FROM partyresource."BackfillShardState"
);
