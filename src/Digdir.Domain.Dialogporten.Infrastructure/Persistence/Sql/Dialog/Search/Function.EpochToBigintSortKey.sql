-- Converts a timestamptz to an epoch-seconds bigint sort key used to order a dialog's
-- aggregated search document. The special 'infinity'/'-infinity' timestamp values yield
-- ±Infinity from EXTRACT(EPOCH ...), which cannot be cast to bigint (SQLSTATE 0A000); they
-- are mapped to the bigint extremes so ordering is preserved (infinity sorts newest,
-- -infinity oldest).
CREATE OR REPLACE FUNCTION search."EpochToBigintSortKey"(p_ts timestamptz)
RETURNS bigint
LANGUAGE sql
IMMUTABLE
PARALLEL SAFE
AS $$
    SELECT CASE p_ts
        WHEN 'infinity'::timestamptz  THEN  9223372036854775807
        WHEN '-infinity'::timestamptz THEN -9223372036854775808
        ELSE EXTRACT(EPOCH FROM p_ts)::bigint
    END;
$$;
