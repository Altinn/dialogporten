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

# The presets mirror tests/Digdir.Library.Dialogporten.E2E.Common/E2EConstants.cs for AT23.
declare -A presets=(
    [enduser]='personal|pid=08844397713|scopes=digdir:dialogporten'
    [alt-enduser]='personal|pid=13838599936|scopes=digdir:dialogporten'
    [serviceowner]='enterprise|org=ttd|orgNo=991825827|scopes=digdir:dialogporten.serviceprovider digdir:dialogporten.serviceprovider.search'
    [serviceowner-admin]='enterprise|org=ttd|orgNo=991825827|scopes=digdir:dialogporten.serviceprovider digdir:dialogporten.serviceprovider.search digdir:dialogporten.serviceprovider.admin'
    [systemuser]='systemuser|systemUserId=51551dd0-b9c5-4f4b-a4a2-9b7e3669b364|systemUserOrg=310057223|scopes=digdir:dialogporten'
    [alt-systemuser]='systemuser|systemUserId=54cb69df-3d8a-4432-804e-742e40de6211|systemUserOrg=313006425|scopes=digdir:dialogporten'
)

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
        [[ -n "${presets[$2]:-}" ]] || { echo "token.sh: unknown preset '$2'" >&2; usage; }
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
