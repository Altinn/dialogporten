#!/usr/bin/env bash
# Smoke-tests what the Dialogporten agent image adds to the agentctl base image, inside a freshly built image. The
# base image and its features are tested where they are built, in digdir/digdir-agents.
#
#   docker run --rm --entrypoint bash -v "$PWD/agents/smoke.sh:/smoke.sh:ro" <image> /smoke.sh
#
# Runs as the image's `agent` user, which has passwordless sudo.
set -euo pipefail

work="$(mktemp -d)"
cd "$work"
fail() { echo "smoke: $*" >&2; exit 1; }

echo "## versions"
test "$(id -un)" = agent || fail "expected to run as agent, got $(id -un)"
dotnet --list-sdks
dotnet --list-sdks | grep -q '^10\.0\.402 ' || fail "the .NET SDK global.json pins is missing"
dotnet-ef --version | tail -1
zsh --version
psql --version
podman --version
/usr/libexec/docker/cli-plugins/docker-compose version
agentctl --version
agentd --version

echo "## container storage"
# Fails with "'overlay' is not supported over overlayfs" when the storage configuration is missing.
test "$(sudo podman info --format '{{.Store.GraphDriverName}}')" = overlay \
    || fail "Podman cannot configure its overlay storage"

echo "## timezone"
case "$(date +%Z)" in
    CET | CEST) ;;
    *) fail "system clock is $(date +%Z), expected Norwegian local time" ;;
esac

echo "## boot unit"
systemctl is-enabled agent-dialogporten-dev-init.service >/dev/null \
    || fail "the Dialogporten development unit is not enabled"
grep -qxF 'PassEnvironment=MASKINPORTEN_AT23_CLIENT_ID MASKINPORTEN_AT23_ENCODED_JWK' \
    /etc/systemd/system/agent-dialogporten-dev-init.service \
    || fail "the development unit does not receive the Maskinporten values"

secrets=/home/agent/.microsoft/usersecrets/750256a4-f332-4783-8802-8a7d9566f9ca/secrets.json
sudo env MASKINPORTEN_AT23_CLIENT_ID=none MASKINPORTEN_AT23_ENCODED_JWK=none \
    /usr/local/libexec/agent-dialogporten-dev-init
jq -e '."Infrastructure:DialogDbConnectionString" | contains("Port=15432")' "$secrets" >/dev/null \
    || fail "the connection string is not in the user secrets"
jq -e 'has("Infrastructure:Maskinporten:ClientId") | not' "$secrets" >/dev/null \
    || fail "a Maskinporten client of none was written to the user secrets"
private="$(jq -r '."Application:Dialogporten:Ed25519KeyPairs:Primary:PrivateComponent"' "$secrets")"
test "${#private}" -eq 43 || fail "the generated Ed25519 private key is not 32 bytes in base64url"

# A second run keeps the generated pair and writes the Maskinporten client it is given.
sudo env MASKINPORTEN_AT23_CLIENT_ID=smoke-client MASKINPORTEN_AT23_ENCODED_JWK=smoke-jwk \
    /usr/local/libexec/agent-dialogporten-dev-init
test "$(jq -r '."Application:Dialogporten:Ed25519KeyPairs:Primary:PrivateComponent"' "$secrets")" = "$private" \
    || fail "a second run replaced the generated key pair"
test "$(jq -r '."Infrastructure:Maskinporten:ClientId"' "$secrets")" = smoke-client \
    || fail "the Maskinporten client was not written to the user secrets"

echo "## dev certificate"
openssl verify /usr/local/share/ca-certificates/aspnet-dev.crt \
    || fail "the HTTPS dev certificate is not trusted system-wide"
dotnet dev-certs https --check || fail "the agent has no HTTPS dev certificate"

echo "smoke: ok"
