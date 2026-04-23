# WebAPI/GraphQL/Service E2E Script

Runs the local E2E setup for WebAPI, Service, and optionally GraphQL, then executes the matching E2E tests.

## Prerequisites
- DB/Redis are running locally:
```bash
podman compose -f docker-compose-db-redis.yml up -d
```
- User secrets are configured for the projects you start locally (`WebApi`, `GraphQL`, `Service`) and for the E2E test projects.
  See [docs/E2E-Tests.md](../../docs/E2E-Tests.md).

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
WEBAPI_PORT=7214
GRAPHQL_PORT=5181
SERVICE_PORT=56842
LocalDevelopment__UseLocalDevelopmentUser=false
LocalDevelopment__UseLocalDevelopmentResourceRegister=false
LocalDevelopment__UseLocalDevelopmentOrganizationRegister=false
LocalDevelopment__UseLocalDevelopmentNameRegister=false
LocalDevelopment__UseLocalDevelopmentPartyNameRegistry=false
LocalDevelopment__UseLocalDevelopmentAltinnAuthorization=false
LocalDevelopment__UseLocalDevelopmentCloudEventBus=true
LocalDevelopment__UseLocalDevelopmentCompactJwsGenerator=true
LocalDevelopment__DisableCache=false
LocalDevelopment__DisableAuth=false
LocalDevelopment__UseInMemoryServiceBusTransport=true
LocalDevelopment__DisableSubjectResourceSyncOnStartup=true
LocalDevelopment__DisablePolicyInformationSyncOnStartup=true
LocalDevelopment__UseLocalMetricsAggregationStorage=true
```

Optional overrides (if set in `.env` or the shell):
```bash
WEBAPI_PORT=7214
GRAPHQL_PORT=5181
SERVICE_PORT=56842
```

## Optional
- Set `DIALOGPORTEN` to the repo root:
```bash
export DIALOGPORTEN=/path/to/dialogporten
```
