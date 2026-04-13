CREATE OR REPLACE FUNCTION public.recompute_dialog_system_labels_mask(p_dialog_ids uuid[])
RETURNS void
LANGUAGE sql
AS $$
    WITH target_dialogs AS (
        SELECT DISTINCT dialog_id
        FROM unnest(p_dialog_ids) AS t(dialog_id)
        WHERE dialog_id IS NOT NULL
    ),
    computed_masks AS (
        SELECT td.dialog_id,
               COALESCE(SUM(DISTINCT (1 << (sl."SystemLabelId" - 1))), 0)::smallint AS system_labels_mask
        FROM target_dialogs td
        LEFT JOIN public."DialogEndUserContext" dec ON dec."DialogId" = td.dialog_id
        LEFT JOIN public."DialogEndUserContextSystemLabel" sl ON sl."DialogEndUserContextId" = dec."Id"
        GROUP BY td.dialog_id
    )
    UPDATE public."Dialog" d
    SET "SystemLabelsMask" = cm.system_labels_mask
    FROM computed_masks cm
    WHERE d."Id" = cm.dialog_id
      AND d."SystemLabelsMask" IS DISTINCT FROM cm.system_labels_mask;
$$;
