# E2E Tests

End-to-end tests that call Dialogporten endpoints using test tokens from the token generator.

## Test Projects
- WebAPI: [tests/Digdir.Domain.Dialogporten.WebAPI.E2E.Tests](../tests/Digdir.Domain.Dialogporten.WebAPI.E2E.Tests)
- GraphQL: [tests/Digdir.Domain.Dialogporten.GraphQl.E2E.Tests](../tests/Digdir.Domain.Dialogporten.GraphQl.E2E.Tests) (calls both GraphQL and WebAPI)

## Run tests
These tests are marked `Explicit` and are skipped by default. Running `dotnet test` will still compile these projects, so you get compile-time checks even when the E2E tests do not run.

To enable these tests locally for debugging, set `E2EExplicitOptions.ExplicitTests` to `false`
(when `true`, tests are marked explicit and skipped unless you pass `xUnit.Explicit=on/only`).

Use the xUnit explicit switch:
- `dotnet test -- xUnit.Explicit=off` (default; do not run explicit tests)
- `dotnet test -- xUnit.Explicit=on` (run all tests, including explicit)
- `dotnet test -- xUnit.Explicit=only` (run only explicit tests)

The tests can also be run with the scripts at [scripts/e2e/README.md](../scripts/e2e/README.md).

## Prerequisites
- Dialogporten running locally (see repo [README.md](../README.md)).
  - WebAPI E2E: WebAPI only.
  - GraphQL E2E: WebAPI + GraphQL.
- Access to Altinn testtools token generator credentials.

## Setup
1. Start Dialogporten locally.
   - Make sure DB/Redis are running: `podman compose -f docker-compose-db-redis.yml up -d`.
   - Ensure `appsettings.Development.json` or `appsettings.local.json` for the relevant projects are set to:
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
These are the same user/pass as in the `dialogporten-bruno` repo `.env` file.
Ask the team on Slack if you do not have them.
This can be done in the Solution Explorer in JetBrains Rider, right-click on the project, select `Tools => .NET User Secrets`.
Or use the command line:
```bash
dotnet user-secrets set -p tests/Digdir.Domain.Dialogporten.WebAPI.E2E.Tests TokenGeneratorUser "<user>"
dotnet user-secrets set -p tests/Digdir.Domain.Dialogporten.WebAPI.E2E.Tests TokenGeneratorPassword "<password>"

dotnet user-secrets set -p tests/Digdir.Domain.Dialogporten.GraphQl.E2E.Tests TokenGeneratorUser "<user>"
dotnet user-secrets set -p tests/Digdir.Domain.Dialogporten.GraphQl.E2E.Tests TokenGeneratorPassword "<password>"
```

## Writing tests
Use the shared E2E base class and custom attributes so hooks and explicit behavior are consistent:
- WebAPI: inherit `E2ETestBase<WebApiE2EFixture>`.
- GraphQL: inherit `E2ETestBase<GraphQlE2EFixture>`.
- Keep tests under `Features/*`.
- Use `[E2EFact]` or `[E2ETheory]` on every test (do not use `[Fact]`/`[Theory]`).
- Explicit behavior is centralized in `E2EExplicitOptions` in [Digdir.Library.Dialogporten.E2E.Common/E2ETestAttributes.cs](../src/Digdir.Library.Dialogporten.E2E.Common/E2ETestAttributes.cs).

Tests must not assume a clean environment. When listing dialogs (or similar), account for other dialogs that may exist for the same test org
or service resource.



## Test data cleanup
Cleanup is scheduled via GitHub Actions at 04:00 UTC for `test`, `staging`, and `yt01`
in [.github/workflows/dispatch-purge-e2e-test-data.yml](../.github/workflows/dispatch-purge-e2e-test-data.yml), and can be triggered manually via Actions -> "Purge E2E test data".

To run the cleanup locally:
```bash
dotnet test tests/Digdir.Domain.Dialogporten.E2E.Cleanup.Tests/Digdir.Domain.Dialogporten.E2E.Cleanup.Tests.csproj \
  --configuration Release \
  -- xUnit.Explicit=only
```
