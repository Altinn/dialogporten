CREATE OR REPLACE FUNCTION public.party_parse_urn(p_party text)
RETURNS TABLE("ShortPrefix" char(1), "Identifier" text)
LANGUAGE plpgsql
IMMUTABLE
STRICT
AS $$
BEGIN
    IF p_party LIKE 'urn:altinn:organization:identifier-no:%' THEN
        RETURN QUERY
        SELECT 'o'::char(1), substr(p_party, 39);
        RETURN;
    END IF;

    IF p_party LIKE 'urn:altinn:person:identifier-no:%' THEN
        RETURN QUERY
        SELECT 'p'::char(1), substr(p_party, 33);
        RETURN;
    END IF;

    IF p_party LIKE 'urn:altinn:person:legacy-selfidentified:%' THEN
        RETURN QUERY
        SELECT 'i'::char(1), substr(p_party, 41);
        RETURN;
    END IF;

    IF p_party LIKE 'urn:altinn:person:idporten-email:%' THEN
        RETURN QUERY
        SELECT 'e'::char(1), substr(p_party, 34);
        RETURN;
    END IF;

    IF p_party LIKE 'urn:altinn:systemuser:uuid:%' THEN
        RETURN QUERY
        SELECT 's'::char(1), substr(p_party, 28);
        RETURN;
    END IF;

    IF p_party LIKE 'urn:altinn:feide-subject:%' THEN
        RETURN QUERY
        SELECT 'f'::char(1), substr(p_party, 26);
        RETURN;
    END IF;

    RAISE EXCEPTION 'Unsupported Party URN format: %', p_party
        USING ERRCODE = '22023';
END;
$$;
