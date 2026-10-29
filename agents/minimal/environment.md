## This computer

- The .NET SDK `global.json` pins, `dotnet-ef` for migrations, Podman with `docker` and `docker compose` for
  Testcontainers and `docker-compose-db-redis.yml`, `zsh` for the E2E launcher, and `psql`. There is no browser or
  desktop.
- At boot, `agent-dialogporten-dev-init.service` makes an HTTPS dev certificate trusted system-wide, and writes the
  local database connection string, a generated Ed25519 key pair and the AT23 Maskinporten client, when the agent has
  one, into the user secrets every runtime project shares. After you change those secrets by hand, keep in mind that
  rerunning the unit keeps the generated values you have and overwrites the Maskinporten client.
- The E2E tests run against AT23. `TokenGeneratorUser` and `TokenGeneratorPassword` for the E2E tests and
  `Infrastructure__Altinn__SubscriptionKey` for the apps are in the environment as placeholders, which the host
  replaces with the real values only in requests to their hosts. Never print, copy or persist them. When one of them
  or the Maskinporten client (`Infrastructure:Maskinporten:ClientId` in `dotnet user-secrets list --project
  src/Digdir.Domain.Dialogporten.WebApi`) is missing, the E2E tests cannot run here: say so in the pull request
  rather than working around it, such as with the `LocalDevelopment` switches.
- The E2E launcher leaves Postgres and Redis running, as `AGENTS.md` describes. Stop them with
  `docker compose -f docker-compose-db-redis.yml down` when a task is done.
- Pull request evidence is test output and terminal recordings, as the `pr-evidence` skill describes.
