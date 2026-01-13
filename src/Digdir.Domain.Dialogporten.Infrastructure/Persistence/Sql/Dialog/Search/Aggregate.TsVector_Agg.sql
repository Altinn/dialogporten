CREATE OR REPLACE FUNCTION public.tsvector_concat_truncated(
    state tsvector,
    next tsvector
)
    RETURNS tsvector
    LANGUAGE plpgsql
    IMMUTABLE
AS
$$
DECLARE
    max_bytes CONSTANT integer := 1048575;
    next_len integer;
BEGIN
    IF next IS NULL THEN
        RETURN state;
    END IF;

    next_len := pg_catalog.pg_column_size(state) + pg_catalog.pg_column_size(next);

    IF next_len > max_bytes THEN
        RETURN state;
    END IF;

    RETURN pg_catalog.tsvector_concat(state, next);
END;
$$;

CREATE OR REPLACE AGGREGATE public.tsvector_agg(tsvector) (
    SFUNC = public.tsvector_concat_truncated,
    STYPE = tsvector,
    INITCOND = ''
    );
