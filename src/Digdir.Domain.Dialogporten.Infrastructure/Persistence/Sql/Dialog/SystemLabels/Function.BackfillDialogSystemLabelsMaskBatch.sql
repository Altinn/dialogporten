CREATE OR REPLACE FUNCTION public.backfill_dialog_system_labels_mask_batch(
    p_last_dialog_id uuid DEFAULT NULL,
    p_batch_size integer DEFAULT 50000
)
RETURNS TABLE
(
    "ProcessedRows" integer,
    "LastDialogId" uuid,
    "Completed" boolean
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_processed_rows integer := 0;
    v_next_last_dialog_id uuid;
BEGIN
    IF p_batch_size <= 0 THEN
        RAISE EXCEPTION 'p_batch_size must be > 0'
            USING ERRCODE = '22023';
    END IF;

    WITH candidate_dialogs AS (
        SELECT dec."DialogId" AS dialog_id
        FROM public."DialogEndUserContext" dec
        JOIN public."DialogEndUserContextSystemLabel" sl ON sl."DialogEndUserContextId" = dec."Id"
        WHERE dec."DialogId" > COALESCE(p_last_dialog_id, '00000000-0000-0000-0000-000000000000'::uuid)
        GROUP BY dec."DialogId"
        HAVING COUNT(*) <> 1
            OR MIN(sl."SystemLabelId") <> 1
        ORDER BY dec."DialogId"
        LIMIT p_batch_size
    ),
    computed_masks AS (
        SELECT cd.dialog_id,
               COALESCE(SUM(DISTINCT (1 << (sl."SystemLabelId" - 1))), 0)::smallint AS computed_system_labels_mask
        FROM candidate_dialogs cd
        JOIN public."DialogEndUserContext" dec ON dec."DialogId" = cd.dialog_id
        JOIN public."DialogEndUserContextSystemLabel" sl ON sl."DialogEndUserContextId" = dec."Id"
        GROUP BY cd.dialog_id
    ),
    updated_dialogs AS (
        UPDATE public."Dialog" d
        SET "SystemLabelsMask" = cm.computed_system_labels_mask
        FROM computed_masks cm
        WHERE d."Id" = cm.dialog_id
          AND d."SystemLabelsMask" IS DISTINCT FROM cm.computed_system_labels_mask
        RETURNING cm.dialog_id
    )
    SELECT COALESCE((SELECT COUNT(*) FROM candidate_dialogs), 0),
           (SELECT dialog_id FROM candidate_dialogs ORDER BY dialog_id DESC LIMIT 1)
    INTO v_processed_rows, v_next_last_dialog_id;

    RETURN QUERY
    SELECT v_processed_rows,
           v_next_last_dialog_id,
           v_processed_rows < p_batch_size;
END;
$$;
