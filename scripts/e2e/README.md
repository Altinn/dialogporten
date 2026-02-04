# WebAPI/GraphQL E2E Script

Runs the WebAPI and/or GraphQL end-to-end tests and manages the local API processes.

## Prerequisites
- DB/Redis are running locally:
```bash
podman compose -f docker-compose-db-redis.yml up -d
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

## Configuration (.env)
The script loads `.env` from this folder by default. You can override by setting `ENV_FILE` to a different path.

Default `.env` values in this folder:
```bash
WEBAPI_ENVIRONMENT=Development
DialogportenBaseUri=https://localhost
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
WEBAPI_PORT=7215
GRAPHQL_PORT=5181
```

## Optional
- Set `DIALOGPORTEN` to the repo root:
```bash
export DIALOGPORTEN=/path/to/dialogporten
```
