#!/usr/bin/env bash
# Reads access state in Altinn Authorization (AT23 by default): who may act for whom, through which roles, access
# packages and resource delegations, and what the decision point answers.
#
#   am.sh parties <pid|orgNo|systemUserUuid> [<orgNo|pid>]   authorized parties, as Dialogporten asks for them;
#                                                           with a second argument, that party's full detail
#   am.sh lookup <pid|orgNo|uuid> ...                      party UUID, party ID and name from the register
#   am.sh whoami <pid>                                     the person's own party UUID and party ID
#   am.sh enduser-token <pid> [scopes]                     a personal token the end-user APIs accept
#   am.sh decide <pid|systemUserUuid> <pid|orgNo> <resource> [action]
#                                                           the decision point's answer, default action read
#   am.sh policy <resource>                                the roles and access packages a resource grants to
#   am.sh get <pid> <path>                                 GET an end-user API as <pid>, e.g.
#                                                           /accessmanagement/api/v2/enduser/clientdelegations/agents?party=<uuid>
#
# Needs Infrastructure__Altinn__SubscriptionKey, and TokenGeneratorUser and TokenGeneratorPassword for the tokens.
set -euo pipefail

platform="${ALTINN_PLATFORM:-https://platform.at23.altinn.cloud}"
token_sh="$(dirname "$(readlink -f "$0")")/../altinn-token-generator/token.sh"

usage() { sed -n '2,17p' "$0" | sed 's/^# \{0,1\}//' >&2; exit 2; }

# A resource owner token with the scopes Dialogporten's own Maskinporten client has for these two APIs.
resourceowner_token() {
    bash "$token_sh" enterprise org=digdir orgNo=991825827 \
        "scopes=altinn:accessmanagement/authorizedparties.admin altinn:authorization/authorize.admin"
}

# A resource owner token for the register's party lookup, which Dialogporten's own client also has.
register_token() {
    bash "$token_sh" enterprise org=digdir orgNo=991825827 scopes=altinn:register/partylookup.admin
}

# Fails with the response body on stderr unless the status is 2xx; prints the body otherwise.
call() {
    local response status body
    response="$(curl --silent --show-error --write-out '\n%{http_code}' \
        -H "Ocp-Apim-Subscription-Key: ${Infrastructure__Altinn__SubscriptionKey:?Infrastructure__Altinn__SubscriptionKey is not set}" \
        "$@")"
    status="${response##*$'\n'}"
    body="${response%$'\n'*}"
    if [[ "$status" != 2* ]]; then
        echo "am.sh: $status from ${*: -1}: $body" >&2
        exit 1
    fi
    printf '%s\n' "$body"
}

# The URN a party or subject identifier is written as in request bodies.
urn() {
    case "$1" in
        [0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]) echo urn:altinn:person:identifier-no ;;
        [0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]) echo urn:altinn:organization:identifier-no ;;
        ????????-????-????-????-????????????) echo urn:altinn:systemuser:uuid ;;
        *) echo "am.sh: '$1' is neither an 11-digit pid, a 9-digit orgNo nor a system user UUID" >&2; exit 2 ;;
    esac
}

authorized_parties() {
    local type
    type="$(urn "$1")"
    jq -n --arg type "$type" --arg value "$1" '{type: $type, value: $value}' |
        call -X POST -H "Authorization: Bearer $(resourceowner_token)" -H 'Content-Type: application/json' -d @- \
            "$platform/accessmanagement/api/v1/resourceowner/authorizedparties?includeAccessPackages=true&includeRoles=true&includeResources=true&includeInstances=true&includePartiesViaKeyRoles=auto&includeSubParties=auto&includeInactiveParties=auto"
}

parties() {
    if (( $# == 1 )); then
        authorized_parties "$1" | jq -c '.[] | {name, orgNo: .organizationNumber, pid: .personId, partyUuid, partyId,
            packages: (.authorizedAccessPackages | length), roles: (.authorizedRoles | length),
            dialogportenResources: [.authorizedResources[] | select(test("dialogporten"))],
            subunits: (.subunits | length)}'
    else
        authorized_parties "$1" | jq --arg id "$2" '[.[], .[].subunits[]?]
            | map(select(.organizationNumber == $id or .personId == $id)) | first // error("no such party")
            | {name, orgNo: .organizationNumber, pid: .personId, partyUuid, partyId, type, isDeleted,
               packages: .authorizedAccessPackages, roles: .authorizedRoles, resources: .authorizedResources,
               instances: (.authorizedInstances | length)}'
    fi
}

# The register URN of an identifier. A UUID is a party UUID, which for a system user is its own UUID.
party_urn() {
    case "$1" in
        ????????-????-????-????-????????????) echo "urn:altinn:party:uuid:$1" ;;
        *) echo "$(urn "$1"):$1" ;;
    esac
}

