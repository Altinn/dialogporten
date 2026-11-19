#!/bin/sh
# Prepares this computer for running Dialogporten locally, as README.md and docs/E2E-Tests.md ask a developer to:
#
# - an ASP.NET HTTPS development certificate, trusted system-wide, for the WebApi at https://localhost:7215 that the
#   E2E tests call;
# - a local Ed25519 key pair for signing dialog tokens, which the E2E tests verify against the WebApi's own JWKS, so
#   it needs no shared key;
# - the local database connection string, from the credentials in the repository's .env that
#   docker-compose-db-redis.yml starts Postgres with;
# - the AT23 Maskinporten client from the agent's MASKINPORTEN_AT23_* values, which act as one client: both replace
#   it in every form, either of them `none` removes every form, and anything else leaves it as it is.
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
    key="$1"
    sed -n "s/^$key=//p" "$dotenv" | tail -n 1
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
    name="$1"
    openssl genpkey -algorithm ed25519 -out "$work/$name.pem" 2>/dev/null
    private="$(openssl pkey -in "$work/$name.pem" -outform DER | tail -c 32 | b64url)"
    public="$(openssl pkey -in "$work/$name.pem" -pubout -outform DER | tail -c 32 | b64url)"
    jq -n --arg kid "dev-$name-signing-key" --arg private "$private" --arg public "$public" \
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

# The Maskinporten client's two values act as one client. Both given replace it, either of them `none` removes it, and
# anything else, such as a run through sudo, which leaves the caller's environment out, leaves it as it is. A value
# that is given but not written is named in the unit's output, never shown.
client_id="${MASKINPORTEN_AT23_CLIENT_ID:-}"
jwk="${MASKINPORTEN_AT23_ENCODED_JWK:-}"
given() { [ -n "$1" ] && [ "$1" != none ]; }
not_written() {
    echo "agent-dialogporten-dev-init: $1 is not written, since the Maskinporten client needs both values" >&2
}
client=keep
if [ "$client_id" = none ] || [ "$jwk" = none ]; then
    client=remove
elif given "$client_id" && given "$jwk"; then
    client=replace
fi
if [ "$client" != replace ]; then
    if given "$client_id"; then not_written MASKINPORTEN_AT23_CLIENT_ID; fi
    if given "$jwk"; then not_written MASKINPORTEN_AT23_ENCODED_JWK; fi
fi

# Replacing or removing the client takes every form of its two keys that .NET reads: any path whose keys, joined with
# colons, name one of them, ignoring ASCII case as .NET does, flat, nested or both. An empty object or array there reads
# as the key without a value. The replacement is written flat, since two forms of one key make the apps fail to load
# the user secrets. jq takes the pair from its environment, never its command line, which other processes can read.
if [ "$client" != keep ]; then
    jq --arg client "$client" '
        delpaths([paths((type != "object" and type != "array") or length == 0)
            | select(map(tostring) | join(":") | ascii_downcase
                | IN("infrastructure:maskinporten:clientid", "infrastructure:maskinporten:encodedjwk"))])
        | if $client == "replace" then
            . + {"Infrastructure:Maskinporten:ClientId": $ENV.MASKINPORTEN_AT23_CLIENT_ID,
                "Infrastructure:Maskinporten:EncodedJwk": $ENV.MASKINPORTEN_AT23_ENCODED_JWK}
          else . end' "$work/secrets.json" >"$work/secrets.next.json"
    mv "$work/secrets.next.json" "$work/secrets.json"
fi

install -m 0600 -o "$user" -g "$user" "$work/secrets.json" "$secrets"
