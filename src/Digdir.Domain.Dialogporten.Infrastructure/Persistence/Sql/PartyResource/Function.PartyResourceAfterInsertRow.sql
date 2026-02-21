CREATE OR REPLACE FUNCTION partyresource.party_resource_after_insert_row()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    v_party_id integer;
    v_resource_id integer;
    v_party_prefix char(1);
    v_unprefixed_party_identifier text;
    v_unprefixed_resource_identifier text;
BEGIN
    SELECT "ShortPrefix", "UnprefixedPartyIdentifier"
    INTO v_party_prefix, v_unprefixed_party_identifier
    FROM partyresource.party_parse_urn(NEW."Party");

    v_unprefixed_resource_identifier := partyresource.resource_from_urn(NEW."ServiceResource");

    INSERT INTO partyresource."Party" ("ShortPrefix", "UnprefixedPartyIdentifier")
    VALUES (v_party_prefix, v_unprefixed_party_identifier)
    ON CONFLICT ("ShortPrefix", "UnprefixedPartyIdentifier")
    DO UPDATE SET "UnprefixedPartyIdentifier" = EXCLUDED."UnprefixedPartyIdentifier"
    RETURNING "Id" INTO v_party_id;

    INSERT INTO partyresource."Resource" ("UnprefixedResourceIdentifier")
    VALUES (v_unprefixed_resource_identifier)
    ON CONFLICT ("UnprefixedResourceIdentifier")
    DO UPDATE SET "UnprefixedResourceIdentifier" = EXCLUDED."UnprefixedResourceIdentifier"
    RETURNING "Id" INTO v_resource_id;

    INSERT INTO partyresource."PartyResource" ("PartyId", "ResourceId")
    VALUES (v_party_id, v_resource_id)
    ON CONFLICT ("PartyId", "ResourceId") DO NOTHING;

    RETURN NULL;
END;
$$;
