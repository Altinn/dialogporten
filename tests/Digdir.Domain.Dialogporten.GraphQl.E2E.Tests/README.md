# GraphQL E2E Tests

End-to-end tests that call Dialogporten GraphQL and WebAPI endpoints using test tokens from the token generator.

## Prerequisites
- Dialogporten WebAPI + GraphQL running locally (see repo `README.md`)
- Access to Altinn testtools token generator credentials

## Setup
1. Start Dialogporten locally (GraphQL + WebAPI).
   - Make sure DB/Redis are running: `podman compose -f docker-compose-db-redis.yml up -d`.
   - Ensure `appsettings.Development.json` or `appsettings.local.json` for both projects are set to:
```json
{
  "LocalDevelopment": {
    "UseLocalDevelopmentUser": false,
    "UseLocalDevelopmentResourceRegister": false,
    "UseLocalDevelopmentOrganizationRegister": false,
    "UseLocalDevelopmentNameRegister": false,
    "UseLocalDevelopmentPartyNameRegistry": false,
    "UseLocalDevelopmentAltinnAuthorization": false,
    "UseLocalDevelopmentCloudEventBus": true,
    "UseLocalDevelopmentCompactJwsGenerator": true,
    "DisableCache": false,
    "DisableAuth": false,
    "UseInMemoryServiceBusTransport": true,
    "DisableSubjectResourceSyncOnStartup": true,
    "DisablePolicyInformationSyncOnStartup": true,
    "UseLocalMetricsAggregationStorage": true
  }
}
```
2. Set the environment for the tests:
```bash
export DOTNET_ENVIRONMENT=Development
```
3. Configure token generator credentials (user secrets):
These are the same user/pass as in the [dialogporten-bruno](https://github.com/Altinn/dialogporten-bruno) repo `.env` file.  
Ask the team on Slack if you do not have them.  
This can be done in the Solution Explorer in JetBrains Rider, right-click on the project, select `Tools => .NET User Secrets`  
Or use the command line:
```bash
dotnet user-secrets set -p tests/Digdir.Domain.Dialogporten.GraphQl.E2E.Tests TokenGeneratorUser "<user>"
dotnet user-secrets set -p tests/Digdir.Domain.Dialogporten.GraphQl.E2E.Tests TokenGeneratorPassword "<password>"
```

## Run tests
These tests are marked `Explicit` and are skipped by default. Running `dotnet test` will still compile this project, so you get compile-time checks even when the E2E tests do not run.

Use the xUnit explicit switch:
- `dotnet test -- xUnit.Explicit=off` (default; do not run explicit tests)
- `dotnet test -- xUnit.Explicit=on` (run all tests, including explicit)
- `dotnet test -- xUnit.Explicit=only` (run only explicit tests)
