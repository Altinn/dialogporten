#!/usr/bin/env bash
# Resolves the fully qualified domain name of the active Dialogporten PostgreSQL server and writes
# it to $GITHUB_OUTPUT as `pg_fqdn`.
#
# Usage: resolvePostgresFqdn.sh <resource-group-name> <server-name-stem>
#
# The resource group can hold several servers carrying the Dialogporten product tag (a legacy
# server and a backup server), so the name stem is matched as well and the script fails unless
# exactly one server matches. Picking the wrong server silently is the failure mode worth ruling
# out here.

set -euo pipefail

RG="${1:-}"
STEM="${2:-}"

if [ -z "$RG" ] || [ -z "$STEM" ]; then
  echo "Usage: $0 <resource-group-name> <server-name-stem>" >&2
  exit 1
fi

MATCHES=$(az postgres flexible-server list \
  --resource-group "$RG" \
  --query "[?tags.Product=='Dialogporten' && contains(name, '$STEM')].fullyQualifiedDomainName" \
  -o tsv)

COUNT=$(printf '%s\n' "$MATCHES" | grep -c . || true)
if [ "$COUNT" -ne 1 ]; then
  echo "Expected exactly one Dialogporten PostgreSQL server matching stem '$STEM' in $RG, found $COUNT:" >&2
  printf '%s\n' "$MATCHES" >&2
  exit 1
fi

echo "Resolved PostgreSQL server: $MATCHES"
echo "pg_fqdn=$MATCHES" >> "$GITHUB_OUTPUT"
