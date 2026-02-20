DROP TRIGGER IF EXISTS "TR_PR_AfterDelete" ON public."Dialog";
CREATE TRIGGER "TR_PR_AfterDelete"
AFTER DELETE ON public."Dialog"
FOR EACH ROW
EXECUTE FUNCTION public.party_resource_after_delete_row();
