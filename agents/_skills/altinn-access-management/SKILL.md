---
name: altinn-access-management
description: Inspect Altinn Authorization and Access Management state in AT23 (authorized parties, roles, access packages, client delegations, system users, resource policies, PDP decisions) that Dialogporten's E2E tests depend on. Use when E2E tests fail with 403/404 or missing dialogs, to check whether the test users' delegations and roles are still in place, or to ask whether a user may access a resource for a party.
---

# Altinn access state in AT23

Dialogporten does not decide access itself. It asks Altinn Authorization for the end user's authorized parties, with
their roles, access packages and resource delegations, and asks the decision point (PDP) about single dialogs. The
E2E tests assume test data in AT23 that is documented in
`tests/Digdir.Library.Dialogporten.E2E.Common/E2EConstants.cs`. When that data changes, tests fail with 403s, 404s
or searches that time out. So do they when the local database's subject-resource mappings are empty, as this agent's
instructions describe: check `select count(*) from "SubjectResource"` first. `am.sh` next to this file reads that
state the way Dialogporten does; run it with `bash`. It uses the `altinn-token-generator` skill's `token.sh` for
tokens and needs `Infrastructure__Altinn__SubscriptionKey`, a placeholder the host fills in only for
`platform.at23.altinn.cloud`.

