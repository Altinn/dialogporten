-- Strips fixed resource URN prefix.
CREATE OR REPLACE FUNCTION partyresource.resource_from_urn(p_resource text)
RETURNS text
LANGUAGE plpgsql
IMMUTABLE
STRICT
AS $$
BEGIN
    IF p_resource LIKE 'urn:altinn:resource:%' THEN
        RETURN substr(p_resource, 21);
    END IF;

    RAISE EXCEPTION 'Unsupported resource URN format: %', p_resource
        USING ERRCODE = '22023';
END;
$$;
