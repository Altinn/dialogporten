CREATE OR REPLACE PROCEDURE partyresource.run_backfill_dialog_partyresource(
    p_batch_size integer DEFAULT 5000,
    p_notice_every integer DEFAULT 1,
    p_idle_sleep_seconds double precision DEFAULT 0.1
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_iteration bigint := 0;
    v_total_processed bigint := 0;
    v_total_inserted_parties bigint := 0;
    v_total_inserted_resources bigint := 0;
    v_total_inserted_pairs bigint := 0;
    v_started_at timestamp with time zone := clock_timestamp();
    v_elapsed interval;

    v_batch record;
BEGIN
    IF p_batch_size <= 0 THEN
        RAISE EXCEPTION 'p_batch_size must be > 0'
            USING ERRCODE = '22023';
    END IF;

    IF p_notice_every <= 0 THEN
        RAISE EXCEPTION 'p_notice_every must be > 0'
            USING ERRCODE = '22023';
    END IF;

    IF p_idle_sleep_seconds < 0 THEN
        RAISE EXCEPTION 'p_idle_sleep_seconds must be >= 0'
            USING ERRCODE = '22023';
    END IF;

    LOOP
        SELECT *
        INTO v_batch
        FROM partyresource.backfill_dialog_partyresource_batch(p_batch_size);

        IF NOT FOUND THEN
            RAISE EXCEPTION 'backfill_dialog_partyresource_batch returned no row'
                USING ERRCODE = '55000';
        END IF;

        v_iteration := v_iteration + 1;
        v_total_processed := v_total_processed + v_batch."ProcessedRows";
        v_total_inserted_parties := v_total_inserted_parties + v_batch."InsertedParties";
        v_total_inserted_resources := v_total_inserted_resources + v_batch."InsertedResources";
        v_total_inserted_pairs := v_total_inserted_pairs + v_batch."InsertedPairs";

        IF v_iteration = 1 OR MOD(v_iteration, p_notice_every) = 0 OR v_batch."Completed" THEN
            v_elapsed := clock_timestamp() - v_started_at;

            RAISE NOTICE
                '[partyresource-backfill] iter=% phase=% batch_processed=% total_processed=% batch_inserted_pairs=% total_inserted_pairs=% last_dialog_id=% select_batch_ms=% parse_ms=% insert_party_ms=% insert_resource_ms=% insert_pair_ms=% total_ms=% elapsed=%',
                v_iteration,
                v_batch."Phase",
                v_batch."ProcessedRows",
                v_total_processed,
                v_batch."InsertedPairs",
                v_total_inserted_pairs,
                v_batch."LastDialogId",
                v_batch."SelectBatchMs",
                v_batch."ParseMs",
                v_batch."InsertPartyMs",
                v_batch."InsertResourceMs",
                v_batch."InsertPairMs",
                v_batch."TotalMs",
                v_elapsed;
        END IF;

        IF v_batch."Completed" THEN
            EXIT;
        END IF;

        IF v_batch."ProcessedRows" = 0 AND p_idle_sleep_seconds > 0 THEN
            PERFORM pg_sleep(p_idle_sleep_seconds);
        END IF;

        COMMIT;
    END LOOP;

    COMMIT;

    v_elapsed := clock_timestamp() - v_started_at;

    RAISE NOTICE
        '[partyresource-backfill] completed total_processed=% total_inserted_parties=% total_inserted_resources=% total_inserted_pairs=% elapsed=%',
        v_total_processed,
        v_total_inserted_parties,
        v_total_inserted_resources,
        v_total_inserted_pairs,
        v_elapsed;
END;
$$;
