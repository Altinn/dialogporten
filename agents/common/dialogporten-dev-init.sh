#!/bin/sh
# Prepares this computer for running Dialogporten locally, as README.md and docs/E2E-Tests.md ask a developer to:
#
# - an ASP.NET HTTPS development certificate, trusted system-wide, for the WebApi at https://localhost:7215 that the
#   E2E tests call;
# - a local Ed25519 key pair for signing dialog tokens, which the E2E tests verify against the WebApi's own JWKS, so
#   it needs no shared key;
# - the local database connection string, from the credentials in the repository's .env that
#   docker-compose-db-redis.yml starts Postgres with;
# - the AT23 Maskinporten client from the agent's MASKINPORTEN_AT23_* values, unless they are `none`.
#
# They go in the user secrets every runtime project shares (UserSecretsId in Directory.Build.props). Generated values
# already there are kept, while the Maskinporten client follows the agent's values. The other AT23 credentials are
# mediated secrets in the agent's environment, as Infrastructure__Altinn__SubscriptionKey and TokenGenerator*.
#
# Runs as root at boot; each step is idempotent.
set -eu

user=agent
home=/home/agent
secrets_id=750256a4-f332-4783-8802-8a7d9566f9ca
secrets_dir="$home/.microsoft/usersecrets/$secrets_id"
secrets="$secrets_dir/secrets.json"
dotenv=/home/agent/code/dialogporten/.env

as_agent() {
    runuser -u "$user" -- env HOME="$home" DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 "$@"
}

work="$(mktemp -d)"
chown "$user:$user" "$work"
trap 'rm -rf "$work"' EXIT

# agent-workspace-init.service is `Type=exec`, so the unit's After= only orders this after the clone has started. Wait
# while it runs, so that the repository's .env is there; a clone that failed or was not needed ends the wait as well.
# Without systemd, as in the smoke test, there is no clone to wait for.
remaining=600
while systemctl is-active --quiet agent-workspace-init.service 2>/dev/null; do
    if [ "$remaining" -eq 0 ]; then
        echo "agent-dialogporten-dev-init: the workspace clone is still running after 10 minutes" >&2
        break
    fi
    remaining=$((remaining - 1))
    sleep 1
done

# The certificate lives in the agent's .NET store, and its public part in the system store that .NET's HttpClient,
# curl and the E2E tests read.
(cd "$work" && as_agent dotnet dev-certs https >/dev/null)
(cd "$work" && as_agent dotnet dev-certs https --export-path "$work/aspnet-dev.crt" --format Pem --no-password >/dev/null)
if ! cmp -s "$work/aspnet-dev.crt" /usr/local/share/ca-certificates/aspnet-dev.crt; then
    install -m 0644 "$work/aspnet-dev.crt" /usr/local/share/ca-certificates/aspnet-dev.crt
    update-ca-certificates >/dev/null
fi

# The value of a plain KEY=value line in the repository's .env, read rather than sourced, since this runs as root.
dotenv_value() {
    sed -n "s/^$1=//p" "$dotenv" | tail -n 1
}

# Without the clone, as when the workspace could not be cloned, the connection string is left as it is.
connection=
if [ -r "$dotenv" ]; then
    connection="Server=localhost;Port=15432;Database=$(dotenv_value POSTGRES_DB);User ID=$(dotenv_value POSTGRES_USER)"
    connection="$connection;Password=$(dotenv_value POSTGRES_PASSWORD);Include Error Detail=True;"
else
    echo "agent-dialogporten-dev-init: no $dotenv, so no database connection string" >&2
fi

b64url() { base64 -w0 | tr '+/' '-_' | tr -d '='; }

keypair() {
    openssl genpkey -algorithm ed25519 -out "$work/$1.pem" 2>/dev/null
    private="$(openssl pkey -in "$work/$1.pem" -outform DER | tail -c 32 | b64url)"
    public="$(openssl pkey -in "$work/$1.pem" -pubout -outform DER | tail -c 32 | b64url)"
    jq -n --arg kid "dev-$1-signing-key" --arg private "$private" --arg public "$public" \
        '{Kid: $kid, PrivateComponent: $private, PublicComponent: $public}'
}

install -d -m 0700 -o "$user" -g "$user" "$home/.microsoft" "$home/.microsoft/usersecrets" "$secrets_dir"
[ -s "$secrets" ] || echo '{}' >"$secrets"

# Later values win in jq's object addition, so the existing secrets go last.
jq --arg connection "$connection" \
    --argjson primary "$(keypair primary)" \
    --argjson secondary "$(keypair secondary)" \
    '(if $connection == "" then {} else {"Infrastructure:DialogDbConnectionString": $connection} end) + {
        "Application:Dialogporten:Ed25519KeyPairs:Primary:Kid": $primary.Kid,
        "Application:Dialogporten:Ed25519KeyPairs:Primary:PrivateComponent": $primary.PrivateComponent,
        "Application:Dialogporten:Ed25519KeyPairs:Primary:PublicComponent": $primary.PublicComponent,
        "Application:Dialogporten:Ed25519KeyPairs:Secondary:Kid": $secondary.Kid,
        "Application:Dialogporten:Ed25519KeyPairs:Secondary:PrivateComponent": $secondary.PrivateComponent,
        "Application:Dialogporten:Ed25519KeyPairs:Secondary:PublicComponent": $secondary.PublicComponent
    } + .' "$secrets" >"$work/secrets.json"

set_secret() {
    jq --arg key "$1" --arg value "$2" '.[$key] = $value' "$work/secrets.json" >"$work/secrets.next.json"
    mv "$work/secrets.next.json" "$work/secrets.json"
}
client_id="${MASKINPORTEN_AT23_CLIENT_ID:-none}"
jwk="${MASKINPORTEN_AT23_ENCODED_JWK:-none}"
[ "$client_id" = none ] || set_secret Infrastructure:Maskinporten:ClientId "$client_id"
[ "$jwk" = none ] || set_secret Infrastructure:Maskinporten:EncodedJwk "$jwk"

install -m 0600 -o "$user" -g "$user" "$work/secrets.json" "$secrets"