lookup() {
    local id urns=()
    for id in "$@"; do urns+=("$(party_urn "$id")"); done
    local found
    found="$(jq -n '{Data: $ARGS.positional}' --args "${urns[@]}" |
        call -X POST -H "Authorization: Bearer $(register_token)" -H 'Content-Type: application/json' -d @- \
            "$platform/register/api/v1/dialogporten/parties/query")"
    jq -c '.data[] | {partyType, id: (.personIdentifier // .organizationIdentifier // .partyUuid), partyUuid, partyId,
        displayName}' <<<"$found"
    # The register leaves out what it does not know, with 206 Partial Content, rather than failing.
    jq -r '.data[] | .externalUrn, .urn' <<<"$found" | sort -u >"${TMPDIR:-/tmp}/am-found.$$"
    for id in "${urns[@]}"; do
        grep -qxF "$id" "${TMPDIR:-/tmp}/am-found.$$" || echo "am.sh: the register does not know $id" >&2
    done
    rm -f "${TMPDIR:-/tmp}/am-found.$$"
}

whoami() {
    [[ "$(urn "$1")" == urn:altinn:person:identifier-no ]] || { echo "am.sh: whoami takes a pid" >&2; exit 2; }
    lookup "$1" | jq -ce '{partyUuid, partyId}'
}

enduser_token() {
    local self
    self="$(whoami "$1")"
    bash "$token_sh" personal "pid=$1" "scopes=${2:-altinn:portal/enduser}" \
        "partyId=$(jq -r .partyId <<<"$self")" "partyuuid=$(jq -r .partyUuid <<<"$self")"
}

decide() {
    local subject_type party_type
    subject_type="$(urn "$1")"
    party_type="$(urn "$2")"
    jq -n --arg st "$subject_type" --arg s "$1" --arg pt "$party_type" --arg p "$2" \
        --arg r "${3#urn:altinn:resource:}" --arg a "${4:-read}" '{Request: {
            AccessSubject: [{Id: "s1", Attribute: [{AttributeId: $st, Value: $s}]}],
            Action: [{Id: "a1", Attribute: [{AttributeId: "urn:oasis:names:tc:xacml:1.0:action:action-id",
                Value: $a, DataType: "http://www.w3.org/2001/XMLSchema#string"}]}],
            Resource: [{Id: "r1", Attribute: [{AttributeId: "urn:altinn:resource", Value: $r},
                {AttributeId: $pt, Value: $p}]}],
            MultiRequests: {RequestReference: [{ReferenceId: ["s1", "r1", "a1"]}]}}}' |
        call -X POST -H "Authorization: Bearer $(resourceowner_token)" -H 'Content-Type: application/json' -d @- \
            "$platform/authorization/api/v1/authorize" |
        jq -c '.response[] | {decision, obligations: [.obligations[]?.attributeAssignment[]? | {category, value}]}'
}

policy() {
    call "$platform/resourceregistry/api/v1/resource/${1#urn:altinn:resource:}/policy/subjects" | jq -r '.data[].urn'
}

get() {
    call -H "Authorization: Bearer $(enduser_token "$1")" "$platform$2" | jq .
}

case "${1:-}" in
    parties) (( $# == 2 || $# == 3 )) || usage; shift; parties "$@" ;;
    lookup) (( $# >= 2 )) || usage; shift; lookup "$@" ;;
    whoami) (( $# == 2 )) || usage; whoami "$2" ;;
    enduser-token) (( $# == 2 || $# == 3 )) || usage; shift; enduser_token "$@" ;;
    decide) (( $# == 4 || $# == 5 )) || usage; shift; decide "$@" ;;
    policy) (( $# == 2 )) || usage; policy "$2" ;;
    get) (( $# == 3 )) || usage; get "$2" "$3" ;;
    *) usage ;;
esac
