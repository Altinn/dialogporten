-- Defines the DialogSearch rebuild work queue; UNLOGGED keeps throughput high since rows are transient.
DROP TABLE IF EXISTS search."DialogSearchRebuildQueue";
CREATE UNLOGGED TABLE search."DialogSearchRebuildQueue" (
  "DialogId"   uuid      PRIMARY KEY,
  "Status"     smallint  NOT NULL DEFAULT 0,  -- 0=pending, 1=processing, 2=done
  "UpdatedAt"  timestamptz NOT NULL DEFAULT now()
);

-- Simple status filter powers progress reporting queries.
DROP INDEX IF EXISTS "IX_DialogSearchRebuildQueue_Status";
CREATE INDEX "IX_DialogSearchRebuildQueue_Status"
  ON search."DialogSearchRebuildQueue" ("Status");

-- Supports the default claimer which orders by DialogId while filtering on Status.
DROP INDEX IF EXISTS "IX_DialogSearchRebuildQueue_StatusDialogId";
CREATE INDEX "IX_DialogSearchRebuildQueue_StatusDialogId"
  ON search."DialogSearchRebuildQueue" ("Status", "DialogId");

-- UpdatedAt index keeps failure dashboards snappy and allows coarse throughput calculations.
DROP INDEX IF EXISTS "IX_DialogSearchRebuildQueue_StatusUpdatedAt";
CREATE INDEX "IX_DialogSearchRebuildQueue_StatusUpdatedAt"
  ON search."DialogSearchRebuildQueue" ("Status", "UpdatedAt");
