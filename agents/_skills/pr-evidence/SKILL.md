---
name: pr-evidence
description: Show pull request reviewers what a change does, with the checks that ran and their results, and a terminal recording where a workflow or command output changed. Use when opening or updating a pull request on Dialogporten.
---

# Show the change to reviewers

Reviewers should see what was run and what came out of it without running it themselves.

## Checks

Fill the template's Verification section with what actually ran, on the pull request's head commit. Copy the
summary lines, not the whole log:

```markdown
## Verification

- `dotnet build Digdir.Domain.Dialogporten.slnx --configuration Release`: 0 warnings, 0 errors
- `dotnet test … --filter 'FullyQualifiedName!~E2E'`: Passed 1234, Failed 0, Skipped 3
- `scripts/e2e/run-webapi-e2e.zsh both`: WebApi E2E Passed 210, GraphQL E2E Passed 45
```

Name each check you did not run, and why, such as missing AT23 credentials. Tick a template box only for what you
did.

## Recordings

For a change to a script, a tool or command output, record it in a terminal, as [terminal](references/terminal.md)
describes. Keep artifacts under `/home/agent/code/.artifacts/<task>/`, outside the checkout, each under 10 MiB and
free of secrets: the AT23 values in the environment are placeholders, but tokens the apps receive are real.

From the artifact directory, use local file references in the body and pass one `--attach` per file, which uploads
them and rewrites the references:

```sh
gh pr create --repo Altinn/dialogporten --base main --head <branch> \
  --title 'fix: ...' --body-file pr-body.md --attach ./result.gif
gh pr edit <number> --attach ./result.gif
```

If an upload fails, look at the pull request before retrying, since it may exist already. Read the body back with
`gh pr view <number> --json body -q .body` and check that attachments have hosted URLs.
