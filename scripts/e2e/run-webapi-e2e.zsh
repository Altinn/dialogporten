#!/usr/bin/env zsh
set -euo pipefail

script_dir="$(cd "$(dirname "$0")" && pwd)"

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

loadEnv() {
  env_file=""
  if [[ -n "${ENV_FILE:-}" ]]; then
    env_file="$ENV_FILE"
  elif [[ -f "$script_dir/.env" ]]; then
    env_file="$script_dir/.env"
  fi

  if [[ -z "${WEBAPI_ENVIRONMENT:-}" ]]; then
      echo "WEBAPI_ENVIRONMENT not found, using Development as default"
      WEBAPI_ENVIRONMENT=Development
  fi

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
      if [[ -e "$repo_root/Digdir.Domain.Dialogporten.slnx" ]]; then
        echo "Repo root found: $repo_root/Digdir.Domain.Dialogporten.slnx"
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
  local engine=""
  local compose_file="$repo_root/docker-compose-db-redis.yml"
  local postgres_service="dialogporten-postgres"
  local redis_service="dialogporten-redis"
  local postgres_container="dialogporten-postgres-1"
  local redis_container="dialogporten-redis-1"
  local timeout_seconds=60
  local poll_interval_seconds=1

  if command -v podman >/dev/null 2>&1; then
    engine="podman"
  elif command -v docker >/dev/null 2>&1; then
    engine="docker"
  else
    echo "ERROR: Neither podman nor docker is installed." >&2
    exit 1
  fi

  resolve_container_ref() {
    local service_name="$1"
    local preferred_name="$2"
    local container_ref=""

    if "$engine" inspect "$preferred_name" >/dev/null 2>&1; then
      echo "$preferred_name"
      return
    fi

    container_ref=$("$engine" compose -f "$compose_file" ps -q "$service_name" 2>/dev/null | head -n 1 || true)
    if [[ -n "$container_ref" ]]; then
      echo "$container_ref"
      return
    fi

    container_ref=$("$engine" ps -a --filter "name=$service_name" --format '{{.ID}}' 2>/dev/null | head -n 1 || true)
    if [[ -n "$container_ref" ]]; then
      echo "$container_ref"
      return
    fi

    echo ""
  }

  is_container_running() {
    local container_ref="$1"
    local running_state=""
    if [[ -z "$container_ref" ]]; then
      return 1
    fi
    running_state=$("$engine" inspect --format '{{.State.Running}}' "$container_ref" 2>/dev/null || true)
    [[ "$running_state" == "true" ]]
  }

  local postgres_running=0
  local redis_running=0
  local container_log="${script_dir}/dialogporten-podman.log"

  postgres_container=$(resolve_container_ref "$postgres_service" "$postgres_container")
  redis_container=$(resolve_container_ref "$redis_service" "$redis_container")

  if is_container_running "$postgres_container"; then
    postgres_running=1
  fi
  if is_container_running "$redis_container"; then
    redis_running=1
  fi

  if (( postgres_running == 1 && redis_running == 1 )); then
    echo "Postgres container is running: ${postgres_container:-not-found}"
    echo "Redis container is running: ${redis_container:-not-found}"
    return
  fi

  echo "Starting db/redis with $engine compose (missing containers detected)..."
  "$engine" compose -f "$compose_file" up -d > "$container_log" 2>&1

  postgres_container=$(resolve_container_ref "$postgres_service" "dialogporten-postgres-1")
  redis_container=$(resolve_container_ref "$redis_service" "dialogporten-redis-1")

  local started_at
  started_at=$(date +%s)
  echo "Waiting up to ${timeout_seconds}s for containers to be running..."
  while true; do
    postgres_running=0
    redis_running=0
    if is_container_running "$postgres_container"; then
      postgres_running=1
    fi
    if is_container_running "$redis_container"; then
      redis_running=1
    fi

    if (( postgres_running == 1 && redis_running == 1 )); then
      break
    fi

    local elapsed=$(( $(date +%s) - started_at ))
    local remaining=$(( timeout_seconds - elapsed ))
    if (( remaining < 0 )); then
      remaining=0
    fi
    echo "Still waiting: postgres=${postgres_running} (${postgres_container:-not-found}), redis=${redis_running} (${redis_container:-not-found}), elapsed=${elapsed}s, remaining=${remaining}s"
    if (( elapsed >= timeout_seconds )); then
      echo "ERROR: Timed out after ${timeout_seconds}s waiting for containers to be running." >&2
      echo "Postgres running: $postgres_running (${postgres_container:-not-found})" >&2
      echo "Redis running: $redis_running (${redis_container:-not-found})" >&2
      echo "Debug commands:" >&2
      echo "  $engine compose -f \"$compose_file\" ps" >&2
      echo "  $engine ps -a --filter \"name=$postgres_service\"" >&2
      echo "  $engine ps -a --filter \"name=$redis_service\"" >&2
      echo "See log: $container_log" >&2
      exit 1
    fi

    echo "Sleeping ${poll_interval_seconds}s before next check..."
    sleep "$poll_interval_seconds"

    postgres_container=$(resolve_container_ref "$postgres_service" "${postgres_container:-dialogporten-postgres-1}")
    redis_container=$(resolve_container_ref "$redis_service" "${redis_container:-dialogporten-redis-1}")
  done

  echo "Postgres container is running: ${postgres_container:-not-found}"
  echo "Redis container is running: ${redis_container:-not-found}"
}

trap cleanup EXIT INT TERM

loadEnv
setRepoPath

webapi_log="${script_dir}/dialogporten-webapi-e2e.log"
graphql_log="${script_dir}/dialogporten-graphql-e2e.log"

podman_check

usage() {
  echo "Usage: $0 [webapi|graphql|both] [--doNotRunTests]" >&2
  exit 1
}

mode="both"
doNotRunTests=0

while [[ $# -gt 0 ]]; do
  case "$1" in
    webapi|graphql|both)
      mode="$1"
      shift
      ;;
    --doNotRunTests)
      doNotRunTests=1
      shift
      ;;
    *)
      usage
      ;;
  esac
done

export DOTNET_ENVIRONMENT="${WEBAPI_ENVIRONMENT}"
export ASPNETCORE_ENVIRONMENT="${WEBAPI_ENVIRONMENT}"
export DialogportenBaseUri="${DialogportenBaseUri:-https://localhost}"
export WebApiPort="${WEBAPI_PORT:-7215}" # Default port should not colide with default port of APIs
export GraphQlPort="${GRAPHQL_PORT:-5180}"
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

if [[ $doNotRunTests -eq 0 ]]; then
  if [[ "$mode" == "webapi" || "$mode" == "both" ]]; then
    echo "Running WebAPI E2E tests..."
    dotnet test "$repo_root/tests/Digdir.Domain.Dialogporten.WebAPI.E2E.Tests/Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.csproj" -- xUnit.Explicit=on
  fi

  if [[ "$mode" == "graphql" || "$mode" == "both" ]]; then
    echo "Running GraphQL E2E tests..."
    dotnet test "$repo_root/tests/Digdir.Domain.Dialogporten.GraphQl.E2E.Tests/Digdir.Domain.Dialogporten.GraphQl.E2E.Tests.csproj" -- xUnit.Explicit=on
  fi
else
  echo "Skipping tests as requested. Services are running."
  echo "Press any key to terminate the services and exit..."
  read -k 1 -s
fi
