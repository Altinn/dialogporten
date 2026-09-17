-- Reusable provisioning of an Entra-mapped PostgreSQL login role for a Dialogporten workload
-- managed identity. One script for every principal; the caller supplies variables.
--
-- RUN AS: an Entra administrator on the PostgreSQL server (the GitHub Actions deployer service
--         principal), authenticating with an `oss-rdbms` access token as its password.
-- RUN AGAINST: database `postgres`. The pgaadauth_* functions exist only there.
--
-- This is step 1 of provisioning. It registers the login role and nothing else; the role holds
-- no privileges on the application database when this script finishes.
--
-- Step 2 lives in two sibling scripts, both run against the `dialogporten` database as the
-- application table owner (`dialogportenPgAdmin`) rather than as the deployer service principal,
-- because GRANT and ALTER DEFAULT PRIVILEGES must be issued by the owner of the objects:
--   provision-profile-roles.sql  creates the shared profile roles and their privileges (once).
--   provision-membership.sql     puts one login role into one profile (once per workload).
--
-- This script is ADDITIVE and idempotent: it registers a login role if it is missing and leaves
-- an already correctly mapped role alone. It does NOT touch dialogportenPgAdmin, existing roles,
-- or server auth configuration.
--
-- Required psql variables (-v name=value), quoted at the use site because role names contain '-':
--   role_name   the PG role to create, equal to the managed identity name,
--               e.g. dp-be-test-sync-rp-info-identity
--   mi_oid      the managed identity's principal (object) id (a GUID)
--
-- Token acquisition (caller side):
--   az account get-access-token --resource-type oss-rdbms --query accessToken -o tsv
-- used as PGPASSWORD; connect as the deployer SP's own Entra username.

\set ON_ERROR_STOP on

-- ===========================================================================
-- STEP 1 (database: postgres) - register the principal, idempotently
-- ===========================================================================

-- Idempotency must be checked by the Entra OBJECT ID, not just the role name. Azure binds an
-- Entra-authenticated role to the principal's unique object id (stored in a pgaadauth security
-- label). If a managed identity is deleted and recreated, it gets a NEW object id while the role
-- name is unchanged - a name-only guard would skip creation and leave the role bound to a dead
-- oid, so the new identity could not authenticate. We therefore inspect the existing mapping:
--   role_exists      = a PG role with this name exists at all
--   mapped_oid       = the object id the existing Entra mapping points to ('' if no mapping)
-- pgaadauth_list_principals(false) returns lowercase, unquoted columns on the server:
--   (rolname, principaltype, objectid, tenantid, ismfa, isadmin)
-- Microsoft's documentation spells them rolename/principalType/objectId; that spelling does not
-- match the actual function (verified on AT23, PostgreSQL 18.4, 2026-09-17).
SELECT
  EXISTS (SELECT 1 FROM pg_roles WHERE rolname = :'role_name')                       AS role_exists,
  COALESCE((SELECT p.objectid
            FROM pgaadauth_list_principals(false) AS p
            WHERE p.rolname = :'role_name'), '')                                     AS mapped_oid
\gset

\if :role_exists
  SELECT :'mapped_oid' = ''             AS mapping_missing \gset
  SELECT :'mapped_oid' = :'mi_oid'      AS mapping_matches \gset
  \if :mapping_missing
    \echo 'ERROR: role exists but has NO Entra (pgaadauth) mapping - refusing to touch it.'
    \echo '       A plain PG role already owns this name; resolve manually before provisioning.'
    \quit 1
  \elif :mapping_matches
    \echo 'Principal exists and is mapped to the expected object id, skipping creation'
  \else
    \echo 'ERROR: role exists but is mapped to a DIFFERENT Entra object id than expected.'
    \echo '       Expected:' :'mi_oid'
    \echo '       Found:   ' :'mapped_oid'
    \echo '       The managed identity was likely deleted and recreated. Drop and reprovision'
    \echo '       the role manually (DROP ROLE then rerun) - this script will not silently re-map.'
    \quit 1
  \endif
\else
  SELECT pgaadauth_create_principal_with_oid(
    :'role_name',  -- roleName -> becomes the PG role
    :'mi_oid',     -- objectId -> the MI's service principal object id
    'service',     -- objectType: 'service' for a user-assigned managed identity
    false,         -- isAdmin
    false          -- isMfa
  );
  \echo 'Created Entra principal'
\endif

-- Ensure the role may log in. Per-role settings such as statement_timeout are applied by
-- provision-membership.sql, which runs as the owner login against the application database.
ALTER ROLE :"role_name" LOGIN;
