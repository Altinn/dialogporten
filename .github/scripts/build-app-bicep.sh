#!/usr/bin/env bash
# Builds every application Bicep template in .azure/applications and each of its parameter files.
# Catches template and parameter errors in PRs without Azure access. The parameter files read their
# values from the environment at deploy time, so this sets placeholders for them. A parameter file that
# reads a new environment variable needs a placeholder here.
set -euo pipefail
shopt -s nullglob

export AZURE_APP_CONFIGURATION_NAME="placeholder-appconfig"
export AZURE_APP_INSIGHTS_CONNECTION_STRING="InstrumentationKey=00000000-0000-0000-0000-000000000000"
export AZURE_CONTAINER_APP_ENVIRONMENT_NAME="placeholder-cae"
export AZURE_ENVIRONMENT_KEY_VAULT_NAME="placeholder-kv"
export AZURE_SERVICE_BUS_NAMESPACE_NAME="placeholder-sb"
export AZURE_SUBSCRIPTION_ID="00000000-0000-0000-0000-000000000000"
export IMAGE_TAG="0.0.0-placeholder"
export REVISION_SUFFIX="placeholder"

out_dir=$(mktemp -d)
trap 'rm -rf "$out_dir"' EXIT

failed=0
for template in .azure/applications/*/main.bicep; do
  app=$(basename "$(dirname "$template")")
  echo "::group::$app"
  if ! az bicep build --file "$template" --outfile "$out_dir/$app.json"; then
    echo "::error file=$template::Bicep build failed for $app"
    failed=1
    # Every parameter file would fail on the same template error.
    echo "::endgroup::"
    continue
  fi
  for params in "$(dirname "$template")"/*.bicepparam; do
    if ! az bicep build-params --file "$params" --outfile "$out_dir/$app-$(basename "$params" .bicepparam).parameters.json"; then
      echo "::error file=$params::Bicep parameter build failed for $params"
      failed=1
    fi
  done
  echo "::endgroup::"
done

exit "$failed"
