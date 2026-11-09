## Improving this agent

This agent's definition is `agents/` in `Altinn/dialogporten`: the `Dockerfile` and `smoke.sh` for the image, the
boot unit in `common/`, these instructions, the skills in `_skills/` and the manifests in `minimal/`. When it gets in
the way of the work, such as a missing tool or a wrong instruction, propose a change to it in a pull request, as the
`agent-definition` skill describes when it is available.

Try the change before you propose it, from your worktree. For a change to the image, build it with Podman and run
the smoke test in it, as CI does:

```sh
cd /home/agent/code/.worktrees/<task>
podman build --target minimal -f agents/Dockerfile -t agent-minimal:<task> agents
podman run --rm --entrypoint bash -v "$PWD/agents/smoke.sh:/smoke.sh:ro" -v "$PWD/global.json:/global.json:ro" \
  agent-minimal:<task> /smoke.sh
podman rmi agent-minimal:<task>
```

For a change to the boot unit, run the new script on this computer with `sudo`, as the unit does. For a change to the
instructions or skills, follow them yourself on the task at hand. Say in the pull request what you could not try, and
when a change widens what the agent may reach, such as hosts or secrets: that is the team's decision.
