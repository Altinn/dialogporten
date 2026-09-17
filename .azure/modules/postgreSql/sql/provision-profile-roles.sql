-- Creates the Dialogporten PostgreSQL profile roles and their privileges.
--
-- RUN AS: an Entra administrator of the server, which SET ROLEs to the owner login (see below).
-- RUN AGAINST: the application database (`dialogporten`).
--
-- Required psql variables (-v name=value), quoted at the use site:
--   owner_role  the login that owns the application tables, `dialogportenPgAdmin`
--
-- A profile is a plain NOLOGIN PostgreSQL role, not an Entra principal. Each workload's login
-- role - created by provision-entra-principal.sql and named after its managed identity - is made
-- a member of exactly one profile by provision-membership.sql. Privileges are therefore defined
-- once per profile and shared by every workload on that profile, instead of being repeated per
-- workload.
--
-- Profiles and what they are for:
--   dp_api_dml           the request-serving APIs: read/write on the application schemas
--   dp_service_dml       the background service: same privileges today, kept separate so the two
--                        can diverge without touching the APIs
--   dp_reindex_search    the search reindex job: reads the application data in `public` and
--                        owns the churn in `search`
--   dp_metrics_read      the custom metrics job: reads catalog views only (pg_class and friends
--                        are world readable), so CONNECT is all it needs
--   dp_sync_sr_mappings  the subject-resource mapping sync job: two named tables
--   dp_sync_rp_info      the resource policy information sync job: one named table
--
-- Idempotent: CREATE ROLE is guarded, GRANT and ALTER DEFAULT PRIVILEGES are safe to re-run.
--
-- ADDITIVE: this script never REVOKEs and does not converge to an exact privilege set. A
-- privilege removed from a profile here stays in place on the server until it is revoked by
-- hand. Exact-set reconciliation is a separate, deliberate step.
--
-- Grants are scoped to objects owned by current_user, so extension-owned objects in `public`
-- (pg_stat_statements and the like) are left alone and no "no privileges were granted" warnings
-- are produced.
--
-- ALTER DEFAULT PRIVILEGES covers tables and sequences created LATER in the listed schemas by
-- the same owner role, which is how tables added by future migrations are picked up. It does
-- NOT cover new SCHEMAS: a migration that adds a schema needs the schema list below extended
-- and this script re-run.

\set ON_ERROR_STOP on

-- GRANT and ALTER DEFAULT PRIVILEGES are the object owner's to issue, and the owner-scoped loops
-- below match on current_user, so the whole script runs as the owner login.
--
-- The connecting role reaches it without a password: on Azure Flexible Server a member of
-- azure_pg_admin holds implicit SET and USAGE on every non-superuser role. Verified on AT23
-- (PostgreSQL 18.4, 2026-09-17): for a role whose only membership is azure_pg_admin,
-- pg_has_role(current_user, 'dialogportenPgAdmin', 'SET') and 'USAGE' are both true while
-- 'MEMBER' is false, and SET ROLE to the owner succeeds. This is Azure-specific; it does not hold
-- on stock PostgreSQL, where the membership would have to be granted explicitly.
--
-- SET ROLE changes current_user and leaves session_user as the connecting identity, so the audit
-- trail still names the job that ran this rather than the shared owner login.
SET ROLE :"owner_role";

-- ===========================================================================
-- Profile roles and database access
-- ===========================================================================
DO $$
DECLARE
  profiles constant text[] := ARRAY[
    'dp_api_dml',
    'dp_service_dml',
    'dp_reindex_search',
    'dp_metrics_read',
    'dp_sync_sr_mappings',
    'dp_sync_rp_info'
  ];
  p text;
BEGIN
  FOREACH p IN ARRAY profiles LOOP
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = p) THEN
      EXECUTE format('CREATE ROLE %I NOLOGIN', p);
    END IF;
    EXECUTE format('GRANT CONNECT ON DATABASE %I TO %I', current_database(), p);
  END LOOP;
END $$;

-- ===========================================================================
-- dp_api_dml and dp_service_dml: DML across the application schemas
-- ===========================================================================
-- Sequences are included so INSERTs into identity/serial columns work.
DO $$
DECLARE
  dml_profiles constant text[] := ARRAY['dp_api_dml', 'dp_service_dml'];
  app_schemas  constant text[] := ARRAY['public', 'partyresource', 'maintenance', 'search'];
  p text;
  s text;
  obj record;
BEGIN
  FOREACH p IN ARRAY dml_profiles LOOP
    FOREACH s IN ARRAY app_schemas LOOP
      EXECUTE format('GRANT USAGE ON SCHEMA %I TO %I', s, p);

      FOR obj IN
        SELECT c.relname FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE n.nspname = s AND c.relkind IN ('r', 'p')
          AND pg_get_userbyid(c.relowner) = current_user
      LOOP
        EXECUTE format('GRANT SELECT, INSERT, UPDATE, DELETE ON %I.%I TO %I', s, obj.relname, p);
      END LOOP;

      -- Views and materialized views are read through by functions such as
      -- search."UpsertDialogSearchOne", none of which is SECURITY DEFINER, so the calling role
      -- needs SELECT on them in its own right.
      FOR obj IN
        SELECT c.relname FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE n.nspname = s AND c.relkind IN ('v', 'm')
          AND pg_get_userbyid(c.relowner) = current_user
      LOOP
        EXECUTE format('GRANT SELECT ON %I.%I TO %I', s, obj.relname, p);
      END LOOP;

      FOR obj IN
        SELECT c.relname FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE n.nspname = s AND c.relkind = 'S'
          AND pg_get_userbyid(c.relowner) = current_user
      LOOP
        EXECUTE format('GRANT USAGE, SELECT ON SEQUENCE %I.%I TO %I', s, obj.relname, p);
      END LOOP;

      -- ON TABLES covers views created later as well - default privileges do not have a separate
      -- view class - so the SELECT in the grant below is what future views inherit.
      EXECUTE format(
        'ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO %I',
        s, p);
      EXECUTE format(
        'ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT USAGE, SELECT ON SEQUENCES TO %I', s, p);
    END LOOP;
  END LOOP;
