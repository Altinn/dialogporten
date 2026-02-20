CREATE OR REPLACE FUNCTION public.party_resource_after_insert_row()
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
    FROM public.party_parse_urn(NEW."Party");

    v_resource_identifier := public.resource_from_urn(NEW."ServiceResource");

    INSERT INTO public."Party" ("ShortPrefix", "Identifier")
    VALUES (v_party_prefix, v_party_identifier)
    ON CONFLICT ("ShortPrefix", "Identifier")
    DO UPDATE SET "ShortPrefix" = EXCLUDED."ShortPrefix"
    RETURNING "Id" INTO v_party_id;

    INSERT INTO public."Resource" ("Identifier")
    VALUES (v_resource_identifier)
    ON CONFLICT ("Identifier")
    DO UPDATE SET "Identifier" = EXCLUDED."Identifier"
    RETURNING "Id" INTO v_resource_id;

    INSERT INTO public."PartyResource" ("PartyId", "ResourceId")
    VALUES (v_party_id, v_resource_id)
    ON CONFLICT ("PartyId", "ResourceId") DO NOTHING;

    RETURN NULL;
END;
$$;
