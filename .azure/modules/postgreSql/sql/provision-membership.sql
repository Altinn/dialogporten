-- Puts one workload login role into one profile role, and sets the per-role options that belong
-- on a login role.
--
-- RUN AS: an Entra administrator of the server, which SET ROLEs to the owner login (see below).
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
--   owner_role  the login that owns the application tables and created the profiles,
--               `dialogportenPgAdmin`
--
-- Idempotent: re-granting an existing membership is a no-op, and ALTER ROLE ... SET overwrites.
-- If either role is missing, the GRANT fails loudly rather than provisioning a half-configured
-- workload.

\set ON_ERROR_STOP on

-- The profiles are owned by the owner login, which holds ADMIN OPTION on them, and
-- ALTER ROLE ... SET additionally requires CREATEROLE plus admin option on the target role.
-- Verified on AT23 (PostgreSQL 18.4, 2026-09-17): that ALTER ROLE fails for a plain member of
-- azure_pg_admin and succeeds after SET ROLE to the owner. So the whole script runs as the owner.
--
-- The connecting role reaches it without a password: on Azure Flexible Server a member of
-- azure_pg_admin holds implicit SET and USAGE on every non-superuser role, so SET ROLE to the
-- owner succeeds although 'MEMBER' is false. This is Azure-specific and does not hold on stock
-- PostgreSQL, where the membership would have to be granted explicitly.
--
-- SET ROLE changes current_user and leaves session_user as the connecting identity, so the audit
-- trail still names the job that ran this rather than the shared owner login.
SET ROLE :"owner_role";

GRANT :"profile" TO :"role_name";

-- ALTER ROLE ... SET writes to pg_db_role_setting keyed on the role that actually connects, and
-- those values are NOT inherited through role membership. They therefore belong on the login role
-- rather than on the profile, even though the privileges above come from the profile.

-- Session audit for schema and role changes. The application roles issue no DDL and no GRANTs, so
-- this is expected to stay silent; it costs nothing while silent and turns an unexpected schema or
-- membership change into a log line that names the role that made it.
--
-- ALTER ROLE ... SET accepts pgaudit.log whether or not pgaudit is actually in place, because a
-- dotted name is stored as a customized option, so a missing extension would leave a setting that
-- looks configured and audits nothing. Installing pgaudit - the server extension allowlist,
-- shared_preload_libraries, and CREATE EXTENSION in this database - is an infrastructure
-- prerequisite handled separately, so this script refuses to proceed rather than paper over it.
-- That the audit records actually appear is verified with a fresh login session during rollout;
-- it cannot be established from here, since the setting only takes effect on the next connection.
DO $$
DECLARE
  preloaded text;
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'pgaudit') THEN
    RAISE EXCEPTION
      'pgaudit is not installed in database %. Run CREATE EXTENSION pgaudit there before provisioning.',
      current_database();
  END IF;

  -- shared_preload_libraries may only be examined by a superuser or a member of
  -- pg_read_all_settings, and the owner login is neither, so a denial here says nothing about the
  -- server and must not fail the run. The check above already rules out the case this would catch:
  -- pgaudit's own CREATE EXTENSION refuses unless the library is preloaded, so the extension
  -- cannot be present without it. This is a second look for servers where the setting is readable.
  BEGIN
    preloaded := current_setting('shared_preload_libraries');
  EXCEPTION WHEN insufficient_privilege THEN
    preloaded := NULL;
  END;

  IF preloaded IS NOT NULL AND position('pgaudit' in preloaded) = 0 THEN
    RAISE EXCEPTION
      'pgaudit is not in shared_preload_libraries (currently: %). Add it on the server before provisioning.',
      preloaded;
  END IF;
END $$;

-- pgaudit.log is a superuser-set parameter, so this needs more than ownership of the target role:
-- the executing role must be a superuser or hold SET on the parameter. On Azure Flexible Server
-- azure_pg_admin is expected to carry that, which is how Microsoft documents configuring per-role
-- session audit logging. A plain non-superuser owner does not, so this statement is the one that
-- fails first if that expectation does not hold on the server.
ALTER ROLE :"role_name" SET pgaudit.log = 'ddl,role';

-- Scheduled jobs have a bounded unit of work, so a statement that runs past it is stuck rather
-- than busy. The request-serving roles are left on the server default, which their own request
-- timeouts already bound. dp_reindex_search is excluded because a reindex legitimately runs long.
SELECT :'profile' IN ('dp_sync_sr_mappings', 'dp_sync_rp_info', 'dp_metrics_read')
  AS bounded_job \gset
\if :bounded_job
  ALTER ROLE :"role_name" SET statement_timeout = '5min';
\endif
