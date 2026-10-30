# Dialogporten agent

A backend developer agent for Dialogporten: a computer of its own with the .NET SDK `global.json` pins, `dotnet-ef`,
Podman for Testcontainers and the E2E launcher's Postgres and Redis, and Claude Code, with Codex as an option. It
clones this repository at boot, builds the solution, runs the unit, architecture and integration tests and, with AT23
credentials, the WebApi and GraphQL E2E tests, and opens pull requests that show what it ran.

| Path | What it is |
| --- | --- |
| `Dockerfile` | The image, published by `.github/workflows/agents-images.yml` as `ghcr.io/altinn/dialogporten/agent-minimal` |
| `smoke.sh` | What CI checks in each built image |
| `common/` | The repositories cloned at boot, and the boot unit that sets up the dev certificate and user secrets |
| `instructions.md`, `improving.md`, `minimal/environment.md` | What the agent is told |
| `_skills/` | Procedures some tasks need |
| `minimal/` | The manifest, its variants and `.env.sample` |

## Set it up

With [agentctl](https://github.com/digdir/digdir-agents) installed and logged in, apply the manifest from a checkout:

```sh
git clone https://github.com/Altinn/dialogporten
cd dialogporten/agents/minimal
cp .env.sample .env
agentctl apply --wait
```

Fill in `.env` before applying; [`minimal/.env.sample`](minimal/.env.sample) explains each value, and Git ignores the
copy. Your Git identity, a GitHub token and the AT23 APIM subscription key are required: the local apps and the
`altinn-access-management` skill call Altinn APIs in AT23 that answer 401 without the key. The E2E tests also need the
AT23 credentials the [E2E docs](../docs/E2E-Tests.md) list. The token generator user and password and the subscription
key stay on your machine, and the host puts them only into requests to their hosts. The Maskinporten client's JWK
cannot stay outside, since the apps sign with it, so use a dedicated AT23 test client for it, or `none`.

The agent works in its own clone. It cannot mount your checkout instead: agentctl refuses a bind mount that
contains a `.env` file, and this repository tracks `.env` and `scripts/e2e/.env`.

## Change it

Change these files in a pull request. The `nested-build` variant builds the image from your checkout, to try a change in
a nested agent before CI publishes it. Changes to the contents of the instructions and skills, and to secrets, reach an
agent when you run `agentctl apply` again from an updated checkout. A new image, or a change to which instruction files
and skills it reads, reaches it only when it is created again.
