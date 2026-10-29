## Improving this agent

This agent's definition is `agents/` in `Altinn/dialogporten`: the `Dockerfile` and `smoke.sh` for the image, the
boot unit in `common/`, these instructions, the skills in `_skills/` and the manifests in `minimal/`. When it gets in
the way of the work, such as a missing tool or a wrong instruction, propose a change to it in a pull request, as the
`agent-definition` skill describes when it is available.

Try the change before you propose it, in a nested agent from your worktree:

```sh
cd /home/agent/code/.worktrees/<task>/agents/minimal
agentctl apply --variant nested-build --name <task>-nested --env-file ~/nested.env --wait
```

`nested-build` builds the image from the worktree, so a change to the `Dockerfile` is tried too. Run in it what
these instructions promise, then delete it. Say in the pull request when a change widens what the agent may reach,
such as hosts or secrets: that is the team's decision.
