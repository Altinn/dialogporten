-- Autovacuum storage parameters, as tuned in prod, for the tables EF does not map. The
-- EF-mapped tables are configured in StorageParameterConfiguration.
--
-- Each table is only altered when its reloptions do not already contain every listed option, so
-- this is a no-op (and takes no lock) where the settings are already in place. ALTER TABLE ... SET
-- takes SHARE UPDATE EXCLUSIVE, which would otherwise cancel a running autovacuum on the table.
-- Option strings are compared verbatim against pg_class.reloptions.
DO
$$
    DECLARE
        r record;
    BEGIN
        FOR r IN
            SELECT t.relation, t.options
            FROM (VALUES ('search."DialogSearchRebuildQueue"', ARRAY [
                             'autovacuum_enabled=true',
                             'autovacuum_vacuum_scale_factor=0.005',
                             'autovacuum_vacuum_threshold=1000',
                             'autovacuum_analyze_scale_factor=0.005',
                             'autovacuum_analyze_threshold=500',
                             'autovacuum_vacuum_cost_limit=200',
                             'autovacuum_vacuum_cost_delay=2'
                             ]),
                         ('partyresource."PartyResource"', ARRAY [
                             'autovacuum_vacuum_scale_factor=0.02',
                             'autovacuum_vacuum_insert_scale_factor=0.02'
                             ])) AS t(relation, options)
            LOOP
                IF NOT EXISTS (SELECT 1
                               FROM pg_class c
                               WHERE c.oid = r.relation::regclass
                                 AND coalesce(c.reloptions, '{}') @> r.options) THEN
                    EXECUTE format('ALTER TABLE %s SET (%s)', r.relation, array_to_string(r.options, ', '));
                END IF;
            END LOOP;
    END
$$;
