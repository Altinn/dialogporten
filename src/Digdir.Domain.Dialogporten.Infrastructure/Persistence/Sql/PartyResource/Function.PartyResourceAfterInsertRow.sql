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
    DO NOTHING
    RETURNING "Id" INTO v_party_id;

    IF v_party_id IS NULL THEN
        SELECT "Id" INTO v_party_id
        FROM partyresource."Party"
        WHERE "ShortPrefix" = v_party_prefix
          AND "UnprefixedPartyIdentifier" = v_unprefixed_party_identifier;
    END IF;

    INSERT INTO partyresource."Resource" ("UnprefixedResourceIdentifier")
    VALUES (v_unprefixed_resource_identifier)
    ON CONFLICT ("UnprefixedResourceIdentifier")
    DO NOTHING
    RETURNING "Id" INTO v_resource_id;

    IF v_resource_id IS NULL THEN
        SELECT "Id" INTO v_resource_id
        FROM partyresource."Resource"
        WHERE "UnprefixedResourceIdentifier" = v_unprefixed_resource_identifier;
    END IF;

    INSERT INTO partyresource."PartyResource" ("PartyId", "ResourceId")
    VALUES (v_party_id, v_resource_id)
    ON CONFLICT ("PartyId", "ResourceId") DO NOTHING;

    RETURN NULL;
END;
$$;
