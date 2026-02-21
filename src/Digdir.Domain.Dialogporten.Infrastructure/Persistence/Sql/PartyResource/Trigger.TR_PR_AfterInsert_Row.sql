DROP TRIGGER IF EXISTS "TR_PR_AfterInsert_Row" ON public."Dialog";
CREATE TRIGGER "TR_PR_AfterInsert_Row"
AFTER INSERT ON public."Dialog"
FOR EACH ROW
EXECUTE FUNCTION partyresource.party_resource_after_insert_row();