Background: [Access management](https://docs.altinn.studio/en/authorization/what-do-you-get/accessmanagement/),
and the code in [altinn-auth](https://github.com/Altinn/altinn-auth) under `src/apps/Altinn.AccessManagement`.
Where the docs and the code disagree, the code is right.

## Check the E2E test data

```sh
S=~/.claude/skills/altinn-access-management
bash $S/am.sh parties 08844397713
bash $S/am.sh parties 08844397713 310923044 | jq -c '{name, partyUuid, packages: (.packages|length), roles: .roles[:5], resources}'
bash $S/am.sh decide 08844397713 310923044 urn:altinn:resource:ttd-dialogporten-automated-tests
bash $S/am.sh decide 51551dd0-b9c5-4f4b-a4a2-9b7e3669b364 310057223 ttd-dialogporten-automated-tests
bash $S/am.sh policy ttd-dialogporten-automated-tests
```

What it should show in AT23, as of October 2026:

- `parties 08844397713`: the user's own person party, `310923044` (daglig leder) and `310010693`.
- `decide` for 08844397713 on itself or on 310923044: `Permit`, with a minimum authentication level 2 obligation.
  On 991825827, which the user does not represent: `NotApplicable`.
- `parties 51551dd0-b9c5-4f4b-a4a2-9b7e3669b364`: 310057223 with the access package
  `ordinaer-post-til-virksomheten`.
- `policy ttd-dialogporten-automated-tests`: `dagl`, `priv`, `org:ttd` and the access packages
  `ordinaer-post-til-virksomheten` and `regnskapsforer-med-signeringsrettighet`.
- `parties 13838599936`: client `210142762` with `regnskapsforer-med-signeringsrettighet`, from the client delegation
  below.

## Party identifiers from a pid or orgNo

Altinn knows each party by a pid or orgNo, a party UUID and an integer party ID, and its APIs want different ones.
The register maps between them. Ask it with an enterprise token with `altinn:register/partylookup.admin`, as
Dialogporten's own party name lookup does:

```sh
bash $S/am.sh lookup 08885399984 314243994 e3370464-b001-4a5e-b375-f25b36dc87ab 51551dd0-b9c5-4f4b-a4a2-9b7e3669b364
```

Without the helper, the request is:

```sh
T=$(bash ~/.claude/skills/altinn-token-generator/token.sh enterprise org=digdir orgNo=991825827 scopes=altinn:register/partylookup.admin)
curl -s -X POST https://platform.at23.altinn.cloud/register/api/v1/dialogporten/parties/query \
  -H "Authorization: Bearer $T" -H "Ocp-Apim-Subscription-Key: $Infrastructure__Altinn__SubscriptionKey" \
  -H 'Content-Type: application/json' -d '{"Data":["urn:altinn:person:identifier-no:08844397713"]}' | jq -c '.data[]'
```

`Data` takes `urn:altinn:person:identifier-no:<pid>`, `urn:altinn:organization:identifier-no:<orgNo>`,
`urn:altinn:party:uuid:<uuid>` and `urn:altinn:systemuser:uuid:<uuid>`, several at once. Each answer has
`partyType`, `personIdentifier` or `organizationIdentifier`, `partyUuid`, `partyId` and `displayName`, but no user
ID. `partyId` is null for a system user.

## Commands

| command | answers |
|---|---|
| `parties <pid\|orgNo\|systemUserUuid>` | one line per party the subject may act for, with counts. This is the call Dialogporten makes. |
| `parties <subject> <orgNo\|pid>` | that one party's roles, access packages, resources and instance count |
| `lookup <pid\|orgNo\|uuid> ...` | each party's type, `partyUuid`, `partyId` and name, from the register |
| `whoami <pid>` | the person's own `partyUuid` and `partyId` |
| `enduser-token <pid> [scopes]` | a personal token with the `partyId` and `partyuuid` `lookup` finds, default scope `altinn:portal/enduser` |
| `decide <pid\|systemUserUuid> <pid\|orgNo> <resource> [action]` | the PDP's decision, default action `read` |
| `policy <resource>` | the roles, access packages and orgs the resource's policy grants to |
| `get <pid> <path>` | GET an end-user API as that person |

`parties` lists can be long: 08885399984 represents some 150 organizations. Use the second argument to look at one.

## End-user APIs

Connections, client delegations and other end-user APIs take party UUIDs in the query (`party`, `from`, `to`,
`agent`, `client`). Get them with `lookup`, or from the `partyUuid` column of `parties`. The caller needs the right
to administer access for `party`, such as daglig leder.

```sh
bash $S/am.sh get 08885399984 '/accessmanagement/api/v2/enduser/clientdelegations/agents?party=49c48d6e-d8a5-4903-ae32-481e68ab6794' | jq -c '.data[] | {agent: .agent.personIdentifier, name: .agent.name, access: [.access[] | {role: .role.code, packages: [.packages[]?.urn]}]}'
bash $S/am.sh get 08885399984 '/accessmanagement/api/v2/enduser/clientdelegations/agents/accesspackages?party=49c48d6e-d8a5-4903-ae32-481e68ab6794&agent=e3370464-b001-4a5e-b375-f25b36dc87ab' | jq -c '.data[]? | {client: .client.organizationIdentifier, access: [.access[] | {role: .role.code, packages: [.packages[].urn]}]}'
bash $S/am.sh get 08844397713 '/accessmanagement/api/v1/enduser/connections?party=e3f80a3f-fef6-4555-a955-6c9ffb2af6ea&from=e3f80a3f-fef6-4555-a955-6c9ffb2af6ea' | jq -c '.data[]? | {name: .party.name, roles: [.roles[]?.code]}'
```

The first two show the client delegation the E2E tests use. 08885399984 is daglig leder of accounting firm
314243994 (party `49c48d6e-…`). 13838599936 (party `e3370464-…`) is its agent, with
`regnskapsforer-med-signeringsrettighet` for client 210142762.

## Repairing state

AT23 is shared with other teams, and Altinn Authorization's own tests depend on some of the same parties, so
**confirm with the user before changing anything**. Say what you will change and why. None of the requests below
has been run by this skill; their shapes come from `ClientDelegationController.cs` and `ConnectionsController.cs` in
altinn-auth. Send them with `curl` and the method each names, `POST` or `DELETE`, and a token from
`am.sh enduser-token <the administrator's pid>`. Client delegations are taken back with a `POST` to a `/delete` route.

- **Client delegation** (as the firm's daglig leder, base `/accessmanagement/api/v2/enduser/clientdelegations`):
  - add an agent: `POST agents?party=<firmUuid>` with `{"personIdentifier":"<pid>","lastName":"<last name>"}`;
  - give the agent a package for a client: `POST agents/accesspackages?party=<firmUuid>&client=<clientUuid>&agent=<agentUuid>`
    with `{"values":[{"role":"regnskapsforer","packages":["urn:altinn:accesspackage:regnskapsforer-med-signeringsrettighet"]}]}`.
    The answer has `changed:false` for grants that already existed;
  - take it back: the same request to `agents/accesspackages/delete`.
- **Access packages between parties** (as someone who administers access for `party`):
  `POST /accessmanagement/api/v1/enduser/connections/accesspackages?party=<from>&to=<to>&package=<urn>`, and
  `DELETE` with `party`, `from`, `to` and `package` to take it back.
- **Roles from the business register**, such as daglig leder, can't be made through these APIs. They come from the
  register's test data.

## Gotchas

- **End-user APIs reject pid-only tokens.** They answer 403 with an empty body unless the personal token carries
  `partyId` and `partyuuid`, which the generator does not look up. Use `am.sh enduser-token` or `am.sh get`.
- **Three identifier kinds.** Request bodies take URNs with a pid or orgNo, end-user query parameters take party
  UUIDs, and the system user API in Authentication takes the integer party ID. `lookup` maps between them.
- **The register leaves out what it does not know.** A pid or orgNo with no party gives 206 Partial Content and no
  entry for it, not an error; `am.sh lookup` names what is missing. One that fails its checksum, such as
  `01010199999`, fails the whole request with 400 `Invalid PartyUrn`. Answers do not come back in the order asked.
- **System users have no party ID.** The register gives a system user `partyType` `system-user`, its own UUID as
  `partyUuid` and a `partyId` of null.
- **`NotApplicable` is a no.** The PDP answers `NotApplicable`, not `Deny`, when no rule matches, such as for a
  party the user does not represent.
- **Resource ids.** The PDP and the resource registry take the id without `urn:altinn:resource:`; `am.sh` strips it.
- **Subscription key.** In AT23 the PDP and the register return 401 without `Ocp-Apim-Subscription-Key`, while
  authorized parties, connections, metadata and the resource registry answer without it. `am.sh` always sends it, as
  Dialogporten does.
- **Scopes.** Resource-owner authorized parties needs `altinn:accessmanagement/authorizedparties.admin` or
  `.resourceowner` and answers 403 without; the PDP needs `altinn:authorization/authorize` or `.admin`. `am.sh` mints
  a digdir enterprise token with both `.admin` scopes, which Dialogporten's own client has. The register's party
  query needs `altinn:register/partylookup.admin` and answers 403 without.
- **Caching after a change.** Dialogporten caches authorized parties for 15 minutes and may serve stale data for up
  to 30 if Altinn is slow (`InfrastructureExtensions.cs`), so a repair can take that long to reach the tests.

## Troubleshooting

- **`am.sh: 403 from …/enduser/…` with an empty body**: either the token lacks the party claims (use `am.sh get`, not
  your own token) or the person may not administer access for `party`.
- **`am.sh: the register does not know urn:altinn:person:identifier-no:<pid>`**: no such party in AT23.
- **`am.sh: '<id>' is neither an 11-digit pid, a 9-digit orgNo nor a system user UUID`**: as it says. Party UUIDs are
  not accepted as subjects.
