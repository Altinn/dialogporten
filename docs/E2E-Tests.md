# E2E Tests

End-to-end tests that call Dialogporten endpoints using test tokens from the token generator.

## Test Projects
- WebAPI: [tests/Digdir.Domain.Dialogporten.WebAPI.E2E.Tests](../tests/Digdir.Domain.Dialogporten.WebAPI.E2E.Tests)
- GraphQL: [tests/Digdir.Domain.Dialogporten.GraphQl.E2E.Tests](../tests/Digdir.Domain.Dialogporten.GraphQl.E2E.Tests) (calls both GraphQL and WebAPI)

## Run tests
These tests are marked `Explicit` and are skipped by default. Running `dotnet test` will still compile these projects, so you get compile-time checks even when the E2E tests do not run.

By default, E2E tests are explicit.
To run them by default locally, create `appsettings.local.json` in `tests/Digdir.Library.Dialogporten.E2E.Common`
and set `ExplicitTests` to `false`:

```json
{
  "ExplicitTests": false
}
```

If `appsettings.local.json` is missing, `ExplicitTests` defaults to `true` (tests remain explicit).
You can still override with xUnit flags when needed:

Use the xUnit explicit switch:
- `dotnet test -- xUnit.Explicit=off` (default; do not run explicit tests)
- `dotnet test -- xUnit.Explicit=on` (run all tests, including explicit)
- `dotnet test -- xUnit.Explicit=only` (run only explicit tests)

The tests can also be run with the scripts at [scripts/e2e/README.md](../scripts/e2e/README.md).

## Prerequisites
- Dialogporten running locally (see repo [README.md](../README.md)).
  - WebAPI E2E: WebApi + Service.
  - GraphQL E2E: WebApi + GraphQL + Service.
- Access to Altinn testtools token generator credentials.

## Setup
1. Start DB/Redis locally:
   - `podman compose -f docker-compose-db-redis.yml up -d`
2. Configure user secrets for the runtime projects you will start locally:
   - Set the required project-scoped secrets for `src/Digdir.Domain.Dialogporten.WebApi`, `src/Digdir.Domain.Dialogporten.GraphQL`, and `src/Digdir.Domain.Dialogporten.Service` as needed.
   - The checked-in `appsettings.Development.json` files already target AT23. Keep your secret values aligned with the same environment, preferably AT23, when running local E2E in `Development`.
   - In practice, that means local DB/Redis values can stay local, while external values such as `Infrastructure:Maskinporten:ClientId`, `Infrastructure:Maskinporten:EncodedJwk`, `Infrastructure:Altinn:SubscriptionKey`, and `Application:Dialogporten:Ed25519KeyPairs:*` must belong to the same external environment as the rest of the development config.
   - If you override environment-specific values in `appsettings.local.json` or user secrets, make sure they still match the AT23-oriented defaults in `appsettings.Development.json`, such as:
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
3. Set the environment for local E2E:
```bash
export DOTNET_ENVIRONMENT=Development
```
   - In `Development`, the E2E token generator uses `at23`.
4. Configure token generator credentials (user secrets) for the E2E test projects:
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
5. Start Dialogporten locally:
   - Shell script: [scripts/e2e/run-webapi-e2e.zsh](../scripts/e2e/run-webapi-e2e.zsh)
   - Rider: use the checked-in `.run` profiles `WebApi (E2E)`, `GraphQL (E2E)`, `Service (E2E)`, or the compound `E2E (WebAPI/GQL/Service)`.
   - The shell script and Rider E2E profiles set `RUNNING_E2E_TESTS=true`, which prevents `appsettings.local.json` from being loaded for `WebApi`, `GraphQL`, and `Service`. This avoids accidental local overrides during E2E runs.
   - The test-project file `tests/Digdir.Library.Dialogporten.E2E.Common/appsettings.local.json` is unaffected and is still the place to set `ExplicitTests=false` if you want explicit tests to run by default in your IDE.

## Writing tests
Use the shared E2E base class and custom attributes so hooks and explicit behavior are consistent:
- WebAPI: inherit `E2ETestBase<WebApiE2EFixture>`.
- GraphQL: inherit `E2ETestBase<GraphQlE2EFixture>`.
- Keep tests under `Features/*`.
- Use `[E2EFact]` or `[E2ETheory]` on every test (do not use `[Fact]`/`[Theory]`).
- Explicit behavior is centralized in `E2EExplicitOptions` in [Digdir.Library.Dialogporten.E2E.Common/E2ETestAttributes.cs](../tests/Digdir.Library.Dialogporten.E2E.Common/E2ETestAttributes.cs).

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
