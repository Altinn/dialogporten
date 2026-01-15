# DIS application deployment specification

This specification describes how Dialogporten applications will be deployed on DIS using Flux with OCI syncroot images. It also documents how the current GitHub Actions workflows participate in the deployment flow and what changes are needed for DIS.

## Goal and scope
- Goal: Deploy Dialogporten apps and jobs to DIS-managed Kubernetes using Flux + OCI syncroot images.
- Scope: Application workloads, Traefik ingress, External Secrets, Workload Identity, Kustomize overlays, and CI/CD integration.
- Out of scope: Cluster provisioning and platform-managed components (Flux, Traefik, service mesh).

## Assumptions and decisions
- DIS manages Flux and common cluster resources.
- Traefik is the ingress controller.
- External Secrets is used for Key Vault integration.
- Kustomize for apps, Helm for non-app resources.
- Environment mapping: `test` -> `at23`, `staging` -> `tt02`, `yt01` -> `yt01`, `prod` -> `prod`.
- Use GHCR for app-config and syncroot OCI images unless DIS requires ACR.

## Inputs and sources of truth
- App runtime requirements and per-environment overrides: `container-runtime.md`.
- DIS-specific constraints and layouts: `container-runtime-dis.md`.
- Existing Azure IAC parameters (to translate into overlays): `.azure/*/*.bicepparam`.

## Desired configuration layout (OCI syncroot)
The syncroot OCI image must contain an environment folder at the root, each with a `kustomization.yaml` entry point:

```
/
├── at23
│   └── kustomization.yaml
├── prod
│   └── kustomization.yaml
├── tt02
│   └── kustomization.yaml
└── yt01
    └── kustomization.yaml
```

Only `at23` is required for non-prod right now; add `at22`/`at24` only if DIS requires them later.

Recommended internal layout:
```
/
├── base
│   ├── apps
│   ├── jobs
│   ├── common
│   └── kustomization.yaml
├── overlays
│   ├── at23
│   ├── tt02
│   ├── yt01
│   └── prod
└── <env>
    └── kustomization.yaml
```

## Runtime requirements (summary)
- Traefik `IngressRoute` for public apps with IP allowlists (`web-api-so`, `web-api-eu`, `graphql`).
- `service` exposed via Traefik on `http`/`https` entrypoints with allowlist (no internal entrypoint configured in platform Traefik).
- External Secrets for `dialogportenAdoConnectionString` and `dialogportenRedisConnectionString` via `SecretStore` + WorkloadIdentity `serviceAccountRef`.
- ApplicationIdentity operator creates Workload Identity ServiceAccounts per app/job.
- KEDA ScaledObject for CPU/memory (matching ACA scale rules).
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
We keep release-please for versioning and image publishing, and add a syncroot build and push step.

### New or updated workflows (target)
- Add a reusable workflow `workflow-build-app-config.yml` that:
  - Builds the app-config OCI image from Kustomize overlays.
  - Pushes to GHCR with a versioned tag.
- Add a reusable workflow `workflow-build-syncroot.yml` that:
  - Builds the syncroot OCI image from the Kustomize overlays.
  - Pushes to the team registry path with a versioned tag.
  - Optionally signs the OCI artifact (if required by DIS).
- Update `ci-cd-main.yml`:
  - After `workflow-publish.yml`, build and push app-config + syncroot for `at23` (test) using the same version tag.
- Update `ci-cd-release-please.yml`:
  - After `workflow-publish.yml`, build and push app-config + syncroot for `tt02` and `yt01` using the release version.
- Update `ci-cd-prod.yml`:
  - After manual version input, build and push app-config + syncroot for `prod`.
- Optional: add `dispatch-syncroot.yml` for manual syncroot builds.

### Syncroot versioning strategy
- Use the same version tag as container images (`<semver>` or `<semver>-<sha>`).
- Reference image tags via `images` in Kustomize overlays to keep config and version aligned.
- Flux should reconcile the latest tag or semver range as configured by DIS.

## Implementation plan
1. Create Kustomize base resources for apps and jobs (Deployments, Services, IngressRoutes, KEDA ScaledObject, CronJobs/Jobs).
2. Add ApplicationIdentity resources and External Secrets resources.
3. Add Traefik ingress and middleware resources:
   - IP allowlists for public apps.
   - Internal-only entrypoint for `service`.
4. Create environment overlays with per-env overrides (replicas, resources, allowlists, schedules, OTEL ratios).
5. Build and publish the app-config and syncroot OCI images using Flux CLI in CI.
6. Update GitHub Actions to publish app-config + syncroot OCI images in the same flows that publish container images.
7. Validate the end-to-end flow by confirming Flux reconciliation and app health endpoints.
8. Define rollback: re-publish or re-pin the previous syncroot OCI tag.

## Validation and rollback
- Validate Kustomize output in CI (`kustomize build` per environment).
- Validate Flux reconciliation and rollout status.
- Verify health endpoints (`/health/startup`, `/health/readiness`, `/health/liveness`).
- Roll back by reverting to the previous syncroot OCI tag/digest.

## Open items
- External Secrets store name, Key Vault URL, and service account name per environment.
- Whether `AZURE_CLIENT_ID` is injected automatically from the ServiceAccount annotation.
- ASO RoleAssignment schema details (principal reference vs principalId).
