#!/usr/bin/env bash
# Exports PGPASSWORD for the PostgreSQL server administrator login, read from the environment
# Key Vault. Meant to be SOURCED by a workflow step that then runs psql:
#
#   source ./.github/tools/pgOwnerPassword.sh
#
# Requires KEY_VAULT_NAME in the environment and an authenticated `az` session.
#
# The vault holds the administrator credentials as an ADO.NET connection string rather than as a
# bare password, so the password field is taken from it. The value is registered as a workflow
# mask before it is used anywhere, so it cannot reach the log through a command echo or an error
# message.

set -euo pipefail

if [ -z "${KEY_VAULT_NAME:-}" ]; then
  echo "::error::KEY_VAULT_NAME is not set" >&2
  exit 1
fi

CONNECTION_STRING=$(az keyvault secret show \
  --vault-name "$KEY_VAULT_NAME" \
  --name dialogportenAdoConnectionString \
  --query value -o tsv)

if [ -z "$CONNECTION_STRING" ]; then
  echo "::error::Could not read dialogportenAdoConnectionString from $KEY_VAULT_NAME" >&2
  exit 1
fi

# Split on ';' and take the single field whose key is Password. The generated password uses only
# alphanumerics and @#*+&%$!~, so it contains neither a ';' nor an '=' and the split is exact.
PGPASSWORD=$(printf '%s' "$CONNECTION_STRING" \
  | tr ';' '\n' \
  | sed -n 's/^[[:space:]]*[Pp]assword[[:space:]]*=[[:space:]]*//p' \
  | head -n 1)

if [ -z "$PGPASSWORD" ]; then
  echo "::error::No Password field found in dialogportenAdoConnectionString" >&2
  exit 1
fi

echo "::add-mask::$PGPASSWORD"
export PGPASSWORD
unset CONNECTION_STRING
