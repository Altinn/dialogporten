#!/usr/bin/env bash
# Checks which release the Dialogporten apps of an environment run. Each app returns its
# image tag on a version endpoint under a path that the edge forwards to it: the service
# owner web API, the end user web API and GraphQL.
#
# Inputs (env):
#   BASE_URL           public Dialogporten URL, for example https://platform.at23.altinn.cloud/dialogporten
#   EXPECTED_VERSION   image tag of the release, for example 1.126.7-a3df68f
#   WAIT_SECONDS       how long to retry until every app runs it (default 0: one try)
# Writes current=true to $GITHUB_OUTPUT when every app runs EXPECTED_VERSION, and
# current=false when an app runs another version. Fails when an app gives no version.
set -euo pipefail

paths=(api/v1/serviceowner/version api/v1/enduser/version graphql/version)
deadline=$((SECONDS + ${WAIT_SECONDS:-0}))

while :; do
  versions=()
  current=true
  answered=true
  for path in "${paths[@]}"; do
    version=$(curl --silent --fail --max-time 10 "${BASE_URL}/${path}") || version=""
    # The body is an image tag. Keep it on one short line, since the log prints it.
    version=${version//[[:space:]]/}
    version=${version:0:64}
    versions+=("${version}")
    if [ -z "${version}" ]; then
      answered=false
    fi
    if [ "${version}" != "${EXPECTED_VERSION}" ]; then
      current=false
    fi
  done
  if [ "${current}" = true ] || [ "${SECONDS}" -ge "${deadline}" ]; then
    break
  fi
  sleep 10
done

for i in "${!paths[@]}"; do
  echo "/${paths[i]}: ${versions[i]:-no answer}"
done

if [ "${answered}" = false ]; then
  echo "::error::An app gave no version, so the running release is unknown."
  exit 1
fi
if [ "${current}" = false ]; then
  echo "::notice::The apps do not all run ${EXPECTED_VERSION}, so the result is stale."
fi
echo "current=${current}" >> "${GITHUB_OUTPUT}"
