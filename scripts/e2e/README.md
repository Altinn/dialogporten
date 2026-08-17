# WebAPI/GraphQL/Service E2E Script

Runs the local E2E setup for WebAPI, Service, and optionally GraphQL, then executes the matching E2E tests.

## Prerequisites
- DB/Redis are running locally:
```bash
podman compose -f docker-compose-db-redis.yml up -d
```
- User secrets are configured for the projects you start locally (`WebApi`, `GraphQL`, `Service`) and for the E2E test projects.
  See [docs/E2E-Tests.md](../../docs/E2E-Tests.md).
- Authorization reference data is seeded — see below.

## Authorization reference data

`SubjectResource` and `ResourcePolicyInformation` must be populated before the E2E tests will pass.
End-user search authorization resolves the caller's roles and access packages through
`SubjectResource`, and the minimum-authentication-level checks read `ResourcePolicyInformation`.

Both tables are empty on a fresh database, and `.env` disables the per-process startup syncs
(`DisableSubjectResourceSyncOnStartup` / `DisablePolicyInformationSyncOnStartup`) because `WebApi`,
`Service` and `GraphQL` all register those hosted services and would race on the same merge —
failing with `duplicate key value violates unique constraint "IX_SubjectResource_Resource_Subject"`.

**Empty tables do not produce an error.** They produce roughly 20 confusing test failures: every
end-user search returns zero hits, so search tests fail as 20-second `E2ERetryPolicies` timeouts,
and with no per-resource minimum authentication level the "inadequate auth level" tests get
`200`/`404` where they expect `403`.

Seed them once per database with the Janitor (takes ~20s):

```bash
cd src/Digdir.Domain.Dialogporten.Janitor
DOTNET_ENVIRONMENT=Development dotnet run --no-launch-profile -- sync-subject-resource-mappings
DOTNET_ENVIRONMENT=Development dotnet run --no-launch-profile -- sync-resource-policy-information
```

The Janitor resolves `CostManagementAggregation/cost-coefficients.json` relative to the working
directory, so it must be run from its own project directory or it fails with `FileNotFoundException`.

To check the current state:

```bash
psql -h localhost -p 5432 -U postgres -d dialogporten \
  -tAc 'select (select count(*) from "SubjectResource"), (select count(*) from "ResourcePolicyInformation");'
```

## Run
From this directory:
```bash
./run-webapi-e2e.zsh
```

Modes:
```bash
./run-webapi-e2e.zsh webapi
./run-webapi-e2e.zsh graphql
./run-webapi-e2e.zsh both
```

The script always starts `WebApi` and `Service`. `graphql` and `both` also start `GraphQL`.
It exports `RUNNING_E2E_TESTS=true`, so `appsettings.local.json` is ignored for the runtime projects during E2E runs.

## Configuration (.env)
The script loads `.env` from this folder by default. You can override by setting `ENV_FILE` to a different path.

Default `.env` values in this folder:
```bash
WEBAPI_ENVIRONMENT=Development
DialogportenBaseUri=https://localhost
WEBAPI_PORT=7215
GRAPHQL_PORT=5180
SERVICE_PORT=56843
LocalDevelopment__UseLocalDevelopmentUser=false
LocalDevelopment__UseLocalDevelopmentResourceRegister=false
LocalDevelopment__UseLocalDevelopmentOrganizationRegister=false
LocalDevelopment__UseLocalDevelopmentNameRegister=false
LocalDevelopment__UseLocalDevelopmentPartyNameRegistry=false
LocalDevelopment__UseLocalDevelopmentAltinnAuthorization=false
LocalDevelopment__UseLocalDevelopmentCloudEventBus=true
LocalDevelopment__UseLocalDevelopmentCompactJwsGenerator=false
LocalDevelopment__DisableCache=false
LocalDevelopment__DisableAuth=false
LocalDevelopment__UseInMemoryServiceBusTransport=true
LocalDevelopment__DisableSubjectResourceSyncOnStartup=true
LocalDevelopment__DisablePolicyInformationSyncOnStartup=true
LocalDevelopment__UseLocalMetricsAggregationStorage=true
```

`UseLocalDevelopmentCompactJwsGenerator` must stay `false` here: the local decorator returns the
literal string `local-development-jws`, while the E2E tests verify real dialog and context token
signatures against the JWKS endpoint (`Expected a compact JWS with three parts, got 1`).

Optional overrides (if set in `.env` or the shell):
```bash
WEBAPI_PORT=7215
GRAPHQL_PORT=5180
SERVICE_PORT=56843
```

## Optional
- Set `DIALOGPORTEN` to the repo root:
```bash
export DIALOGPORTEN=/path/to/dialogporten
```
