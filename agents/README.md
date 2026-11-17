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
| `minimal/` | The manifest and its variants |

## Set it up

[Install agentctl](https://github.com/digdir/digdir-agents/tree/main/agentctl#install) and log in to Claude Code with
`agentctl claude login`. Then store, once on your machine, what the agent uses without it entering the sandbox. The
host puts each value only into requests to its hosts.

- The GitHub account the agent works as, such as a GitHub user made for your agents. Sign in as that user, such as in
  a private window, and check that agentctl names it before it keeps the token. The agent pushes and opens pull
  requests as that account, and Git commits as it. An account that cannot push to this repository works from its
  fork, and a maintainer approves the CI runs of its pull requests.

  ```sh
  agentctl github login default
  ```

- The AT23 APIM subscription key, `Infrastructure:Altinn:SubscriptionKey` in your user secrets. The local apps and the
  `altinn-access-management` skill call Altinn APIs in AT23 that answer 401 without it.

  ```sh
  agentctl secret set altinn-at23-subscription-key --hosts platform.at23.altinn.cloud
  ```

- For the E2E tests, the test token generator's user and password, which the [E2E docs](../docs/E2E-Tests.md) list.

  ```sh
  agentctl secret set altinn-token-generator-user --hosts altinn-testtools-token-generator.azurewebsites.net
  agentctl secret set altinn-token-generator-password --hosts altinn-testtools-token-generator.azurewebsites.net
  ```

The E2E tests also need an AT23 Maskinporten client with the scopes `Infrastructure:Maskinporten:Scope` lists in the
WebApi's `appsettings.Development.json`. The apps sign with its JWK, so the key enters the sandbox: use a dedicated
test client, and give it in a variant of your own beside the manifest, `minimal/agent.mine.yaml`, which Git ignores:

```yaml
apiVersion: agents.platform/v1alpha1
kind: AgentVariant
extends: agent.yaml
spec:
  environment:
    - name: MASKINPORTEN_AT23_CLIENT_ID
      value: <client ID>
    - name: MASKINPORTEN_AT23_ENCODED_JWK
      value: <base64-encoded JWK>
```

Then apply the manifest from a checkout, with `--variant mine` when you have that variant:

```sh
git clone https://github.com/Altinn/dialogporten
cd dialogporten/agents/minimal
agentctl apply --wait --variant mine
```

The agent works in its own clone of this repository.

## Change it

Change these files in a pull request. The `build` variant builds the image from your checkout, to try a change before CI
publishes it. Changes to the instructions and skills, and to secrets, reach an agent when you run `agentctl apply`
again from an updated checkout. A new image reaches it only when it is created again. After an agentctl release,
`agentctl update` in a checkout proposes the changes the definition needs for it.
