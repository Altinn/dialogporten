#!/usr/bin/env bash
# Smoke-tests what the Dialogporten agent image adds to the agentctl base image, inside a freshly built image. The
# base image and its features are tested where they are built, in digdir/digdir-agents.
#
#   docker run --rm --entrypoint bash -v "$PWD/agents/smoke.sh:/smoke.sh:ro" -v "$PWD/global.json:/global.json:ro" \
#     <image> /smoke.sh
#
# Runs as the image's `agent` user, which has passwordless sudo.
set -euo pipefail

work="$(mktemp -d)"
cd "$work"
fail() { echo "smoke: $*" >&2; exit 1; }

echo "## versions"
[[ "$(id -un)" == agent ]] || fail "expected to run as agent, got $(id -un)"
dotnet --list-sdks
sdk="$(sed -n 's/.*"version": *"\([^"]*\)".*/\1/p' /global.json)"
[[ -n "$sdk" ]] || fail "no SDK version in /global.json"
dotnet --list-sdks | grep -q "^${sdk//./\\.} " || fail "the .NET SDK $sdk that global.json pins is missing"
dotnet-ef --version | tail -1
zsh --version
psql --version
podman --version
/usr/libexec/docker/cli-plugins/docker-compose version

echo "## container storage"
# Podman itself cannot start in the unprivileged container this runs in, so check its configuration.
storage=/etc/containers/storage.conf
[[ "$(sed -n 's/^driver = "\(.*\)"$/\1/p' "$storage")" == overlay ]] \
    || fail "Podman's storage driver is not overlay"
! grep -q '^mount_program' "$storage" \
    || fail "Podman's overlay storage still uses a mount program"

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
# The unit runs after the workspace clone and takes the database credentials from the repository's .env.
clone=/home/agent/code/dialogporten
[[ ! -e "$clone" ]] || fail "$clone exists: run this in a freshly built image, not in a booted agent"
sudo install -d -o agent -g agent /home/agent/code "$clone"
printf '%s\n' POSTGRES_USER=smoke-user POSTGRES_PASSWORD=smoke-password POSTGRES_DB=smoke-db >"$clone/.env"
sudo env MASKINPORTEN_AT23_CLIENT_ID=none MASKINPORTEN_AT23_ENCODED_JWK=none \
    /usr/local/libexec/agent-dialogporten-dev-init
[[ "$(jq -r '."Infrastructure:DialogDbConnectionString"' "$secrets")" == \
    'Server=localhost;Port=15432;Database=smoke-db;User ID=smoke-user;Password=smoke-password;Include Error Detail=True;' ]] \
    || fail "the connection string from the repository's .env is not in the user secrets"
jq -e 'has("Infrastructure:Maskinporten:ClientId") | not' "$secrets" >/dev/null \
    || fail "a Maskinporten client of none was written to the user secrets"
private="$(jq -r '."Application:Dialogporten:Ed25519KeyPairs:Primary:PrivateComponent"' "$secrets")"
[[ "${#private}" -eq 43 ]] || fail "the generated Ed25519 private key is not 32 bytes in base64url"

# A second run keeps the generated pair and writes the Maskinporten client it is given.
sudo env MASKINPORTEN_AT23_CLIENT_ID=smoke-client MASKINPORTEN_AT23_ENCODED_JWK=smoke-jwk \
    /usr/local/libexec/agent-dialogporten-dev-init
[[ "$(jq -r '."Application:Dialogporten:Ed25519KeyPairs:Primary:PrivateComponent"' "$secrets")" == "$private" ]] \
    || fail "a second run replaced the generated key pair"
[[ "$(jq -r '."Infrastructure:Maskinporten:ClientId"' "$secrets")" == smoke-client ]] \
    || fail "the Maskinporten client was not written to the user secrets"
[[ "$(jq -r '."Infrastructure:Maskinporten:EncodedJwk"' "$secrets")" == smoke-jwk ]] \
    || fail "the Maskinporten key was not written to the user secrets"

# A run without the values, as through sudo, which leaves the caller's environment out, keeps the client.
sudo env -u MASKINPORTEN_AT23_CLIENT_ID -u MASKINPORTEN_AT23_ENCODED_JWK /usr/local/libexec/agent-dialogporten-dev-init
jq -e '."Infrastructure:Maskinporten:ClientId" == "smoke-client"
    and ."Infrastructure:Maskinporten:EncodedJwk" == "smoke-jwk"' "$secrets" >/dev/null \
    || fail "a run without the Maskinporten values changed the client in the user secrets"

# A run with `none` again removes the client and keeps the generated pair.
sudo env MASKINPORTEN_AT23_CLIENT_ID=none MASKINPORTEN_AT23_ENCODED_JWK=none \
    /usr/local/libexec/agent-dialogporten-dev-init
jq -e 'has("Infrastructure:Maskinporten:ClientId") or has("Infrastructure:Maskinporten:EncodedJwk") | not' \
    "$secrets" >/dev/null || fail "a Maskinporten client of none left the earlier client in the user secrets"
[[ "$(jq -r '."Application:Dialogporten:Ed25519KeyPairs:Primary:PrivateComponent"' "$secrets")" == "$private" ]] \
    || fail "a run with a Maskinporten client of none replaced the generated key pair"

# The two values act as one client. Each row below gives the client ID and the key, each a value, `none`, empty or
# unset; the client the user secrets start with, the agent's earlier pair or forms of it set by hand; what the run
# must leave, the given pair written flat, no client, or the client it started with; and the value the unit must name
# as not written, or `-`.
without_client="$(jq -c . "$secrets")"
earlier='{"Infrastructure:Maskinporten:ClientId": "smoke-client",
    "Infrastructure:Maskinporten:EncodedJwk": "smoke-jwk"}'
# Forms .NET reads as the same two keys, flat in other casing, mixed and nested, beside settings that must stay.
hand_set='{"infrastructure:maskinporten:clientid": "hand-client",
    "Infrastructure:Maskinporten": {"clientId": "hand-client"},
    "Infrastructure": {"Maskinporten": {"EncodedJwk": "hand-jwk", "Scope": "smoke-scope"}},
    "Infrastructure:Maskinporten:ClientIdOld": "smoke-near"}'
