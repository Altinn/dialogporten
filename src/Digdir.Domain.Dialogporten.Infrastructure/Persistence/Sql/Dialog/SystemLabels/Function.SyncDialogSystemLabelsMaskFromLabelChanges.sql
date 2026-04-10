CREATE OR REPLACE FUNCTION public.sync_dialog_system_labels_mask_from_label_changes(p_end_user_context_ids uuid[])
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
    v_dialog_ids uuid[];
BEGIN
    SELECT COALESCE(array_agg(DISTINCT dec."DialogId"), ARRAY[]::uuid[])
    INTO v_dialog_ids
    FROM public."DialogEndUserContext" dec
    WHERE dec."Id" = ANY(p_end_user_context_ids)
      AND dec."DialogId" IS NOT NULL;

    IF cardinality(v_dialog_ids) = 0 THEN
        RETURN;
    END IF;

    PERFORM public.recompute_dialog_system_labels_mask(v_dialog_ids);
END;
$$;

CREATE OR REPLACE FUNCTION public.sync_dialog_system_labels_mask_from_inserted_label_rows()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    v_end_user_context_ids uuid[];
BEGIN
    SELECT COALESCE(array_agg(DISTINCT nr."DialogEndUserContextId"), ARRAY[]::uuid[])
    INTO v_end_user_context_ids
    FROM new_rows nr;

    IF cardinality(v_end_user_context_ids) > 0 THEN
        PERFORM public.sync_dialog_system_labels_mask_from_label_changes(v_end_user_context_ids);
    END IF;

    RETURN NULL;
END;
$$;

CREATE OR REPLACE FUNCTION public.sync_dialog_system_labels_mask_from_deleted_label_rows()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    v_end_user_context_ids uuid[];
BEGIN
    SELECT COALESCE(array_agg(DISTINCT orow."DialogEndUserContextId"), ARRAY[]::uuid[])
    INTO v_end_user_context_ids
    FROM old_rows orow;

    IF cardinality(v_end_user_context_ids) > 0 THEN
        PERFORM public.sync_dialog_system_labels_mask_from_label_changes(v_end_user_context_ids);
    END IF;

    RETURN NULL;
END;
$$;

CREATE OR REPLACE FUNCTION public.sync_dialog_system_labels_mask_from_updated_label_rows()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    v_end_user_context_ids uuid[];
BEGIN
    SELECT COALESCE(array_agg(DISTINCT rows."DialogEndUserContextId"), ARRAY[]::uuid[])
    INTO v_end_user_context_ids
    FROM (
        SELECT nr."DialogEndUserContextId"
        FROM new_rows nr
        UNION
        SELECT orow."DialogEndUserContextId"
        FROM old_rows orow
    ) rows;

    IF cardinality(v_end_user_context_ids) > 0 THEN
        PERFORM public.sync_dialog_system_labels_mask_from_label_changes(v_end_user_context_ids);
    END IF;

    RETURN NULL;
END;
$$;
