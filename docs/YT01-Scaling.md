# YT01 Scaling

The YT01 environment is scaled down outside working hours to save costs. A set of GitHub Actions workflows handle scaling the environment on and off, with support for postponing the scheduled shutdown.

## Scaling On

Use the **manual workflow** (`dispatch-scale-yt01-manual.yml`) with `state: on`.

Inputs:

| Input | Required | Default | Description |
|-------|----------|---------|-------------|
| `state` | Yes | — | Set to `on` |
| `deploy` | No | `true` | If a newer release exists, deploy it automatically |
| `postpone_until` | No | — | ISO 8601 datetime to postpone the scheduled shutdown until (e.g. `2026-03-01T20:00:00+01:00`) |

What happens when scaling on:
1. Workload profile nodes are scaled up
2. PostgreSQL server is started
3. Container app revisions are activated
4. Scheduled container app jobs are resumed
5. Public network access is enabled
6. Availability test (health check) is enabled
7. If `deploy` is true and a newer release exists, the CI/CD workflow is triggered

> **Note:** If a new version is deployed during scale-on, wait for the deployment workflow to finish before running tests against the environment. The deploy runs asynchronously, so the environment may still be starting up with the new version.

## Scaling Off

Use the **manual workflow** (`dispatch-scale-yt01-manual.yml`) with `state: off`.

This always clears any active postpone before shutting down, so a manual scale-off cannot be blocked by a postpone.

## Scheduled Shutdown

The **scheduled workflow** (`dispatch-scale-yt01-scheduled.yml`) runs daily at **16:00 UTC** (17:00 CET / 18:00 CEST).

Before scaling down, it checks for an active postpone. If the postpone timestamp is still in the future, the shutdown is skipped. If the timestamp has passed, the postpone is cleared and shutdown proceeds.

The scheduled workflow cannot be triggered manually. This is by design — manual scale-off should go through the manual workflow to properly clear postpone state.

## Postpone Behavior

Postponing prevents the scheduled shutdown from running until a given time. Key points:

- Set via the `postpone_until` input when scaling on (e.g. if you need the environment to stay up past 16:00 UTC)
- **Can be set even when the environment is already running** — it just sets a future timestamp that the scheduled shutdown checks
- The timestamp is stored as a GitHub environment variable (`YT01_DB_SHUTDOWN_POSTPONE_UNTIL`)
- The scheduled workflow checks this variable each day. If the timestamp is in the future, shutdown is skipped
- Stale (past-dated) postpone values are automatically cleaned up by both the manual and scheduled workflows

### Examples

Keep the environment running until 20:00 CET tonight:
- Run the manual workflow with `state: on` and `postpone_until: 2026-04-08T20:00:00+02:00`
- Or, if the environment is already running, just run it with `state: on` and the `postpone_until` value — the scale-on is idempotent

Immediately shut down regardless of any postpone:
- Run the manual workflow with `state: off` — this clears the postpone first

## Scale-Down Order

The scale-down sequence is ordered so that resources are available when needed:

1. Disable availability test (health check monitoring)
2. **Purge E2E test data** — runs before anything else is disabled, since it needs the API and database
3. Disable public network access
4. Deactivate container app revisions (all apps in parallel)
5. Pause scheduled container app jobs (saves original cron schedules for restore)
6. Scale workload profile nodes to 0
7. Stop PostgreSQL server

This sequence is shared between manual and scheduled scale-off via the reusable workflow `workflow-scale-yt01-down.yml`.

## Workflow Files

| File | Purpose |
|------|---------|
| `dispatch-scale-yt01-manual.yml` | Manual scale on/off with optional postpone |
| `dispatch-scale-yt01-scheduled.yml` | Daily scheduled shutdown at 16:00 UTC |
| `workflow-scale-yt01-down.yml` | Reusable scale-down logic (called by both manual and scheduled) |
