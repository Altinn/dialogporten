#!/usr/bin/env bash
# Runs the PostgreSQL provisioning scripts from inside the container app environment, which is the
# only place with network access to the VNet-injected server.
#
# One connection identity throughout: this job's own managed identity, which is registered as a
# Microsoft Entra administrator of the server and authenticates with a token instead of a password.
# No secret is read and none is needed.
#
#   Step 1, against `postgres`: register the workload login roles. Only an Entra administrator may
#           call the pgaadauth_* functions, and they exist only in that database.
#   Step 2, against the application database: the profile roles, their privileges and the
#           memberships. GRANT, ALTER DEFAULT PRIVILEGES and ALTER ROLE ... SET are the owner's to
#           issue, so those scripts SET ROLE to the owner login. An Entra administrator is a member
#           of azure_pg_admin, which on Azure Flexible Server carries implicit SET on every
#           non-superuser role, so that succeeds without the owner's password.
#
# Additive and idempotent: it creates what is missing and re-applies the expected grants and
# memberships. It removes nothing and does not reconcile drift.
#
# Environment:
#   PROVISION_WORKLOADS  JSON array of {"roleName","objectId","profile"}
#   PGHOST               the PostgreSQL server FQDN
#   PGPORT               server port, defaults to 5432
#   PG_DATABASE          the application database, defaults to dialogporten
#   PG_ADMIN_ROLE        this job's PostgreSQL role name, equal to its identity name
#   PG_OWNER_ROLE        the login owning the application tables, defaults to dialogportenPgAdmin
#   AZURE_CLIENT_ID      this job's managed identity client id
#   IDENTITY_ENDPOINT    set by Container Apps
#   IDENTITY_HEADER      set by Container Apps

set -euo pipefail

SQL_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/sql"

PGPORT="${PGPORT:-5432}"
PG_DATABASE="${PG_DATABASE:-dialogporten}"
PG_OWNER_ROLE="${PG_OWNER_ROLE:-dialogportenPgAdmin}"

require_env() {
  local name="$1"
  if [ -z "${!name:-}" ]; then
    echo "ERROR: $name is not set" >&2
    exit 1
  fi
}

for var in PROVISION_WORKLOADS PGHOST PG_ADMIN_ROLE AZURE_CLIENT_ID \
           IDENTITY_ENDPOINT IDENTITY_HEADER; do
  require_env "$var"
done

export PGHOST PGPORT
export PGSSLMODE=require
export PGUSER="$PG_ADMIN_ROLE"

PSQL_OPTS=(--no-psqlrc -v ON_ERROR_STOP=1)

workload_count="$(jq 'length' <<<"$PROVISION_WORKLOADS")"
if [ "$workload_count" -eq 0 ]; then
  echo "No workloads to provision"
  exit 0
fi
echo "Provisioning $workload_count workload(s) against $PGHOST as $PG_ADMIN_ROLE"

# One token serves the whole run: it is issued to this job's identity and is valid for far longer
# than the job takes. The value is only ever assigned, never printed.
PGPASSWORD="$(
  curl -sS -f \
    -H "X-IDENTITY-HEADER: ${IDENTITY_HEADER}" \
    "${IDENTITY_ENDPOINT}?resource=https://ossrdbms-aad.database.windows.net&api-version=2019-08-01&client_id=${AZURE_CLIENT_ID}" \
    | jq -r '.access_token'
)"
if [ -z "$PGPASSWORD" ] || [ "$PGPASSWORD" = "null" ]; then
  echo "ERROR: could not obtain an access token for the job identity" >&2
  exit 1
fi
export PGPASSWORD

# ---------------------------------------------------------------------------
# Step 1: register the Entra login roles (database: postgres)
# ---------------------------------------------------------------------------
for i in $(seq 0 $((workload_count - 1))); do
  role_name="$(jq -r ".[$i].roleName" <<<"$PROVISION_WORKLOADS")"
  object_id="$(jq -r ".[$i].objectId" <<<"$PROVISION_WORKLOADS")"

  echo "Registering principal for $role_name"
  psql "${PSQL_OPTS[@]}" --dbname postgres \
    -v role_name="$role_name" -v mi_oid="$object_id" \
    -f "$SQL_DIR/provision-entra-principal.sql"
done

# ---------------------------------------------------------------------------
# Step 2: profiles and memberships (database: the application database)
# ---------------------------------------------------------------------------
export PGDATABASE="$PG_DATABASE"

echo "Provisioning profile roles in $PGDATABASE as $PG_OWNER_ROLE"
psql "${PSQL_OPTS[@]}" -v owner_role="$PG_OWNER_ROLE" \
  -f "$SQL_DIR/provision-profile-roles.sql"

for i in $(seq 0 $((workload_count - 1))); do
  role_name="$(jq -r ".[$i].roleName" <<<"$PROVISION_WORKLOADS")"
  profile="$(jq -r ".[$i].profile" <<<"$PROVISION_WORKLOADS")"

  echo "Granting $profile to $role_name"
  psql "${PSQL_OPTS[@]}" \
    -v role_name="$role_name" -v profile="$profile" -v owner_role="$PG_OWNER_ROLE" \
    -f "$SQL_DIR/provision-membership.sql"
done

echo "Provisioning complete"
