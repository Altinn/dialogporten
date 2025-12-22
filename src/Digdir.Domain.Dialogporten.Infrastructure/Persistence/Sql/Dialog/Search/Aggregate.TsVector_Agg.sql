CREATE OR REPLACE AGGREGATE public.tsvector_agg(tsvector) (
    SFUNC = pg_catalog.tsvector_concat,
    STYPE = tsvector,
    INITCOND = ''
    );
