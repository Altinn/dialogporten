CREATE OR REPLACE FUNCTION public.party_resource_after_delete_row()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    v_party_id integer;
    v_resource_id integer;
    v_party_prefix char(1);
    v_party_identifier text;
    v_resource_identifier text;
BEGIN
    SELECT "ShortPrefix", "Identifier"
    INTO v_party_prefix, v_party_identifier
    FROM public.party_parse_urn(OLD."Party");

    v_resource_identifier := public.resource_from_urn(OLD."ServiceResource");

    SELECT "Id" INTO v_party_id FROM public."Party"
    WHERE "ShortPrefix" = v_party_prefix AND "Identifier" = v_party_identifier;

    SELECT "Id" INTO v_resource_id FROM public."Resource"
    WHERE "Identifier" = v_resource_identifier;

    IF v_party_id IS NOT NULL AND v_resource_id IS NOT NULL THEN
        DELETE FROM public."PartyResource"
        WHERE "PartyId" = v_party_id
          AND "ResourceId" = v_resource_id
          AND NOT EXISTS (
            SELECT 1 FROM public."Dialog"
            WHERE "Party" = OLD."Party"
              AND "ServiceResource" = OLD."ServiceResource"
        );
    END IF;

    RETURN NULL;
END;
$$;
