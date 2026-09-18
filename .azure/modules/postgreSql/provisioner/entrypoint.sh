#!/usr/bin/env bash
# Runs the PostgreSQL provisioning scripts from inside a workload that has network access to the
# VNet-injected server, which is the only place the server is reachable from. The same image runs
# as a Container Apps job and as a Kubernetes job; the platforms differ only in how the access
# token is acquired and in how a workload's object id is found.
#
# One connection identity throughout: the identity this job runs as, which is registered as a
# Microsoft Entra administrator of the server and authenticates with a token instead of a password.
# No secret is read and none is needed.
#
#   Token on Kubernetes:     Azure workload identity federation. The projected service account
#                            token in AZURE_FEDERATED_TOKEN_FILE is exchanged for an access token
#                            at AZURE_AUTHORITY_HOST.
#   Token on Container Apps: the platform identity endpoint, IDENTITY_ENDPOINT with IDENTITY_HEADER.
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
#   PROVISION_WORKLOADS  JSON array of workload entries. Every entry carries a profile, and names
#                        the identity in one of two ways:
#
#                          {"roleName": "...", "objectId": "...", "profile": "..."}
#                              the object id is already known, typically resolved by the
#                              deployment that created the identity.
#
#                          {"applicationIdentity": "...", "profile": "...", "roleName": "..."}
#                              the identity is described by an ApplicationIdentity resource in the
#                              Kubernetes cluster this runs in. Its object id is read from that
#                              resource, as is the role name when the entry leaves roleName out.
#                              Entries of this shape are only valid on Kubernetes.
#
#   PGHOST               the PostgreSQL server FQDN
#   PGPORT               server port, defaults to 5432
#   PG_DATABASE          the application database, defaults to dialogporten
#   PG_ADMIN_ROLE        this job's PostgreSQL role name, equal to its identity name
#   PG_OWNER_ROLE        the login owning the application tables, defaults to dialogportenPgAdmin
#   PROVISION_NAMESPACE  the namespace holding the ApplicationIdentity resources, defaults to the
#                        namespace this job runs in
#   AZURE_CLIENT_ID      the client id of the identity this job runs as
#   AZURE_TENANT_ID      the tenant to request the token from, needed with workload identity
#   AZURE_FEDERATED_TOKEN_FILE, AZURE_AUTHORITY_HOST  set by Azure workload identity
#   IDENTITY_ENDPOINT, IDENTITY_HEADER                set by Container Apps

set -euo pipefail

SQL_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/sql"

PGPORT="${PGPORT:-5432}"
PG_DATABASE="${PG_DATABASE:-dialogporten}"
PG_OWNER_ROLE="${PG_OWNER_ROLE:-dialogportenPgAdmin}"

PG_RESOURCE="https://ossrdbms-aad.database.windows.net"
SERVICE_ACCOUNT_DIR="/var/run/secrets/kubernetes.io/serviceaccount"
APPLICATION_IDENTITY_API="apis/application.dis.altinn.cloud/v1alpha1"

require_env() {
  local name="$1"
  if [[ -z "${!name:-}" ]]; then
    echo "ERROR: $name is not set" >&2
    exit 1
  fi
}

for var in PROVISION_WORKLOADS PGHOST PG_ADMIN_ROLE AZURE_CLIENT_ID; do
  require_env "$var"
done

