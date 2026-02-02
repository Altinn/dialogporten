#!/usr/bin/env zsh
set -euo pipefail

script_dir="$(cd "$(dirname "$0")" && pwd)"
# Clean up function
cleanup() {
  if [[ -n "${webapi_pid:-}" ]] && kill -0 "$webapi_pid" >/dev/null 2>&1; then
    kill "$webapi_pid" >/dev/null 2>&1 || true
    wait "$webapi_pid" >/dev/null 2>&1 || true
  fi
  if [[ -n "${graphql_pid:-}" ]] && kill -0 "$graphql_pid" >/dev/null 2>&1; then
    kill "$graphql_pid" >/dev/null 2>&1 || true
    wait "$graphql_pid" >/dev/null 2>&1 || true
  fi
}

# Read .env file
loadEnv() {
  env_file=""
  if [[ -n "${ENV_FILE:-}" ]]; then
    env_file="$ENV_FILE"
  elif [[ -f "$script_dir/.env" ]]; then
    env_file="$script_dir/.env"
  fi
  
  # Load env file
  if [[ -n "$env_file" ]]; then
    echo "Loading environment from $env_file"
    set -a
    source "$env_file"
    set +a
  fi
}

setRepoPath() {
if [[ -n "${DIALOGPORTEN:-}" ]]; then
  repo_root="$DIALOGPORTEN"
else
  echo "DIALOGPORTEN not set, searching for repo root...."
  repo_root="$script_dir"   # start from script location
  depth=0
  found=0
  while [[ $repo_root != "/" && $depth -lt 5 ]]; do
    if [[ -e "$repo_root/Digdir.Domain.Dialogporten.sln" ]]; then
      echo "Repo root found: $repo_root/Digdir.Domain.Dialogporten.sln"
      found=1
      break
    fi
    repo_root=${repo_root:h}
    ((++depth))
  done
  if [[ $found -ne 1 ]]; then
    echo "ERROR: Repo root not found within 5 levels from $script_dir" >&2
    exit 1
  fi
fi
}

podman_check() {
  postgre_run=$(podman ps -f "status=running" -f name=postgres -q)
  redis_run=$(podman ps -f "status=running" -f name=redis -q)
  podman_log="${script_dir}/dialogporten-podman.log"
  if [[ -n "${postgre_run:-}" && -n "${redis_run:-}" ]]; then
    echo "postgre is running"
    echo "redis is running"
    return
  fi
  
  podman compose -f $repo_root/docker-compose-db-redis.yml up > "$podman_log" 2>&1 &
}
# Run cleanup when script exit or terminates
trap cleanup EXIT INT TERM

setRepoPath
loadEnv

# fix Path
webapi_log="${script_dir}/dialogporten-webapi-e2e.log"
graphql_log="${script_dir}/dialogporten-graphql-e2e.log"

podman_check

mode="${1:-both}"
case "$mode" in
  webapi|graphql|both) ;;
  *)
    echo "Usage: $0 [webapi|graphql|both]" >&2
    exit 1
    ;;
esac

export DOTNET_ENVIRONMENT="${WEBAPI_ENVIRONMENT}"
export ASPNETCORE_ENVIRONMENT="${WEBAPI_ENVIRONMENT}"
export DialogportenBaseUri="${DialogportenBaseUri:-https://localhost}"
export WebApiPort="${WEBAPI_PORT:-7215}"
export GraphQlPort="${GRAPHQL_PORT:-5181}"
export WebApiUrl="${DialogportenBaseUri}:${WebApiPort}"
export GraphQlBaseUrl="http://localhost:${GraphQlPort}"
export GraphQlUrl="${GraphQlBaseUrl}/graphql"

wait_for_url() {
  local url="$1"
  local name="$2"
  local log_file="$3"

  echo "Waiting for $name to respond at $url..."
  local start_time
  start_time=$(date +%s)
  local timeout_seconds=60
  echo "Timeout $timeout_seconds seconds"
  while true; do
    if curl -k -s -o /dev/null -I "$url"; then
      break
    fi

    local time_ran=$(( $(date +%s) - start_time ))
    echo "$((timeout_seconds - time_ran )) Seconds Remaining"

    if (( time_ran > timeout_seconds )); then
      echo "Timed out waiting for $name. Logs:"
      tail -n 200 "$log_file" || true
      exit 1
    fi
    sleep 1
  done
}

start_webapi() {
  echo "Starting Dialogporten WebAPI on ${WebApiUrl}..."
  dotnet run \
    --project "$repo_root/src/Digdir.Domain.Dialogporten.WebApi/Digdir.Domain.Dialogporten.WebApi.csproj" \
    --environment "${WEBAPI_ENVIRONMENT}" \
    --urls "$WebApiUrl" \
    >"$webapi_log" 2>&1 &
  webapi_pid=$!
  echo "WebApi PID: $webapi_pid"
  wait_for_url "$WebApiUrl" "WebAPI" "$webapi_log"
}

start_graphql() {
  echo "Starting Dialogporten GraphQL on ${GraphQlBaseUrl}..."
  dotnet run \
    --project "$repo_root/src/Digdir.Domain.Dialogporten.GraphQL/Digdir.Domain.Dialogporten.GraphQL.csproj" \
    --environment "${WEBAPI_ENVIRONMENT}" \
    --urls "$GraphQlBaseUrl" \
    >"$graphql_log" 2>&1 &
  graphql_pid=$!
  echo "GraphQL PID: $graphql_pid"
  wait_for_url "$GraphQlUrl" "GraphQL" "$graphql_log"
}

#  Always start WebApi
start_webapi

if [[ "$mode" == "graphql" || "$mode" == "both" ]]; then
  start_graphql
fi

if [[ "$mode" == "webapi" || "$mode" == "both" ]]; then
  echo "Running WebAPI E2E tests..."
  dotnet test "$repo_root/tests/Digdir.Domain.Dialogporten.WebAPI.E2E.Tests/Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.csproj" -- xUnit.Explicit=on
fi

if [[ "$mode" == "graphql" || "$mode" == "both" ]]; then
  echo "Running GraphQL E2E tests..."
  dotnet test "$repo_root/tests/Digdir.Domain.Dialogporten.GraphQl.E2E.Tests/Digdir.Domain.Dialogporten.GraphQl.E2E.Tests.csproj" -- xUnit.Explicit=on
fi
