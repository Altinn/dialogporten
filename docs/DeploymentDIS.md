# DIS application deployment specification

This specification describes how Dialogporten applications will be deployed on DIS using Flux with a GitRepository-based syncroot (no OCI artifacts). It also documents how the current GitHub Actions workflows participate in the deployment flow and what changes are needed for DIS.

## Goal and scope
- Goal: Deploy Dialogporten apps and jobs to DIS-managed Kubernetes using Flux + GitRepository syncroot.
- Scope: Application workloads, Traefik ingress, External Secrets, Workload Identity, Kustomize overlays, and CI/CD integration.
- Out of scope: Cluster provisioning and platform-managed components (Flux, Traefik, service mesh).

## Assumptions and decisions
- DIS manages Flux and common cluster resources.
- Traefik is the ingress controller.
- External Secrets is used for Key Vault integration.
- Kustomize for apps, Helm for non-app resources.
- Environment mapping: `test` -> `at23`, `staging` -> `tt02`, `yt01` -> `yt01`, `prod` -> `prod`.
- Flux syncroot pulls app-config directly from this repository via `GitRepository` (branch `main`).
- No app-config or syncroot OCI images are built or published.

## Inputs and sources of truth
- App runtime requirements and per-environment overrides: `container-runtime.md`.
- DIS-specific constraints and layouts: `container-runtime-dis.md`.
- Existing Azure IAC parameters (to translate into overlays): `.azure/*/*.bicepparam`.

## Desired configuration layout (GitRepository syncroot)
Flux points to this repository and applies the environment overlays under `flux/dialogporten`.

```
flux/
├── dialogporten
│   ├── base
│   │   ├── apps
│   │   ├── jobs
│   │   ├── common
│   │   └── kustomization.yaml
│   ├── overlays
│   │   ├── at23
│   │   ├── tt02
│   │   ├── yt01
│   │   └── prod
│   └── kustomization.yaml
└── syncroot
    ├── base
    │   ├── dialogporten-namespace.yaml
    │   ├── dialogporten-git-repository.yaml
    │   ├── dialogporten-flux-kustomization.yaml
    │   └── kustomization.yaml
    ├── at23
    │   └── kustomization.yaml
    ├── tt02
    │   └── kustomization.yaml
    ├── yt01
    │   └── kustomization.yaml
    └── prod
        └── kustomization.yaml
```

`flux/syncroot/<env>/kustomization.yaml` patches the `spec.path` in the Flux `Kustomization` to point at `./flux/dialogporten/overlays/<env>`.

Only `at23` is required for non-prod right now; add `at22`/`at24` only if DIS requires them later.

## Runtime requirements (summary)
- Traefik `IngressRoute` for public apps with IP allowlists (`web-api-so`, `web-api-eu`, `graphql`).
- `service` exposed via Traefik on `http`/`https` entrypoints with allowlist (no internal entrypoint configured in platform Traefik).
- External Secrets for `dialogportenAdoConnectionString` and `dialogportenRedisConnectionString` via `SecretStore` + WorkloadIdentity `serviceAccountRef`.
- ApplicationIdentity operator creates Workload Identity ServiceAccounts per app/job.
- HPA for CPU/memory (matching ACA scale rules).
- Resource requests/limits per environment (from `container-runtime.md`).

## Azure RBAC permissions
Permissions are granted via Azure Service Operator RoleAssignment resources (`authorization.azure.com`) in the app-config overlays.

Baseline mapping:
- App Configuration Data Reader: `web-api-so`, `web-api-eu`, `graphql`, `service`.
- Service Bus Data Owner: `service`.
- Monitoring Reader + Storage Blob Data Contributor: `aggregate-cost-metrics-job`.
- Key Vault roles are not required for apps/jobs when External Secrets is used.

## Current GitHub Actions workflows (summary)
Key workflows in this repo:
- `ci-cd-main.yml`: on push to `main`, builds/tests and deploys to test; publishes images with version plus git short SHA.
- `ci-cd-release-please.yml`: on push to `main`, runs release-please; if release created, builds and publishes images via `workflow-publish.yml`, then triggers staging and yt01 deployments via repository dispatch.
- `ci-cd-staging.yml` and `ci-cd-yt01.yml`: deploy apps and infra on release creation.
- `ci-cd-prod-dry-run.yml`: production dry run on release creation.
- `ci-cd-prod.yml`: manual production deployment.
- `workflow-publish.yml`: builds and pushes images for `webapi`, `graphql`, `service`, `migration-bundle`, `janitor`.
- `dispatch-apps.yml` and `dispatch-infrastructure.yml`: manual deployments to ACA.
- `ci-cd-pull-request-release-please.yml`: dry run staging deployment for release PRs.

Note: `ci-cd-release-please.yml` is the workflow that builds and publishes release images.

## Proposed DIS CI/CD integration
- Keep the existing release/test image publishing workflows for container images.
- No additional workflows are needed for app-config or syncroot because Flux pulls from Git.
- Config changes take effect when merged to `main`; Flux reconciles the new revision.
- Image tag update strategy (workflow-driven or Flux Image Automation) is still to be decided.
- Add repository dispatch scaffolding to update image tags in a separate `dialogporten-flux-manifests` repo.
  - This repo must be created before the dispatch can be enabled.
  - Requires a `FLUX_MANIFESTS_DISPATCH_TOKEN` secret with access to that repo.
- Keep the current Bicep/ACA deploy steps in place for now; dispatch runs alongside them.

## Implementation plan
1. Create Kustomize base resources for apps and jobs (Deployments, Services, IngressRoutes, HorizontalPodAutoscaler, CronJobs/Jobs).
2. Add ApplicationIdentity resources and External Secrets resources.
3. Add Traefik ingress and middleware resources:
   - IP allowlists for public apps.
   - Internal-only access pattern for `service` via allowlists on `http`/`https`.
4. Create environment overlays with per-env overrides (replicas, resources, allowlists, schedules, OTEL ratios).
5. Create `flux/syncroot/base` with Namespace + Flux `GitRepository` + `Kustomization`.
6. Create `flux/syncroot/<env>` kustomizations that patch `spec.path` for each environment.
7. Provide a `dialogporten-flux-substitutions` ConfigMap/Secret in `flux-system` for runtime values.
8. Validate the end-to-end flow by confirming Flux reconciliation and app health endpoints.
9. Define rollback by reverting to a previous Git revision (or pinning the GitRepository ref).

## Validation and rollback
- Validate Kustomize output in CI (`kustomize build` per environment).
- Validate Flux reconciliation and rollout status.
- Verify health endpoints (`/health/startup`, `/health/readiness`, `/health/liveness`).
- Roll back by reverting Git changes or pinning `GitRepository.spec.ref` to a previous commit/tag.

## Open items
- External Secrets store name, Key Vault URL, and service account name per environment.
- Whether `AZURE_CLIENT_ID` is injected automatically from the ServiceAccount annotation.
- ASO RoleAssignment schema details (principal reference vs principalId).
- Whether DIS requires GitRepository auth for GitHub (public vs private).

## Platform gaps to plan for
- No RoleAssignment CR examples in platform Flux; we need to define our own `authorization.azure.com` RoleAssignments.
- No internal Traefik entrypoint exists; internal-only access must be enforced via allowlists on `http`/`https`.
- If GitRepository auth is required, syncroot must include a secret reference.
