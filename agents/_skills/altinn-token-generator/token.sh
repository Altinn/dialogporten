#!/usr/bin/env bash
# Mints Altinn test tokens from the test token generator, which AT23, TT02 and YT01 trust as Altinn-issued.
#
#   token.sh <kind> [param=value ...]    prints the JWT
#   token.sh e2e <preset>                prints the token the E2E tests use for <preset>
#   token.sh claims <any of the above>   prints the token's claims as JSON instead
#
# Kinds: personal, enterprise, systemuser, enterpriseuser, selfidentified, platform, platformaccess.
# Presets: enduser, alt-enduser, serviceowner, serviceowner-admin, systemuser, alt-systemuser.
# Params are the generator's own query parameters (pid, scopes, org, orgNo, systemUserId, systemUserOrg, userId,
# partyId, partyuuid, authLvl, ...). env defaults to $ALTINN_ENV or at23, ttl to 1800 seconds.
# Needs TokenGeneratorUser and TokenGeneratorPassword in the environment.
set -euo pipefail

base=https://altinn-testtools-token-generator.azurewebsites.net/api

usage() { sed -n '2,12p' "$0" | sed 's/^# \{0,1\}//' >&2; exit 2; }

declare -A endpoints=(
    [personal]=GetPersonalToken [enterprise]=GetEnterpriseToken [systemuser]=GetSystemUserToken
    [enterpriseuser]=GetEnterpriseUserToken [selfidentified]=GetSelfIdentifiedUserToken
    [platform]=GetPlatformToken [platformaccess]=GetPlatformAccessToken
)

# The presets mirror tests/Digdir.Library.Dialogporten.E2E.Common/E2EConstants.cs, which picks the service owner's
# orgNo and the system users by environment. Presets it has no identity for are left out.
env="${ALTINN_ENV:-at23}"
case "$env" in
    at23)
        so_org_no=991825827
        system_user=51551dd0-b9c5-4f4b-a4a2-9b7e3669b364 alt_system_user=54cb69df-3d8a-4432-804e-742e40de6211 ;;
    tt02)
        so_org_no=991825827
        system_user=7633be4c-dbd5-49f4-bbc4-c058b275de7b alt_system_user=96a46590-1c2a-4e46-a7e7-975f40b985ef ;;
    yt01) so_org_no=713431400 system_user='' alt_system_user='' ;;
    *) so_org_no='' system_user='' alt_system_user='' ;;
esac
so_scopes='digdir:dialogporten.serviceprovider digdir:dialogporten.serviceprovider.search'
declare -A presets=(
    [enduser]='personal|pid=08844397713|scopes=digdir:dialogporten'
    [alt-enduser]='personal|pid=13838599936|scopes=digdir:dialogporten'
)
if [[ -n "$so_org_no" ]]; then
    presets[serviceowner]="enterprise|org=ttd|orgNo=$so_org_no|scopes=$so_scopes"
    presets[serviceowner-admin]="enterprise|org=ttd|orgNo=$so_org_no"
    presets[serviceowner-admin]+="|scopes=$so_scopes digdir:dialogporten.serviceprovider.admin"
fi
if [[ -n "$system_user" ]]; then
    presets[systemuser]="systemuser|systemUserId=$system_user|systemUserOrg=310057223|scopes=digdir:dialogporten"
    presets[alt-systemuser]="systemuser|systemUserId=$alt_system_user|systemUserOrg=313006425"
    presets[alt-systemuser]+="|scopes=digdir:dialogporten"
fi

mint() {
    local kind="$1"; shift
    [[ -n "${endpoints[$kind]:-}" ]] || { echo "token.sh: unknown kind '$kind'" >&2; usage; }
    local -a args=(--get --silent --show-error --write-out '\n%{http_code}'
        --user "${TokenGeneratorUser:?TokenGeneratorUser is not set}:${TokenGeneratorPassword:?TokenGeneratorPassword is not set}")
    local has_env=0 has_ttl=0 param
    for param in "$@"; do
        [[ "$param" == *=* ]] || { echo "token.sh: expected param=value, got '$param'" >&2; exit 2; }
        [[ "$param" == env=* ]] && has_env=1
        [[ "$param" == ttl=* ]] && has_ttl=1
        args+=(--data-urlencode "$param")
    done
    (( has_env )) || args+=(--data-urlencode "env=${ALTINN_ENV:-at23}")
    (( has_ttl )) || args+=(--data-urlencode "ttl=1800")

    local response status body
    response="$(curl "${args[@]}" "$base/${endpoints[$kind]}")"
    status="${response##*$'\n'}"
    body="${response%$'\n'*}"
    if [[ "$status" != 200 ]]; then
        echo "token.sh: the token generator answered $status: $body" >&2
        exit 1
    fi
    printf '%s\n' "$body"
}

token() {
    (( $# >= 1 )) || usage
    if [[ "$1" == e2e ]]; then
        (( $# == 2 )) || usage
        [[ -n "${presets[$2]:-}" ]] || {
            echo "token.sh: no preset '$2' in $env; this environment has: ${!presets[*]}" >&2
            exit 2
        }
        local -a p
        IFS='|' read -r -a p <<<"${presets[$2]}"
        mint "${p[@]}"
    else
        mint "$@"
    fi
}

claims() {
    local payload
    payload="$(cut -d. -f2 | tr '_-' '/+')"
    while (( ${#payload} % 4 )); do payload+='='; done
    base64 -d <<<"$payload" | jq .
}

case "${1:-}" in
    '' | -h | --help) usage ;;
    claims) shift; token "$@" | claims ;;
    *) token "$@" ;;
esac
