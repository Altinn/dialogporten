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

With [agentctl](https://github.com/digdir/digdir-agents) installed and logged in:

```sh
agentctl setup --from https://github.com/Altinn/dialogporten
```

Setup asks for the values in [`minimal/.env.sample`](minimal/.env.sample). Only your Git identity and a GitHub token
are needed to build and run the non-E2E tests. The E2E tests also need the AT23 credentials the
[E2E docs](../docs/E2E-Tests.md) list: the token generator user and password and the APIM subscription key stay on
your machine, and the host puts them only into requests to their hosts. The Maskinporten client's JWK cannot stay
outside, since the apps sign with it, so use a dedicated AT23 test client for it, or `none`.

Choose the `worktree` variant to work in your own checkout instead of the agent's clone.

## Change it

Change these files in a pull request. The `nested-build` variant builds the image from your checkout, to try a change
in a nested agent before CI publishes it. Changes to the instructions, skills and secrets reach an agent with
`agentctl setup` and Update; a new image reaches it when it is created again.