# Writes an access token for this job's identity to stdout. The token and, on Kubernetes, the
# assertion it is exchanged for are only ever assigned, never printed.
acquire_token() {
  local authority response
  if [[ -n "${AZURE_FEDERATED_TOKEN_FILE:-}" ]] && [[ -r "${AZURE_FEDERATED_TOKEN_FILE}" ]]; then
    require_env AZURE_TENANT_ID
    authority="${AZURE_AUTHORITY_HOST:-https://login.microsoftonline.com/}"
    # The authority is written with a trailing slash in some places and without it in others.
    [[ "${authority: -1}" = "/" ]] || authority="${authority}/"
    response="$(
      curl -sS -f \
        --data-urlencode "client_id=${AZURE_CLIENT_ID}" \
        --data-urlencode "grant_type=client_credentials" \
        --data-urlencode "scope=${PG_RESOURCE}/.default" \
        --data-urlencode "client_assertion_type=urn:ietf:params:oauth:client-assertion-type:jwt-bearer" \
        --data-urlencode "client_assertion=$(cat "${AZURE_FEDERATED_TOKEN_FILE}")" \
        "${authority}${AZURE_TENANT_ID}/oauth2/v2.0/token"
    )"
    jq -r '.access_token // empty' <<<"$response"
    return 0
  fi

  if [[ -n "${IDENTITY_ENDPOINT:-}" ]] && [[ -n "${IDENTITY_HEADER:-}" ]]; then
    response="$(
      curl -sS -f \
        -H "X-IDENTITY-HEADER: ${IDENTITY_HEADER}" \
        "${IDENTITY_ENDPOINT}?resource=${PG_RESOURCE}&api-version=2019-08-01&client_id=${AZURE_CLIENT_ID}"
    )"
    jq -r '.access_token // empty' <<<"$response"
    return 0
  fi

  echo "ERROR: no token source available. Set AZURE_FEDERATED_TOKEN_FILE to a readable file for" >&2
  echo "       workload identity federation, or IDENTITY_ENDPOINT and IDENTITY_HEADER when running" >&2
  echo "       on Container Apps." >&2
  return 1
}

# Writes the status of the named ApplicationIdentity resource to stdout as JSON.
read_application_identity_status() {
  local name="$1"
  local namespace token response ready

  if [[ -z "${KUBERNETES_SERVICE_HOST:-}" ]]; then
    echo "ERROR: workload entry names the ApplicationIdentity '$name', which can only be resolved" >&2
    echo "       from inside a Kubernetes cluster. Give the entry an objectId instead." >&2
    return 1
  fi

  namespace="${PROVISION_NAMESPACE:-}"
  if [[ -z "$namespace" ]]; then
    if [[ ! -r "${SERVICE_ACCOUNT_DIR}/namespace" ]]; then
      echo "ERROR: cannot read ${SERVICE_ACCOUNT_DIR}/namespace. Set PROVISION_NAMESPACE to the" >&2
      echo "       namespace holding the ApplicationIdentity resources." >&2
      return 1
    fi
    namespace="$(cat "${SERVICE_ACCOUNT_DIR}/namespace")"
  fi

  if [[ ! -r "${SERVICE_ACCOUNT_DIR}/token" ]]; then
    echo "ERROR: cannot read the service account token needed to look up ApplicationIdentity '$name'" >&2
    return 1
  fi
  token="$(cat "${SERVICE_ACCOUNT_DIR}/token")"

  if ! response="$(
    curl -sS -f \
      --cacert "${SERVICE_ACCOUNT_DIR}/ca.crt" \
      -H "Authorization: Bearer ${token}" \
      "https://${KUBERNETES_SERVICE_HOST}:${KUBERNETES_SERVICE_PORT:-443}/${APPLICATION_IDENTITY_API}/namespaces/${namespace}/applicationidentities/${name}"
  )"; then
    echo "ERROR: could not read ApplicationIdentity '$name' in namespace '$namespace'" >&2
    return 1
  fi

  ready="$(jq -r 'first(.status.conditions[]? | select(.type == "Ready") | .status) // empty' <<<"$response")"
  if [[ "$ready" != "True" ]]; then
    echo "ERROR: ApplicationIdentity '$name' in namespace '$namespace' is not Ready (condition: ${ready:-none})" >&2
    return 1
  fi

  jq -c '.status // {}' <<<"$response"
}

workload_count="$(jq 'length' <<<"$PROVISION_WORKLOADS")"
if [[ "$workload_count" -eq 0 ]]; then
  echo "No workloads to provision"
  exit 0
