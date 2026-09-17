#!/usr/bin/env bash
# Runs the PostgreSQL provisioning scripts from inside the container app environment, which is the
# only place with network access to the VNet-injected server.
#
# Two connections, because the two halves have different requirements:
#   1. Against `postgres`, as this job's own managed identity, which is registered as a Microsoft
#      Entra administrator of the server. Only such an administrator may call the pgaadauth_*
#      functions, and they exist only in that database.
#   2. Against the application database, as the server administrator login that owns the
#      application tables, because GRANT and ALTER DEFAULT PRIVILEGES are the owner's to issue.
#
# Additive and idempotent: it creates what is missing and re-applies the expected grants and
# memberships. It removes nothing and does not reconcile drift.
#
# Environment:
#   PROVISION_WORKLOADS         JSON array of {"roleName","objectId","profile"}
#   DB_OWNER_CONNECTION_STRING  ADO.NET connection string for the application database owner
#   PG_ADMIN_ROLE               this job's PostgreSQL role name, equal to its identity name
#   AZURE_CLIENT_ID             this job's managed identity client id
#   IDENTITY_ENDPOINT           set by Container Apps
#   IDENTITY_HEADER             set by Container Apps

set -euo pipefail

SQL_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/sql"

require_env() {
  local name="$1"
  if [ -z "${!name:-}" ]; then
    echo "ERROR: $name is not set" >&2
    exit 1
  fi
}

for var in PROVISION_WORKLOADS DB_OWNER_CONNECTION_STRING PG_ADMIN_ROLE AZURE_CLIENT_ID \
           IDENTITY_ENDPOINT IDENTITY_HEADER; do
  require_env "$var"
done

# Reads one field from the ADO.NET connection string. Keys are matched without regard to case or
# internal spaces, so "User Id" and "userid" are the same field, and the value is returned as-is.
ado_field() {
  jq -rn --arg s "$DB_OWNER_CONNECTION_STRING" --arg k "$1" '
    ($k | ascii_downcase | gsub(" "; "")) as $want
    | $s
    | split(";")
    | map(select(index("=")))
    | map({
        key: (.[0:index("=")] | ascii_downcase | gsub(" "; "")),
        value: (.[index("=") + 1:])
      })
    | map(select(.key == $want))
    | .[0].value // ""
  '
}

PGHOST="$(ado_field 'Server')"
PGPORT="$(ado_field 'Port')"
PGDATABASE="$(ado_field 'Database')"
DB_OWNER_USER="$(ado_field 'User Id')"
DB_OWNER_PASSWORD="$(ado_field 'Password')"

for field in PGHOST PGPORT PGDATABASE DB_OWNER_USER DB_OWNER_PASSWORD; do
  if [ -z "${!field}" ]; then
    echo "ERROR: could not read $field from the connection string" >&2
    exit 1
  fi
done

export PGHOST PGPORT
export PGSSLMODE=require

PSQL_OPTS=(--no-psqlrc -v ON_ERROR_STOP=1)

workload_count="$(jq 'length' <<<"$PROVISION_WORKLOADS")"
if [ "$workload_count" -eq 0 ]; then
  echo "No workloads to provision"
  exit 0
fi
echo "Provisioning $workload_count workload(s) against $PGHOST"

# ---------------------------------------------------------------------------
# Step 1: register the Entra login roles (database: postgres)
# ---------------------------------------------------------------------------
# One token serves every workload: it is issued to this job's identity and is valid for far longer
# than the job runs. The value is only ever assigned, never printed.
access_token="$(
  curl -sS -f \
    -H "X-IDENTITY-HEADER: ${IDENTITY_HEADER}" \
    "${IDENTITY_ENDPOINT}?resource=https://ossrdbms-aad.database.windows.net&api-version=2019-08-01&client_id=${AZURE_CLIENT_ID}" \
    | jq -r '.access_token'
)"
if [ -z "$access_token" ] || [ "$access_token" = "null" ]; then
  echo "ERROR: could not obtain an access token for the job identity" >&2
  exit 1
fi

for i in $(seq 0 $((workload_count - 1))); do
  role_name="$(jq -r ".[$i].roleName" <<<"$PROVISION_WORKLOADS")"
  object_id="$(jq -r ".[$i].objectId" <<<"$PROVISION_WORKLOADS")"

  echo "Registering principal for $role_name"
  PGUSER="$PG_ADMIN_ROLE" PGPASSWORD="$access_token" \
    psql "${PSQL_OPTS[@]}" --dbname postgres \
      -v role_name="$role_name" -v mi_oid="$object_id" \
      -f "$SQL_DIR/provision-entra-principal.sql"
done

unset access_token

# ---------------------------------------------------------------------------
# Step 2: profiles and memberships (database: the application database)
# ---------------------------------------------------------------------------
export PGDATABASE
export PGUSER="$DB_OWNER_USER"
export PGPASSWORD="$DB_OWNER_PASSWORD"

echo "Provisioning profile roles in $PGDATABASE"
psql "${PSQL_OPTS[@]}" -f "$SQL_DIR/provision-profile-roles.sql"

for i in $(seq 0 $((workload_count - 1))); do
  role_name="$(jq -r ".[$i].roleName" <<<"$PROVISION_WORKLOADS")"
  profile="$(jq -r ".[$i].profile" <<<"$PROVISION_WORKLOADS")"

  echo "Granting $profile to $role_name"
  psql "${PSQL_OPTS[@]}" \
    -v role_name="$role_name" -v profile="$profile" \
    -f "$SQL_DIR/provision-membership.sql"
done

echo "Provisioning complete"
