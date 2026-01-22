# DIS runtime implementation (Dialogporten)

This document tracks the detailed plan, progress, and open issues for implementing the DIS deployment specification.

## Plan (with progress)
- [x] Capture ACA -> DIS runtime requirements from `container-runtime.md` and `.azure/*/*.bicepparam`.
- [x] Create initial Flux layout skeleton for app-config and syncroot under `flux/`.
- [x] Add base manifests for apps and jobs (Deployment/Service/IngressRoute/HPA/CronJob/Job).
- [~] Add ApplicationIdentity, External Secrets, and RoleAssignment resources (ApplicationIdentity + External Secrets done; RoleAssignments pending).
- [ ] Add environment overlays for replicas/resources/allowlists/schedules/OTEL (initial patches in place; placeholders remain).
- [x] Add syncroot environment patches for GitRepository path.
- [ ] Configure Flux substitution inputs (ConfigMap/Secret in `flux-system`) for required env values.
- [ ] Validate `kustomize build` per env (dotnet build/test skipped per request).

## Runtime mapping summary (from `container-runtime.md`)
### Environments
- `test` -> `at23`
- `staging` -> `tt02`
- `yt01` -> `yt01`
- `prod` -> `prod`

### IP allowlists (web-api-so, web-api-eu, graphql)
- prod: 51.120.88.54/32
- staging: 51.13.86.131/32
- test: 51.13.79.23/32, 51.120.88.69/32
- yt01: 51.13.85.197/32

### HPA targets (per app)
- web-api-so: max 10, CPU 70, memory 70
- web-api-eu: max 20, CPU 50, memory 70
- graphql: max 10, CPU 70, memory 70
- service: max 10, CPU 70, memory 70

### Min replicas (per env)
- prod: min 2 for web-api-so, web-api-eu, graphql, service
- staging/test/yt01: min 1 unless overridden

### Resource overrides (per env)
- prod: web-api-so/web-api-eu/graphql/service = 2 CPU / 4Gi, OTEL ratio 0.2
- yt01: web-api-so/web-api-eu/graphql/service = 2 CPU / 4Gi, OTEL ratio 1
- staging: web-api-so = 1 CPU / 2Gi, OTEL ratio 1
- test: defaults, OTEL ratio 1

### Jobs
- web-api-migration-job: timeout 86400 (all envs)
- sync-resource-policy-information-job: cron 10/15/20/25 3 * * * (prod/staging/test/yt01), timeout 600
- sync-subject-resource-mappings-job: cron */5 * * * * (all envs), timeout 600
- reindex-dialogsearch-job: timeout 172800/600/600/86400 (prod/staging/test/yt01)
- aggregate-cost-metrics-job: prod+staging only, cron 0 2 * * *, timeout 1800, storageContainerName costmetrics

## Open questions
- What is the exact ASO RoleAssignment schema in DIS (principal reference vs principalId)?
- What are the canonical hostnames and paths for Traefik IngressRoute rules per app/environment?
- Where should `AZURE_APPCONFIG_URI`, `APPLICATIONINSIGHTS_CONNECTION_STRING`, and `Infrastructure__MassTransit__Host` be sourced in DIS?
- When do we create and wire the `dialogporten-flux-manifests` repo for image tag updates (repository dispatch scaffold added)?

## Needs more consideration
- Workload profile mapping to node pools (Dedicated-D8 scheduling) and required labels/taints in DIS.
- OTEL collector endpoint and required env vars for DIS workloads (and whether to omit App Insights connection string).
- Strategy for manual job triggers (migration/reindex) in DIS.
- Traefik internal-only access pattern for `service` without an internal entrypoint.
- How to manage secret material (App Insights/App Config/Service Bus host) in Flux without leaking values.
- Replace `set-by-env` placeholders in manifests with real sources (ConfigMap/Secret/ExternalSecret/EnvFrom).
- Determine storage account name/source for `aggregate-cost-metrics-job` (now requires env substitution).
- RoleAssignment CRs are still missing and need the DIS ASO schema before we can implement them.
- Flux `Kustomization` must substitute values from `flux-system` for app-config (ConfigMap/Secret named `dialogporten-flux-substitutions`).
  - Required keys: `DIALOGPORTEN_APPINSIGHTS_CONNECTION_STRING`, `DIALOGPORTEN_AZURE_APPCONFIG_URI`, `DIALOGPORTEN_SERVICEBUS_HOST`, `DIALOGPORTEN_KEY_VAULT_URL`, `DIALOGPORTEN_AZURE_SUBSCRIPTION_ID`, `DIALOGPORTEN_COST_METRICS_STORAGE_ACCOUNT_NAME`.
  - Service Bus format: `sb://<namespace>.servicebus.windows.net/`.
- E2E tests should run only after Flux reports a successful reconciliation; we need a Flux notification/webhook to confirm deployment success.

## Not supported / blocked
- RoleAssignment resources are blocked until DIS ASO schema is confirmed.

## Progress log
- 2026-01-21: Created `flux/` skeleton for app-config and syncroot.
- 2026-01-21: Added base app/job manifests and initial per-env overlay patches (allowlists, schedules, OTEL ratios, resources).
- 2026-01-21: Added ExternalSecrets scaffolding and APIM-aligned ingress path rules.
- 2026-01-21: Added ApplicationIdentity resources for all apps/jobs and the SecretStore ServiceAccount.
- 2026-01-21: Aligned syncroot Flux API versions with platform (v1).
- 2026-01-21: Removed explicit `AZURE_CLIENT_ID` env vars, relying on DIS identity injection.
- 2026-01-21: Introduced `dialogporten-runtime` ConfigMap for App Config URI and Service Bus host placeholders.
- 2026-01-21: Switched syncroot to GitRepository source and added Flux substitution requirements.
- 2026-01-21: Removed OCI artifact build workflows; Flux now pulls directly from Git.
- 2026-01-22: Updated DIS docs to reflect GitRepository-based syncroot and added Flux README with RoleAssignment note.
- 2026-01-22: Added repository-dispatch scaffolding to set image tags in `dialogporten-flux-manifests`.
- 2026-01-22: Moved staging/yt01 dispatch to `ci-cd-staging.yml` and `ci-cd-yt01.yml` to run alongside Bicep deploys.
- 2026-01-22: Added per-environment image tag overrides in `flux/dialogporten/overlays/*` (v.1.100.1) as a temporary base.
