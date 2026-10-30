## This computer

- The .NET SDK `global.json` pins, `dotnet-ef` for migrations, Podman with `docker` and `docker compose` for
  Testcontainers and `docker-compose-db-redis.yml`, `zsh` for the E2E launcher, and `psql`. There is no browser or
  desktop.
- At boot, `agent-dialogporten-dev-init.service` makes an HTTPS dev certificate trusted system-wide, and writes the
  local database connection string, a generated Ed25519 key pair and the AT23 Maskinporten client, when the agent has
  one, into the user secrets every runtime project shares. It runs in the background after the workspace clone, so
  on a fresh boot wait until `systemctl show -P SubState agent-dialogporten-dev-init` says `exited` before you run
  the apps. After you change those secrets by hand, keep in mind that rerunning the unit keeps the generated values
  you have and overwrites the Maskinporten client.
- The E2E tests run against AT23. `TokenGeneratorUser` and `TokenGeneratorPassword` for the E2E tests and
  `Infrastructure__Altinn__SubscriptionKey` for the apps are in the environment as placeholders, which the host
  replaces with the real values only in requests to their hosts. Never print, copy or persist them, except into a
  nested agent's `~/nested.env` as `improving.md` describes. When one of them
  or the Maskinporten client (`Infrastructure:Maskinporten:ClientId` in `dotnet user-secrets list --project
  src/Digdir.Domain.Dialogporten.WebApi`) is missing, the E2E tests cannot run here: say so in the pull request
  rather than working around it, such as with the `LocalDevelopment` switches.
- The E2E tests list and label dialogs from the local copy of Altinn's subject-resource mappings and resource
  policies. `scripts/e2e/.env` turns off their sync at startup, so on a database the launcher has just created they
  are empty: searches time out and system labels answer 403 while single-dialog GETs pass. Fill them once Postgres
  is up, with the launcher's environment, without which the Janitor reads its local fake registry and syncs nothing:

  ```sh
  (set -a; source scripts/e2e/.env; set +a; export DOTNET_ENVIRONMENT=Development RUNNING_E2E_TESTS=true
   cd src/Digdir.Domain.Dialogporten.Janitor && dotnet run -- sync-subject-resource-mappings &&
   dotnet run --no-build -- sync-resource-policy-information)
  ```

  Some policy fetches fail for AT23 resources that have no policy; that is expected. The apps cache the mappings in
  Redis for 20 minutes, so after a run on empty tables, clear it before the next:
  `docker compose -f docker-compose-db-redis.yml exec dialogporten-redis redis-cli FLUSHALL`.
- The E2E tests also depend on test users, delegations and roles in AT23. When they fail with 403s or missing dialogs
  and the local mappings are filled, check that state with the `altinn-access-management` skill before suspecting the
  code, and mint tokens to reproduce a request with the `altinn-token-generator` skill.
- The E2E launcher leaves Postgres and Redis running, as `AGENTS.md` describes. Stop them with
  `docker compose -f docker-compose-db-redis.yml down` when a task is done.
- Pull request evidence is test output and terminal recordings, as the `pr-evidence` skill describes.
