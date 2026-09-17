-- Puts one workload login role into one profile role, and sets the per-role options that belong
-- on a login role.
--
-- RUN AS: the server administrator login `dialogportenPgAdmin`, which created the profile roles
--         and therefore holds ADMIN OPTION on them.
-- RUN AGAINST: the application database (`dialogporten`).
--
-- Run once per workload, after provision-entra-principal.sql has created the login role and
-- provision-profile-roles.sql has created the profiles.
--
-- Required psql variables (-v name=value), quoted at the use site because role names contain '-':
--   role_name   the login role, equal to the managed identity name,
--               e.g. dp-be-test-webapi-so-identity
--   profile     one of dp_api_dml, dp_service_dml, dp_reindex_search, dp_metrics_read,
--               dp_sync_sr_mappings, dp_sync_rp_info
--
-- Idempotent: re-granting an existing membership is a no-op, and ALTER ROLE ... SET overwrites.
-- If either role is missing, the GRANT fails loudly rather than provisioning a half-configured
-- workload.

\set ON_ERROR_STOP on

GRANT :"profile" TO :"role_name";

-- ALTER ROLE ... SET writes to pg_db_role_setting keyed on the role that actually connects, and
-- those values are NOT inherited through role membership. They therefore belong on the login role
-- rather than on the profile, even though the privileges above come from the profile.

-- Session audit for schema and role changes. The application roles issue no DDL and no GRANTs, so
-- this is expected to stay silent; it costs nothing while silent and turns an unexpected schema or
-- membership change into a log line that names the role that made it.
ALTER ROLE :"role_name" SET pgaudit.log = 'ddl,role';

-- Scheduled jobs have a bounded unit of work, so a statement that runs past it is stuck rather
-- than busy. The request-serving roles are left on the server default, which their own request
-- timeouts already bound. dp_reindex_search is excluded because a reindex legitimately runs long.
SELECT :'profile' IN ('dp_sync_sr_mappings', 'dp_sync_rp_info', 'dp_metrics_read')
  AS bounded_job \gset
\if :bounded_job
  ALTER ROLE :"role_name" SET statement_timeout = '5min';
\endif