END $$;

-- ===========================================================================
-- dp_reindex_search: read the source data, write the search index
-- ===========================================================================
-- MAINTAIN covers VACUUM, ANALYZE, REINDEX and CLUSTER on the named table without table
-- ownership or membership in an administrative role. It is a table-level privilege from
-- PostgreSQL 17 onwards; the servers run 18.
DO $$
DECLARE
  p          constant text := 'dp_reindex_search';
  obj record;
BEGIN
  -- public: read-only over the dialog data the index is built from. Views are included because
  -- the search upsert functions read through them and none is SECURITY DEFINER.
  EXECUTE format('GRANT USAGE ON SCHEMA public TO %I', p);
  FOR obj IN
    SELECT c.relname FROM pg_class c
    JOIN pg_namespace n ON n.oid = c.relnamespace
    WHERE n.nspname = 'public' AND c.relkind IN ('r', 'p', 'v', 'm')
      AND pg_get_userbyid(c.relowner) = current_user
  LOOP
    EXECUTE format('GRANT SELECT ON public.%I TO %I', obj.relname, p);
  END LOOP;
  -- ON TABLES covers views created later as well; default privileges have no separate view class.
  EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT ON TABLES TO %I', p);

  -- search: full DML, since the job rebuilds the contents of this schema.
  EXECUTE format('GRANT USAGE ON SCHEMA search TO %I', p);
  FOR obj IN
    SELECT c.relname FROM pg_class c
    JOIN pg_namespace n ON n.oid = c.relnamespace
    WHERE n.nspname = 'search' AND c.relkind IN ('r', 'p')
      AND pg_get_userbyid(c.relowner) = current_user
  LOOP
    EXECUTE format('GRANT SELECT, INSERT, UPDATE, DELETE ON search.%I TO %I', obj.relname, p);
  END LOOP;
  -- The search views (VDialogDocument, VDialogContent) live here and are read, not written.
  FOR obj IN
    SELECT c.relname FROM pg_class c
    JOIN pg_namespace n ON n.oid = c.relnamespace
    WHERE n.nspname = 'search' AND c.relkind IN ('v', 'm')
      AND pg_get_userbyid(c.relowner) = current_user
  LOOP
    EXECUTE format('GRANT SELECT ON search.%I TO %I', obj.relname, p);
  END LOOP;
  FOR obj IN
    SELECT c.relname FROM pg_class c
    JOIN pg_namespace n ON n.oid = c.relnamespace
    WHERE n.nspname = 'search' AND c.relkind = 'S'
      AND pg_get_userbyid(c.relowner) = current_user
  LOOP
    EXECUTE format('GRANT USAGE, SELECT ON SEQUENCE search.%I TO %I', obj.relname, p);
  END LOOP;
  EXECUTE format(
    'ALTER DEFAULT PRIVILEGES IN SCHEMA search GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO %I',
    p);
  EXECUTE format(
    'ALTER DEFAULT PRIVILEGES IN SCHEMA search GRANT USAGE, SELECT ON SEQUENCES TO %I', p);

  -- Maintenance on the index table itself: a full reindex leaves enough dead tuples that the job
  -- needs to be able to VACUUM and ANALYZE it rather than wait for autovacuum.
  IF to_regclass('search."DialogSearch"') IS NOT NULL THEN
    EXECUTE format('GRANT MAINTAIN ON TABLE search."DialogSearch" TO %I', p);
  ELSE
    RAISE NOTICE 'search."DialogSearch" not present, skipping MAINTAIN grant for %', p;
  END IF;
END $$;

-- ===========================================================================
-- dp_metrics_read: catalog reads only
-- ===========================================================================
-- Deliberately empty. The job reads pg_class and other catalog relations, which are readable by
-- any role that can connect, so CONNECT (granted above) is the whole privilege set. No schema
-- USAGE and no table grants are issued, and no default privileges are set: if the job ever needs
-- application data, that is a change to make here explicitly.

-- ===========================================================================
-- dp_sync_sr_mappings: subject-resource mapping sync
-- ===========================================================================
-- Named tables only. No ALTER DEFAULT PRIVILEGES for this profile: a schema-wide default would
-- hand it DML on every table a future migration adds to `public`, which is far wider than the two
-- tables it uses. A new table this job needs is added to the list here and this script re-run.
DO $$
DECLARE
  p constant text := 'dp_sync_sr_mappings';
BEGIN
  EXECUTE format('GRANT USAGE ON SCHEMA public TO %I', p);
  EXECUTE format('GRANT USAGE ON SCHEMA partyresource TO %I', p);
  EXECUTE format('GRANT SELECT ON partyresource."Resource" TO %I', p);
  EXECUTE format('GRANT SELECT, INSERT, UPDATE, DELETE ON public."SubjectResource" TO %I', p);
END $$;

-- ===========================================================================
-- dp_sync_rp_info: resource policy information sync
-- ===========================================================================
-- One named table, written with a raw-SQL MERGE. No DELETE and, for the same reason as above, no
-- ALTER DEFAULT PRIVILEGES.
DO $$
DECLARE
  p constant text := 'dp_sync_rp_info';
BEGIN
  EXECUTE format('GRANT USAGE ON SCHEMA public TO %I', p);
  EXECUTE format('GRANT SELECT, INSERT, UPDATE ON public."ResourcePolicyInformation" TO %I', p);
END $$;