hand_set_others='["Infrastructure","Maskinporten","Scope"]=smoke-scope
["Infrastructure:Maskinporten:ClientIdOld"]=smoke-near'
# The Maskinporten settings in the user secrets, a line per path .NET reads, so that their forms can be told apart.
maskinporten_settings() {
    jq -r 'paths(type != "object" and type != "array") as $p
        | select($p | map(tostring) | join(":") | ascii_downcase | startswith("infrastructure:maskinporten:"))
        | "\($p | tojson)=\(getpath($p))"' "$secrets" | LC_ALL=C sort
}
value() { [[ "$1" == empty ]] || printf '%s' "$1"; }
# A jq that records its command line, which other processes can read, first on the unit's PATH.
mkdir "$work/bin"
cat >"$work/bin/jq" <<EOF
#!/bin/sh
printf '%s\n' "\$*" >>"$work/jq-args"
exec $(command -v jq) "\$@"
EOF
chmod +x "$work/bin/jq"
unit_path="$work/bin:$(sudo printenv PATH)"
while read -r client_id jwk start leaves named; do
    row="client ID $client_id and key $jwk on the $start client"
    fixture="$earlier" others=''
    [[ "$start" == earlier ]] || fixture="$hand_set" others="$hand_set_others"
    jq -n --argjson base "$without_client" --argjson fixture "$fixture" '$base + $fixture' >"$secrets"
    before="$(maskinporten_settings)"
    assignments=()
    [[ "$client_id" == unset ]] || assignments+=("MASKINPORTEN_AT23_CLIENT_ID=$(value "$client_id")")
    [[ "$jwk" == unset ]] || assignments+=("MASKINPORTEN_AT23_ENCODED_JWK=$(value "$jwk")")
    : >"$work/jq-args"
    {
        sudo env -u MASKINPORTEN_AT23_CLIENT_ID -u MASKINPORTEN_AT23_ENCODED_JWK PATH="$unit_path" \
            "${assignments[@]}" /usr/local/libexec/agent-dialogporten-dev-init </dev/null
    } >"$work/out" 2>"$work/err" || { cat "$work/err" >&2; fail "$row: the unit failed"; }
    case "$leaves" in
        pair) expected="$(printf '%s\n' "$others" "[\"Infrastructure:Maskinporten:ClientId\"]=$client_id" \
            "[\"Infrastructure:Maskinporten:EncodedJwk\"]=$jwk" | sed '/^$/d' | LC_ALL=C sort)" ;;
        none) expected="$others" ;;
        kept) expected="$before" ;;
    esac
    [[ "$(maskinporten_settings)" == "$expected" ]] \
        || fail "$row: the user secrets do not hold what the run must leave, $leaves"
    [[ "$(sed -n 's/^agent-dialogporten-dev-init: \(MASKINPORTEN_AT23_[A-Z_]*\) is not written.*/\1/p' "$work/err")" \
        == "${named#-}" ]] || fail "$row: the unit did not name exactly the value it did not write, $named"
    ! grep -qF -e smoke-client-2 -e smoke-jwk-2 "$work/out" "$work/err" || fail "$row: the unit printed a value"
    ! grep -qF -e smoke-client-2 -e smoke-jwk-2 "$work/jq-args" || fail "$row: a value was on jq's command line"
    [[ "$(jq -r '."Application:Dialogporten:Ed25519KeyPairs:Primary:PrivateComponent"' "$secrets")" == "$private" ]] \
        || fail "$row: the run replaced the generated key pair"
done <<'EOF'
smoke-client-2 none earlier none MASKINPORTEN_AT23_CLIENT_ID
none smoke-jwk-2 earlier none MASKINPORTEN_AT23_ENCODED_JWK
smoke-client-2 unset earlier kept MASKINPORTEN_AT23_CLIENT_ID
unset smoke-jwk-2 earlier kept MASKINPORTEN_AT23_ENCODED_JWK
smoke-client-2 empty earlier kept MASKINPORTEN_AT23_CLIENT_ID
none unset earlier none -
unset none earlier none -
empty none earlier none -
none none hand-set none -
smoke-client-2 smoke-jwk-2 hand-set pair -
EOF

echo "## dev certificate"
# Against the system store itself: a container runtime may point SSL_CERT_FILE at a CA bundle it copied when the
# container started, before the boot unit added the certificate, as the agent's own Podman does.
openssl verify -CAfile /etc/ssl/certs/ca-certificates.crt /usr/local/share/ca-certificates/aspnet-dev.crt \
    || fail "the HTTPS dev certificate is not trusted system-wide"
dotnet dev-certs https --check || fail "the agent has no HTTPS dev certificate"

echo "smoke: ok"
