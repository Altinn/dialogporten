# DIS runtime implementation (Dialogporten)

This document tracks the detailed plan, progress, and open issues for implementing the DIS deployment specification.

## Plan (with progress)
- [x] Capture ACA -> DIS runtime requirements from `container-runtime.md` and `.azure/*/*.bicepparam`.
- [x] Create initial Flux layout skeleton for app-config and syncroot images under `flux/`.
- [x] Add base manifests for apps and jobs (Deployment/Service/IngressRoute/HPA/CronJob/Job).
- [~] Add ApplicationIdentity, External Secrets, and RoleAssignment resources (ApplicationIdentity + External Secrets done; RoleAssignments pending).
- [ ] Add environment overlays for replicas/resources/allowlists/schedules/OTEL (initial patches in place; placeholders remain).
- [ ] Add syncroot environment patches for app-config tag wiring.
- [ ] Implement CI workflows for app-config and syncroot OCI images.
- [ ] Validate `kustomize build` per env and run `dotnet build` + `dotnet test`.

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
- Does DIS bridge ApplicationIdentity annotations to workload identity env var injection (AZURE_CLIENT_ID)?
- Are GHCR OCIRepositories supported in DIS, or must we use ACR?
- Which Flux API versions (v1beta2 vs v1) are required by DIS?
- What are the canonical hostnames and paths for Traefik IngressRoute rules per app/environment?
- Where should `AZURE_APPCONFIG_URI`, `APPLICATIONINSIGHTS_CONNECTION_STRING`, and `Infrastructure__MassTransit__Host` be sourced in DIS?

## Needs more consideration
- Workload profile mapping to node pools (Dedicated-D8 scheduling) and required labels/taints in DIS.
- OTEL collector endpoint and required env vars for DIS workloads (and whether to omit App Insights connection string).
- Strategy for manual job triggers (migration/reindex) in DIS.
- Traefik internal-only access pattern for `service` without an internal entrypoint.
- How to manage secret material (App Insights/App Config/Service Bus host) in Flux without leaking values.
- Replace `set-by-env` placeholders in manifests with real sources (ConfigMap/Secret/ExternalSecret/EnvFrom).
- Confirm SecretStore identity approach (currently `dialogporten-secrets` ApplicationIdentity).

## Not supported / blocked
- RoleAssignment resources are blocked until DIS ASO schema is confirmed.
- Workload identity env var injection is blocked until DIS confirms bridging behavior.

## Progress log
- 2026-01-21: Created `flux/` skeleton for app-config and syncroot images.
- 2026-01-21: Added base app/job manifests and initial per-env overlay patches (allowlists, schedules, OTEL ratios, resources).
- 2026-01-21: Added ExternalSecrets scaffolding and APIM-aligned ingress path rules.
- 2026-01-21: Added ApplicationIdentity resources for all apps/jobs and the SecretStore ServiceAccount.
