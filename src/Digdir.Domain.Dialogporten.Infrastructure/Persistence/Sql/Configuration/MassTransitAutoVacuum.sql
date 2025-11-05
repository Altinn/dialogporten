-- Override autovacuum settings for these high churn tables
-- Setting scale factors to 0 ignores table size, and performs a vacuum/analyze as soon
-- as the thresholds are hit. We increase the amount of work allowed to the max, and
-- explicitly set the delay to 2ms (which should be equal to default in PG13+)
ALTER TABLE public."MassTransitOutboxMessage" SET (
    autovacuum_enabled = true,
    autovacuum_vacuum_scale_factor = 0.0,
    autovacuum_vacuum_threshold = 1000,
    autovacuum_analyze_scale_factor = 0.0,
    autovacuum_analyze_threshold = 500,
    autovacuum_vacuum_cost_limit = 10000,
    autovacuum_vacuum_cost_delay = 2
    );

ALTER TABLE public."MassTransitOutboxState" SET (
    autovacuum_enabled = true,
    autovacuum_vacuum_scale_factor = 0.0,
    autovacuum_vacuum_threshold = 1000,
    autovacuum_analyze_scale_factor = 0.0,
    autovacuum_analyze_threshold = 500,
    autovacuum_vacuum_cost_limit = 10000,
    autovacuum_vacuum_cost_delay = 2
    );

-- Force index cleanup during auto vacuum
-- See https://www.postgresql.org/docs/current/sql-createtable.html#RELOPTION-VACUUM-INDEX-CLEANUP
ALTER TABLE public."MassTransitOutboxMessage"
    SET (vacuum_index_cleanup = on);

ALTER TABLE public."MassTransitOutboxState"
    SET (vacuum_index_cleanup = on);
