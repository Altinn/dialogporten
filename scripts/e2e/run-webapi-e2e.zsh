#!/usr/bin/env zsh
set -euo pipefail

# Clean up function
cleanup() {
  if [[ -n "${webapi_pid:-}" ]] && kill -0 "$webapi_pid" >/dev/null 2>&1; then
    kill "$webapi_pid" >/dev/null 2>&1 || true
    wait "$webapi_pid" >/dev/null 2>&1 || true
  fi
}

# Run cleanup when script exit or terminates
trap cleanup EXIT INT TERM

script_dir="$(cd "$(dirname "$0")" && pwd)"

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
# Read .env file
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

# fix Path
webapi_log="${script_dir}/dialogporten-webapi-e2e.log"

export DOTNET_ENVIRONMENT="${WEBAPI_ENVIRONMENT}"
export ASPNETCORE_ENVIRONMENT="${WEBAPI_ENVIRONMENT}"
export DialogportenBaseUri="${DialogportenBaseUri:-https://localhost}"
export WebApiPort="${1:-7215}"
export WebApiUrl="${DialogportenBaseUri}:${WebApiPort}"

echo "Starting Dialogporten WebAPI on ${WebApiUrl}..."
dotnet run \
  --project "$repo_root/src/Digdir.Domain.Dialogporten.WebApi/Digdir.Domain.Dialogporten.WebApi.csproj" \
  --environment "${WEBAPI_ENVIRONMENT}" \
  --urls "$WebApiUrl" \
  >"$webapi_log" 2>&1 &
webapi_pid=$!
echo "WebApi PID: $webapi_pid"

# Waiting for WebAPI to start
echo "Waiting for WebAPI to respond..."

start_time=$(date +%s)
timeout_seconds=60
echo "Timeout $timeout_seconds secounds"
while true; do
  if curl -k -s -o /dev/null -I "$WebApiUrl"; then
    break
  fi
  now=$(date +%s)
  time_ran=$((now - start_time))
  echo "$((timeout_seconds - time_ran )) Seconds Remaining"

  if (( now - start_time > timeout_seconds )); then
    echo "Timed out waiting for WebAPI. Logs:"
    tail -n 200 "$webapi_log" || true
    exit 1
  fi

  sleep 1
done

# Run tests
echo "Running WebAPI E2E tests..."
dotnet test "$repo_root/tests/Digdir.Domain.Dialogporten.WebAPI.E2E.Tests/Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.csproj" -- xUnit.Explicit=on
