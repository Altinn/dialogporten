DROP TRIGGER IF EXISTS "TR_PR_AfterDelete" ON public."Dialog";
DROP TRIGGER IF EXISTS "UpdatePartyResource_AfterDelete_Row" ON public."Dialog";
-- NOTE: This trigger only runs on physical row deletion.
-- Soft-deletes do not fire this trigger; stale party-resource pairs are expected
-- until regular purge/hard-delete has removed the row.
CREATE TRIGGER "UpdatePartyResource_AfterDelete_Row"
AFTER DELETE ON public."Dialog"
FOR EACH ROW
EXECUTE FUNCTION partyresource.party_resource_after_delete_row();