fi

# ---------------------------------------------------------------------------
# Resolve every entry to a role name, an object id and a profile before any of it reaches the
# database, so a mistyped or unresolvable entry stops the run rather than half-applying it.
# ---------------------------------------------------------------------------
workloads='[]'
for i in $(seq 0 $((workload_count - 1))); do
  entry="$(jq -c ".[$i]" <<<"$PROVISION_WORKLOADS")"
  role_name="$(jq -r '.roleName // empty' <<<"$entry")"
  object_id="$(jq -r '.objectId // empty' <<<"$entry")"
  profile="$(jq -r '.profile // empty' <<<"$entry")"
  application_identity="$(jq -r '.applicationIdentity // empty' <<<"$entry")"

  if [[ -n "$application_identity" ]]; then
    echo "Resolving ApplicationIdentity $application_identity"
    identity_status="$(read_application_identity_status "$application_identity")"
    object_id="$(jq -r '.principalId // empty' <<<"$identity_status")"
    if [[ -z "$object_id" ]]; then
      echo "ERROR: ApplicationIdentity '$application_identity' carries no principalId" >&2
      exit 1
    fi
    if [[ -z "$role_name" ]]; then
      role_name="$(jq -r '.managedIdentityName // empty' <<<"$identity_status")"
      if [[ -z "$role_name" ]]; then
        echo "ERROR: ApplicationIdentity '$application_identity' carries no managedIdentityName," >&2
        echo "       and the workload entry gives no roleName to use instead" >&2
        exit 1
      fi
    fi
  fi

  for field in role_name object_id profile; do
    if [[ -z "${!field}" ]]; then
      echo "ERROR: workload entry $i resolves to no $field: $entry" >&2
      exit 1
    fi
  done

  workloads="$(
    jq -c --arg roleName "$role_name" --arg objectId "$object_id" --arg profile "$profile" \
      '. + [{roleName: $roleName, objectId: $objectId, profile: $profile}]' <<<"$workloads"
  )"
done

export PGHOST PGPORT
export PGSSLMODE=verify-full
export PGSSLROOTCERT="${PGSSLROOTCERT:-system}"
export PGUSER="$PG_ADMIN_ROLE"

PSQL_OPTS=(--no-psqlrc -v ON_ERROR_STOP=1)

echo "Provisioning $workload_count workload(s) against $PGHOST as $PG_ADMIN_ROLE"

# One token serves the whole run: it is issued to this job's identity and is valid for far longer
# than the job takes.
if ! PGPASSWORD="$(acquire_token)"; then
  exit 1
fi
if [[ -z "$PGPASSWORD" ]]; then
  echo "ERROR: could not obtain an access token for the job identity" >&2
  exit 1
fi
export PGPASSWORD

# Check the database prerequisite before creating any principals, profiles or memberships.
# Installing the extension and restarting the server are deliberate bootstrap operations.
psql "${PSQL_OPTS[@]}" --dbname "$PG_DATABASE" -f "$SQL_DIR/require-pgaudit.sql"

# ---------------------------------------------------------------------------
# Step 1: register the Entra login roles (database: postgres)
# ---------------------------------------------------------------------------
for i in $(seq 0 $((workload_count - 1))); do
  role_name="$(jq -r ".[$i].roleName" <<<"$workloads")"
  object_id="$(jq -r ".[$i].objectId" <<<"$workloads")"

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
  role_name="$(jq -r ".[$i].roleName" <<<"$workloads")"
  profile="$(jq -r ".[$i].profile" <<<"$workloads")"

  echo "Granting $profile to $role_name"
  psql "${PSQL_OPTS[@]}" \
    -v role_name="$role_name" -v profile="$profile" -v owner_role="$PG_OWNER_ROLE" \
    -f "$SQL_DIR/provision-membership.sql"
done

echo "Provisioning complete"
