## Improving this agent

This agent's definition is `agents/` in `Altinn/dialogporten`: the `Dockerfile` and `smoke.sh` for the image, the
boot unit in `common/`, these instructions, the skills in `_skills/` and the manifests in `minimal/`. When it gets in
the way of the work, such as a missing tool or a wrong instruction, propose a change to it in a pull request, as the
`agent-definition` skill describes when it is available.

Try the change before you propose it, in a nested agent from your worktree. `~/nested.env` gives it the values
`minimal/.env.sample` names. For the secrets it passes on this agent's own placeholders, which stay inert: the nested
agent's requests leave through this one, where the host puts in the real values, so none of them enter either agent.

```sh
cat >~/nested.env <<EOF
GIT_USER_NAME=$GIT_USER_NAME
GIT_USER_EMAIL=$GIT_USER_EMAIL
GITHUB_TOKEN=$GITHUB_TOKEN
ALTINN_AT23_SUBSCRIPTION_KEY=$Infrastructure__Altinn__SubscriptionKey
E2E_TOKEN_GENERATOR_USER=$TokenGeneratorUser
E2E_TOKEN_GENERATOR_PASSWORD=$TokenGeneratorPassword
MASKINPORTEN_AT23_CLIENT_ID=none
MASKINPORTEN_AT23_ENCODED_JWK=none
EOF
chmod 600 ~/nested.env
cd /home/agent/code/.worktrees/<task>/agents/minimal
agentctl apply --variant nested-build --name <task>-nested --env-file ~/nested.env --wait
```

`nested-build` builds the image from the worktree, so a change to the `Dockerfile` is tried too. Run in it what
these instructions promise, then delete it. Say in the pull request when a change widens what the agent may reach,
such as hosts or secrets: that is the team's decision.
