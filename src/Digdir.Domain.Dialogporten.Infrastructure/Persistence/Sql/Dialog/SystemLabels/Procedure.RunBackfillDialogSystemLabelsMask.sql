CREATE OR REPLACE PROCEDURE public.run_backfill_dialog_system_labels_mask(
    p_batch_size integer DEFAULT 50000,
    p_start_dialog_id uuid DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_last_dialog_id uuid := p_start_dialog_id;
    v_batch record;
    v_total_processed bigint := 0;
    v_batch_number integer := 0;
    v_started_at timestamptz := clock_timestamp();
    v_batch_started_at timestamptz;
BEGIN
    IF p_batch_size <= 0 THEN
        RAISE EXCEPTION 'p_batch_size must be > 0'
            USING ERRCODE = '22023';
    END IF;

    RAISE NOTICE 'Starting dialog system labels mask backfill with batch size %, start dialog id %.', p_batch_size, v_last_dialog_id;

    LOOP
        v_batch_started_at := clock_timestamp();

        SELECT *
        INTO v_batch
        FROM public.backfill_dialog_system_labels_mask_batch(v_last_dialog_id, p_batch_size);

        IF NOT FOUND OR v_batch."ProcessedRows" = 0 THEN
            EXIT;
        END IF;

        v_batch_number := v_batch_number + 1;
        v_total_processed := v_total_processed + v_batch."ProcessedRows";
        v_last_dialog_id := v_batch."LastDialogId";

        RAISE NOTICE 'Backfill batch % processed % dialogs, total %, last dialog id %, batch duration %.',
            v_batch_number,
            v_batch."ProcessedRows",
            v_total_processed,
            v_last_dialog_id,
            clock_timestamp() - v_batch_started_at;

        COMMIT;

        EXIT WHEN v_batch."Completed";
    END LOOP;

    RAISE NOTICE 'Completed dialog system labels mask backfill. Total dialogs processed: %, total duration %.',
        v_total_processed,
        clock_timestamp() - v_started_at;
END;
$$;
