using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDialogSearchReindexInfra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
           /*
            * DialogSearch reindex infrastructure
            *
            * Purpose:
            *   Establishes a complete and self-contained subsystem for rebuilding and maintaining
            *   the full-text search index for dialogs in PostgreSQL.
            *
            * Structure:
            *   1) Schema:
            *        Ensures the dedicated schema "search" exists.
            *
            *   2) Views:
            *        a) v_dialog_content — canonical, unified view of all plain-text dialog sources
            *           (content, transmissions, activities, attachments).
            *        b) v_dialog_document — aggregates and weights language-aware tsvectors per dialog.
            *        These encapsulate all content extraction and weighting logic, shared by workers
            *        and single-dialog upserts.
            *
            *   3) Tables:
            *        a) dialogsearch_rebuild_queue (UNLOGGED) — work queue with status, attempts, and errors.
            *        b) dialogsearch_rebuild_metrics (UNLOGGED) — per-batch metrics for throughput and latency.
            *        UNLOGGED tables are used for speed; the data is ephemeral and can be safely rebuilt.
            *
            *   4) Operational views:
            *        Views exposing rebuild progress, failures, performance rates, and ETA estimates,
            *        intended for observability and monitoring.
            *
            *   5) Seeders:
            *        Helper functions to enqueue dialogs for reindexing, either all, since a timestamp,
            *        or only those that appear stale.
            *
            *   6) Upsert helper:
            *        A function for upserting a single dialog’s search vector, using the canonical views.
            *
            *   7) Worker functions:
            *        Functions responsible for performing concurrent reindexing in batches:
            *          - Two small “claimer” functions define how work is claimed (standard or stale-first).
            *          - One generic orchestrator function handles timing, metrics, upsert logic, and
            *            error handling, calling the appropriate claimer.
            *
            */

            // 1) Schema
            const string schemaSql = """
                CREATE SCHEMA IF NOT EXISTS search;
                """;

            // 2a) Canonical content view
            const string viewContentSql = """
                CREATE OR REPLACE VIEW search.v_dialog_content AS
                SELECT dc."DialogId" AS "DialogId",
                       CASE dc."TypeId" WHEN 1 THEN 'B' ELSE 'D' END AS "Weight",
                       l."LanguageCode" AS "LanguageCode",
                       l."Value"        AS "Value"
                FROM "DialogContent" dc
                JOIN "LocalizationSet" dcls ON dc."Id" = dcls."DialogContentId"
                JOIN "Localization"   l     ON dcls."Id" = l."LocalizationSetId"
                WHERE dc."MediaType" = 'text/plain'

                UNION ALL
                SELECT dt."DialogId", 'D', l."LanguageCode", l."Value"
                FROM "DialogTransmission" dt
                JOIN "DialogTransmissionContent" dtc ON dt."Id" = dtc."TransmissionId"
                JOIN "LocalizationSet" dcls          ON dtc."Id" = dcls."TransmissionContentId"
                JOIN "Localization"   l              ON dcls."Id" = l."LocalizationSetId"
                WHERE dtc."MediaType" = 'text/plain'

                UNION ALL
                SELECT da."DialogId", 'D', l."LanguageCode", l."Value"
                FROM "DialogActivity" da
                JOIN "LocalizationSet" dcls ON da."Id" = dcls."ActivityId"
                JOIN "Localization"   l     ON l."LocalizationSetId" = dcls."Id"

                UNION ALL
                -- Attachment description (dialog-linked)
                SELECT a."DialogId", 'D', l."LanguageCode", l."Value"
                FROM "Attachment" a
                JOIN "LocalizationSet" dcls ON dcls."AttachmentId" = a."Id"
                JOIN "Localization"   l     ON l."LocalizationSetId" = dcls."Id"

                UNION ALL
                -- Attachment description (transmission-linked)
                SELECT dt."DialogId", 'D', l."LanguageCode", l."Value"
                FROM "DialogTransmission" dt
                JOIN "Attachment" a         ON a."TransmissionId" = dt."Id"
                JOIN "LocalizationSet" dcls ON dcls."AttachmentId" = a."Id"
                JOIN "Localization"   l     ON l."LocalizationSetId" = dcls."Id";
                """;

            // 2b) Aggregated per-dialog tsvector document
            const string viewDocumentSql = """
                CREATE OR REPLACE VIEW search.v_dialog_document AS
                SELECT d."Id" AS "DialogId",
                       (
                         SELECT
                           string_agg(
                             setweight(
                               to_tsvector(COALESCE(isomap."TsConfigName", 'simple')::regconfig, c."Value"),
                               c."Weight"::"char"
                             )::text,
                             ' '
                           )::tsvector
                         FROM search.v_dialog_content c
                         LEFT JOIN search."Iso639TsVectorMap" isomap
                           ON c."LanguageCode" = isomap."IsoCode"
                         WHERE c."DialogId" = d."Id"
                       ) AS "Document"
                FROM "Dialog" d;
                """;

            // 3) Core tables + indexes
            const string tablesSql = """
                 CREATE UNLOGGED TABLE IF NOT EXISTS search.dialogsearch_rebuild_queue (
                   dialog_id   uuid PRIMARY KEY,
                   status      smallint NOT NULL DEFAULT 0,  -- 0=pending, 1=processing, 2=done, 3=failed
                   attempts    integer  NOT NULL DEFAULT 0,
                   last_error  text,
                   updated_at  timestamptz NOT NULL DEFAULT now()
                 );

                 CREATE INDEX IF NOT EXISTS ix_ds_rebuild_queue_status
                   ON search.dialogsearch_rebuild_queue (status);

                 -- This yields faster standard claiming (status=0 ORDER BY dialog_id)
                 CREATE INDEX IF NOT EXISTS ix_ds_rebuild_queue_status_id
                   ON search.dialogsearch_rebuild_queue (status, dialog_id);

                 CREATE UNLOGGED TABLE IF NOT EXISTS search.dialogsearch_rebuild_metrics (
                   ts              timestamptz NOT NULL DEFAULT clock_timestamp(),
                   worker          text        NOT NULL DEFAULT current_setting('application_name', true),
                   pid             int         NOT NULL DEFAULT pg_backend_pid(),
                   batch_size      int         NOT NULL,
                   work_mem_bytes  bigint      NOT NULL,
                   processed       int         NOT NULL,
                   duration_ms     numeric     NOT NULL,
                   dialogs_per_sec numeric     NOT NULL,
                   note            text
                 );

                 CREATE INDEX IF NOT EXISTS ix_ds_metrics_ts     ON search.dialogsearch_rebuild_metrics (ts);
                 CREATE INDEX IF NOT EXISTS ix_ds_metrics_worker ON search.dialogsearch_rebuild_metrics (worker);
                 """;


            // 4) Observability views (progress/failures/rates/eta)
            const string progressViewsSql = """
                CREATE OR REPLACE VIEW search.dialogsearch_rebuild_progress AS
                SELECT
                  count(*)                                         AS total,
                  count(*) FILTER (WHERE status=0)                 AS pending,
                  count(*) FILTER (WHERE status=1)                 AS processing,
                  count(*) FILTER (WHERE status=2)                 AS done,
                  count(*) FILTER (WHERE status=3)                 AS failed,
                  (count(*) FILTER (WHERE status=2))::numeric
                    / NULLIF(count(*),0)                           AS done_ratio
                FROM search.dialogsearch_rebuild_queue;

                CREATE OR REPLACE VIEW search.dialogsearch_rebuild_failures AS
                SELECT dialog_id, attempts, updated_at, last_error
                FROM search.dialogsearch_rebuild_queue
                WHERE status = 3
                ORDER BY updated_at DESC;

                CREATE OR REPLACE VIEW search.dialogsearch_rates AS
                SELECT
                  now() AS asof,
                  coalesce(avg(dialogs_per_sec) FILTER (WHERE ts > now() - interval '1 minute'), 0)   AS dps_1m,
                  coalesce(avg(dialogs_per_sec) FILTER (WHERE ts > now() - interval '5 minutes'), 0)  AS dps_5m,
                  coalesce(avg(dialogs_per_sec) FILTER (WHERE ts > now() - interval '15 minutes'), 0) AS dps_15m,
                  coalesce(sum(processed) FILTER (WHERE ts > now() - interval '1 minute') / 60.0, 0)  AS realized_dps_1m,
                  coalesce(sum(processed) FILTER (WHERE ts > now() - interval '5 minutes') / 300.0, 0) AS realized_dps_5m,
                  coalesce(sum(processed) FILTER (WHERE ts > now() - interval '15 minutes') / 900.0, 0) AS realized_dps_15m
                FROM search.dialogsearch_rebuild_metrics;

                CREATE OR REPLACE VIEW search.dialogsearch_eta AS
                WITH p AS (
                  SELECT
                    count(*)                                         AS total,
                    count(*) FILTER (WHERE status=2)                 AS done,
                    count(*) FILTER (WHERE status=0)                 AS pending
                  FROM search.dialogsearch_rebuild_queue
                ),
                r AS (
                  SELECT coalesce(avg(dialogs_per_sec) FILTER (WHERE ts > now() - interval '5 minutes'), 0.0) AS dps
                  FROM search.dialogsearch_rebuild_metrics
                )
                SELECT p.total, p.done, p.pending, r.dps,
                       CASE WHEN r.dps > 0 THEN (p.pending / r.dps) ELSE NULL END AS seconds_remaining_estimate
                FROM p, r;
                """;

            // 5) Seeders (full, since, stale)
            const string seedersSql = """
                CREATE OR REPLACE FUNCTION search.seed_dialogsearch_queue_full(reset_existing boolean DEFAULT false)
                RETURNS int
                LANGUAGE plpgsql AS $$
                DECLARE n int;
                BEGIN
                  IF reset_existing THEN
                    UPDATE search.dialogsearch_rebuild_queue
                       SET status=0, attempts=0, last_error=NULL, updated_at=now();
                  END IF;

                  INSERT INTO search.dialogsearch_rebuild_queue (dialog_id)
                  SELECT d."Id" FROM "Dialog" d
                  ON CONFLICT (dialog_id) DO NOTHING;

                  GET DIAGNOSTICS n = ROW_COUNT;
                  RETURN n;
                END; $$;

                CREATE OR REPLACE FUNCTION search.seed_dialogsearch_queue_since(since timestamptz, reset_matching boolean DEFAULT false)
                RETURNS int
                LANGUAGE plpgsql AS $$
                DECLARE n int;
                BEGIN
                  IF reset_matching THEN
                    UPDATE search.dialogsearch_rebuild_queue q
                       SET status=0, attempts=0, last_error=NULL, updated_at=now()
                      WHERE q.dialog_id IN (SELECT d."Id" FROM "Dialog" d WHERE d."UpdatedAt" >= since);
                  END IF;

                  INSERT INTO search.dialogsearch_rebuild_queue (dialog_id)
                  SELECT d."Id"
                  FROM "Dialog" d
                  WHERE d."UpdatedAt" >= since
                  ON CONFLICT (dialog_id) DO NOTHING;

                  GET DIAGNOSTICS n = ROW_COUNT;
                  RETURN n;
                END; $$;

                CREATE OR REPLACE FUNCTION search.seed_dialogsearch_queue_stale(reset_matching boolean DEFAULT false)
                RETURNS int
                LANGUAGE plpgsql AS $$
                DECLARE n int;
                BEGIN
                  IF reset_matching THEN
                    UPDATE search.dialogsearch_rebuild_queue q
                       SET status=0, attempts=0, last_error=NULL, updated_at=now()
                      WHERE q.dialog_id IN (
                        SELECT d."Id"
                        FROM "Dialog" d
                        LEFT JOIN search."DialogSearch" ds ON ds."DialogId" = d."Id"
                        WHERE ds."DialogId" IS NULL
                           OR d."UpdatedAt" > ds."UpdatedAt"
                      );
                  END IF;

                  INSERT INTO search.dialogsearch_rebuild_queue (dialog_id)
                  SELECT d."Id"
                  FROM "Dialog" d
                  LEFT JOIN search."DialogSearch" ds ON ds."DialogId" = d."Id"
                  WHERE ds."DialogId" IS NULL
                     OR d."UpdatedAt" > ds."UpdatedAt"
                  ON CONFLICT (dialog_id) DO NOTHING;

                  GET DIAGNOSTICS n = ROW_COUNT;
                  RETURN n;
                END; $$;
                """;

            // 6) Helper for single-dialog upsert
            const string upsertOneSql = """
                CREATE OR REPLACE FUNCTION search.upsert_dialogsearch_one(p_dialog_id uuid)
                RETURNS void
                LANGUAGE sql
                AS $$
                  INSERT INTO search."DialogSearch" ("DialogId","UpdatedAt","SearchVector")
                  SELECT "DialogId", now(), COALESCE("Document",'')
                  FROM search.v_dialog_document
                  WHERE "DialogId" = p_dialog_id
                  ON CONFLICT ("DialogId") DO UPDATE
                  SET "UpdatedAt"   = EXCLUDED."UpdatedAt",
                      "SearchVector"= EXCLUDED."SearchVector";
                $$;
                """;

            // 7a) Worker functions – claimers (use alias in FOR UPDATE OF)
            const string claimersSql = """
               CREATE OR REPLACE FUNCTION search.claim_dialogs_standard(batch_size int)
               RETURNS uuid[] LANGUAGE sql AS $$
                 WITH to_claim AS (
                   SELECT q.dialog_id
                   FROM search.dialogsearch_rebuild_queue q
                   WHERE q.status = 0
                   ORDER BY q.dialog_id
                   FOR UPDATE OF q SKIP LOCKED
                   LIMIT batch_size
                 ),
                 mark_processing AS (
                   UPDATE search.dialogsearch_rebuild_queue q
                      SET status = 1, attempts = q.attempts + 1, updated_at = now()
                    WHERE q.dialog_id IN (SELECT dialog_id FROM to_claim)
                   RETURNING q.dialog_id
                 )
                 SELECT coalesce(array_agg(dialog_id), '{}') FROM mark_processing;
               $$;

               CREATE OR REPLACE FUNCTION search.claim_dialogs_stale_first(batch_size int)
               RETURNS uuid[] LANGUAGE sql AS $$
                 WITH to_claim AS (
                   SELECT q.dialog_id
                   FROM search.dialogsearch_rebuild_queue q
                   JOIN "Dialog" d ON d."Id" = q.dialog_id
                   LEFT JOIN search."DialogSearch" ds ON ds."DialogId" = q.dialog_id
                   WHERE q.status = 0
                   ORDER BY (ds."DialogId" IS NULL) DESC,
                            (CASE WHEN ds."DialogId" IS NULL THEN interval '1000 years'
                                  ELSE (d."UpdatedAt" - ds."UpdatedAt") END) DESC NULLS LAST,
                            q.dialog_id
                   FOR UPDATE OF q SKIP LOCKED
                   LIMIT batch_size
                 ),
                 mark_processing AS (
                   UPDATE search.dialogsearch_rebuild_queue q
                      SET status = 1, attempts = q.attempts + 1, updated_at = now()
                    WHERE q.dialog_id IN (SELECT dialog_id FROM to_claim)
                   RETURNING q.dialog_id
                 )
                 SELECT coalesce(array_agg(dialog_id), '{}') FROM mark_processing;
               $$;
               """;


            // 7b) Workers – generic orchestrator
            const string workerGenericSql = """
                CREATE OR REPLACE FUNCTION search.rebuild_dialogsearch_once(
                    strategy       text   DEFAULT 'standard',       -- 'standard'|'stale_first'
                    batch_size     int    DEFAULT 1000,
                    work_mem_bytes bigint DEFAULT 268435456
                ) RETURNS int
                LANGUAGE plpgsql AS $$
                DECLARE
                  claimed_ids uuid[];
                  processed   int := 0;
                  t0          timestamptz;
                  t1          timestamptz;
                  dur_s       numeric;
                BEGIN
                  PERFORM set_config('work_mem', work_mem_bytes::text, true);
                  t0 := clock_timestamp();

                  IF strategy = 'stale_first' THEN
                    SELECT search.claim_dialogs_stale_first(batch_size) INTO claimed_ids;
                  ELSE
                    SELECT search.claim_dialogs_standard(batch_size) INTO claimed_ids;
                  END IF;

                  IF array_length(claimed_ids, 1) IS NULL THEN
                    t1 := clock_timestamp();
                    dur_s := GREATEST(EXTRACT(EPOCH FROM (t1 - t0)), 0.000001);
                    INSERT INTO search.dialogsearch_rebuild_metrics (batch_size, work_mem_bytes, processed, duration_ms, dialogs_per_sec, note)
                    VALUES (batch_size, work_mem_bytes, 0, dur_s*1000.0, 0, 'empty-batch');
                    RETURN 0;
                  END IF;

                  WITH upsert AS (
                    INSERT INTO search."DialogSearch" ("DialogId","UpdatedAt","SearchVector")
                    SELECT "DialogId", now(), COALESCE("Document",'')
                    FROM search.v_dialog_document
                    WHERE "DialogId" = ANY (claimed_ids)
                    ON CONFLICT ("DialogId") DO UPDATE
                    SET "UpdatedAt"   = EXCLUDED."UpdatedAt",
                        "SearchVector"= EXCLUDED."SearchVector"
                    RETURNING "DialogId"
                  )
                  UPDATE search.dialogsearch_rebuild_queue q
                     SET status = 2, updated_at = now()
                   WHERE q.dialog_id IN (SELECT "DialogId" FROM upsert);

                  GET DIAGNOSTICS processed = ROW_COUNT;

                  t1 := clock_timestamp();
                  dur_s := GREATEST(EXTRACT(EPOCH FROM (t1 - t0)), 0.000001);
                  INSERT INTO search.dialogsearch_rebuild_metrics (batch_size, work_mem_bytes, processed, duration_ms, dialogs_per_sec)
                  VALUES (batch_size, work_mem_bytes, processed, dur_s*1000.0, processed/dur_s);

                  RETURN processed;

                EXCEPTION WHEN OTHERS THEN
                  IF claimed_ids IS NOT NULL AND array_length(claimed_ids,1) IS NOT NULL THEN
                    UPDATE search.dialogsearch_rebuild_queue
                       SET status = 0, last_error = SQLERRM, updated_at = now()
                     WHERE dialog_id = ANY (claimed_ids)
                       AND status = 1;
                  END IF;
                  t1 := clock_timestamp();
                  dur_s := GREATEST(EXTRACT(EPOCH FROM (t1 - t0)), 0.000001);
                  INSERT INTO search.dialogsearch_rebuild_metrics (batch_size, work_mem_bytes, processed, duration_ms, dialogs_per_sec, note)
                  VALUES (batch_size, work_mem_bytes, 0, dur_s*1000.0, 0, 'exception');
                  RAISE;
                END;
                $$;
                """;


            // Execute in dependency-safe order
            migrationBuilder.Sql(schemaSql);
            migrationBuilder.Sql(viewContentSql);
            migrationBuilder.Sql(viewDocumentSql);
            migrationBuilder.Sql(tablesSql);
            migrationBuilder.Sql(progressViewsSql);
            migrationBuilder.Sql(seedersSql);
            migrationBuilder.Sql(upsertOneSql);
            migrationBuilder.Sql(claimersSql);
            migrationBuilder.Sql(workerGenericSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            const string downSql = """
                -- Drop generic worker first
                DROP FUNCTION IF EXISTS search.rebuild_dialogsearch_once(text, int, bigint);

                -- Drop claimers
                DROP FUNCTION IF EXISTS search.claim_dialogs_stale_first(int);
                DROP FUNCTION IF EXISTS search.claim_dialogs_standard(int);

                -- Upsert helper
                DROP FUNCTION IF EXISTS search.upsert_dialogsearch_one(uuid);

                -- Ops views
                DROP VIEW IF EXISTS search.dialogsearch_eta;
                DROP VIEW IF EXISTS search.dialogsearch_rates;
                DROP VIEW IF EXISTS search.dialogsearch_rebuild_failures;
                DROP VIEW IF EXISTS search.dialogsearch_rebuild_progress;

                -- Core views
                DROP VIEW IF EXISTS search.v_dialog_document;
                DROP VIEW IF EXISTS search.v_dialog_content;

                -- Seeders
                DROP FUNCTION IF EXISTS search.seed_dialogsearch_queue_stale(boolean);
                DROP FUNCTION IF EXISTS search.seed_dialogsearch_queue_since(timestamptz, boolean);
                DROP FUNCTION IF EXISTS search.seed_dialogsearch_queue_full(boolean);

                -- Tables
                DROP TABLE IF EXISTS search.dialogsearch_rebuild_metrics;
                DROP TABLE IF EXISTS search.dialogsearch_rebuild_queue;
                """;

            migrationBuilder.Sql(downSql);
        }
    }
}
