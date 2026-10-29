---
name: altinn-token-generator
description: Mint Altinn test tokens (end user, service owner, system user, enterprise) for AT23, TT02 or YT01 from the Altinn test token generator, as the E2E tests do, and decode their claims. Use when calling Dialogporten or Altinn APIs by hand, reproducing an E2E request, or checking which claims a test token carries.
---

# Altinn test tokens

The E2E tests get every token from the [Altinn test token generator](https://github.com/Altinn/AltinnTestTools/tree/master/TokenGenerator),
which signs tokens that AT23, TT02 and YT01 accept as Altinn-issued. `token.sh` next to this file wraps it; run it
with `bash`. It needs `TokenGeneratorUser` and `TokenGeneratorPassword`, which are placeholders here that the host
fills in only for the generator's host, so pass them as they are and never print them.

## Tokens the E2E tests use

`token.sh e2e <preset>` mints the token `tests/Digdir.Library.Dialogporten.E2E.Common/TestTokenGenerator.cs` would,
with the identities in `E2EConstants.cs`:

| preset | kind | identity | scopes |
|---|---|---|---|
| `enduser` | personal | pid 08844397713 | `digdir:dialogporten` |
| `alt-enduser` | personal | pid 13838599936 | `digdir:dialogporten` |
| `serviceowner` | enterprise | ttd, 991825827 | `digdir:dialogporten.serviceprovider` and `.search` |
| `serviceowner-admin` | enterprise | ttd, 991825827 | as above and `.admin` |
| `systemuser` | system user | 51551dd0-…-9b7e3669b364 for 310057223 | `digdir:dialogporten` |
| `alt-systemuser` | system user | 54cb69df-…-742e40de6211 for 313006425 | `digdir:dialogporten` |

```sh
T=$(bash ~/.claude/skills/altinn-token-generator/token.sh e2e enduser)
```

Then call an API with `-H "Authorization: Bearer $T"`. Keep tokens in variables rather than printing them.

## Any other token

`token.sh <kind> param=value ...` passes the params to the generator as they are. Kinds: `personal`, `enterprise`,
`systemuser`, `enterpriseuser`, `selfidentified`, `platform`, `platformaccess`. `env` defaults to `$ALTINN_ENV` or
`at23`, `ttl` to 1800 seconds.

```sh
S=~/.claude/skills/altinn-token-generator
bash $S/token.sh claims personal pid=08885399984 scopes=altinn:portal/enduser partyId=50650580 partyuuid=066e42cd-02d2-4489-b665-e72157057d0d
bash $S/token.sh claims e2e serviceowner-admin | jq -c '{scope, org: ."urn:altinn:org", consumer: .consumer.ID}'
ALTINN_ENV=tt02 bash $S/token.sh claims e2e enduser | jq -r .iss
```

`claims` in front of either form prints the token's claims as JSON instead of the token.

Parameters per kind, from the generator's source:

- **personal**: `pid`, `scopes` (default `altinn:enduser`), `userId`, `partyId`, `partyuuid`, `authLvl` (1–4, default 3,
  which also sets `acr`), `consumerOrgNo`, `userName`, `clientAmr`.
- **enterprise**: `org` (becomes `urn:altinn:org`), `orgNo` (`consumer` and `urn:altinn:orgNumber`), `scopes`,
  `supplierOrgNo`.
- **systemuser**: `systemUserId` (required), `systemUserOrg`, `orgNo` (the vendor, default 991825827), `scopes`.
- **enterpriseuser**: `orgNo`, `partyId`, `userId`, `userName` (all required), `scopes`, `org`, `partyuuid`.

## Gotchas

- **The generator looks nothing up.** A personal token carries `urn:altinn:userid`, `urn:altinn:partyid` and
  `urn:altinn:party:uuid` only when you pass `userId`, `partyId` and `partyuuid`. The E2E tokens carry only `pid`.
  APIs that read the party from the token, such as Altinn's end-user Access Management APIs, answer 403 to a
  pid-only token; the `altinn-access-management` skill's `am.sh enduser-token` fills them in.
- **`dump` needs a value.** The generator's own `dump=true` returns the decoded token, but a bare `dump` is
  ignored and returns the token. Use `token.sh claims`, which decodes locally and gives plain JSON.
- **Scopes are space separated in one param.** Quote it: `"scopes=a b"`.
- **AT23 issuer.** Tokens for `at23` have `iss` `https://platform.at23.altinn.cloud/authentication/api/v1/openid/`
  and `actual_iss` `altinn-test-tools`; the local WebApi trusts that issuer in Development.

## Troubleshooting

- **`token.sh: the token generator answered 400: {"pid":"Parameter was supplied with an invalid value"}`**: the
  generator names the bad param; a pid is 11 digits and an orgNo 9. An unknown `env` gives the same with `"env"`.
- **401 from the generator**: the credentials did not reach it. Check that `TokenGeneratorUser` and
  `TokenGeneratorPassword` are set, without printing them.
