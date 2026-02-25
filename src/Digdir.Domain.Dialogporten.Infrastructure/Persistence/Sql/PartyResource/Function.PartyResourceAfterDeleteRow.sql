CREATE OR REPLACE FUNCTION partyresource.party_resource_after_delete_row()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    v_party_id integer;
    v_resource_id integer;
    v_deleted_rows integer;
    v_party_prefix char(1);
    v_unprefixed_party_identifier text;
    v_unprefixed_resource_identifier text;
BEGIN
    SELECT "ShortPrefix", "UnprefixedPartyIdentifier"
    INTO v_party_prefix, v_unprefixed_party_identifier
    FROM partyresource.party_parse_urn(OLD."Party");

    v_unprefixed_resource_identifier := partyresource.resource_from_urn(OLD."ServiceResource");

    SELECT "Id" INTO v_party_id FROM partyresource."Party"
    WHERE "ShortPrefix" = v_party_prefix AND "UnprefixedPartyIdentifier" = v_unprefixed_party_identifier;

    SELECT "Id" INTO v_resource_id FROM partyresource."Resource"
    WHERE "UnprefixedResourceIdentifier" = v_unprefixed_resource_identifier;

    IF v_party_id IS NOT NULL AND v_resource_id IS NOT NULL THEN
        -- We intentionally do not track dialog soft-deletes here to avoid extra update-trigger load.
        -- Rows are pruned on hard-delete/purge instead. Concurrent deletes can temporarily leave stale
        -- PartyResource rows because each transaction may still observe another matching Dialog row.
        DELETE FROM partyresource."PartyResource"
        WHERE "PartyId" = v_party_id
          AND "ResourceId" = v_resource_id
          AND NOT EXISTS (
            SELECT 1 FROM public."Dialog"
            WHERE "Party" = OLD."Party"
              AND "ServiceResource" = OLD."ServiceResource"
        );

        GET DIAGNOSTICS v_deleted_rows = ROW_COUNT;

        IF v_deleted_rows > 0 THEN
            BEGIN
                DELETE FROM partyresource."Party" p
                WHERE p."Id" = v_party_id
                  AND NOT EXISTS (
                    SELECT 1
                    FROM partyresource."PartyResource" pr
                    WHERE pr."PartyId" = p."Id"
                );
            EXCEPTION
                WHEN foreign_key_violation THEN
                    NULL;
            END;

            BEGIN
                DELETE FROM partyresource."Resource" r
                WHERE r."Id" = v_resource_id
                  AND NOT EXISTS (
                    SELECT 1
                    FROM partyresource."PartyResource" pr
                    WHERE pr."ResourceId" = r."Id"
                );
            EXCEPTION
                WHEN foreign_key_violation THEN
                    NULL;
            END;
        END IF;
    END IF;

    RETURN NULL;
END;
$$;
