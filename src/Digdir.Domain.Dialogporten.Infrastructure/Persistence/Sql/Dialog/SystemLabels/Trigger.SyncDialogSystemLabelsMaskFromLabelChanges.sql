DROP TRIGGER IF EXISTS "TR_DialogSystemLabelsMask_AfterInsert_Statement" ON public."DialogEndUserContextSystemLabel";
DROP TRIGGER IF EXISTS "TR_DialogSystemLabelsMask_AfterDelete_Statement" ON public."DialogEndUserContextSystemLabel";
DROP TRIGGER IF EXISTS "TR_DialogSystemLabelsMask_AfterUpdate_Statement" ON public."DialogEndUserContextSystemLabel";

CREATE TRIGGER "TR_DialogSystemLabelsMask_AfterInsert_Statement"
AFTER INSERT ON public."DialogEndUserContextSystemLabel"
REFERENCING NEW TABLE AS new_rows
FOR EACH STATEMENT
EXECUTE FUNCTION public.sync_dialog_system_labels_mask_from_inserted_label_rows();

CREATE TRIGGER "TR_DialogSystemLabelsMask_AfterDelete_Statement"
AFTER DELETE ON public."DialogEndUserContextSystemLabel"
REFERENCING OLD TABLE AS old_rows
FOR EACH STATEMENT
EXECUTE FUNCTION public.sync_dialog_system_labels_mask_from_deleted_label_rows();

CREATE TRIGGER "TR_DialogSystemLabelsMask_AfterUpdate_Statement"
AFTER UPDATE ON public."DialogEndUserContextSystemLabel"
REFERENCING OLD TABLE AS old_rows NEW TABLE AS new_rows
FOR EACH STATEMENT
EXECUTE FUNCTION public.sync_dialog_system_labels_mask_from_updated_label_rows();
