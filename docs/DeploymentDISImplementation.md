# DIS deployment implementation plan (Dialogporten)

This document expands on `docs/DeploymentDIS.md` and translates the DIS model into a concrete implementation plan for Dialogporten. It uses the layout and Flux pattern from `/Users/arealmaas/code/digdir/altinn-correspondence/flux` as a reference, while keeping our preferred internal structure.

## Reference pattern (altinn-correspondence)
The correspondence repo uses OCI images and a syncroot that bootstraps Flux. For Dialogporten, we keep the same repository layout but use a `GitRepository` source instead of OCI artifacts.

Key takeaways for Dialogporten:
- Syncroot wires Flux to the app-config overlays.
- App-config lives in this repo under `flux/dialogporten` (no OCI image).
- Namespaces and app identity are defined declaratively in app config or syncroot.

## Proposed repo layout (Dialogporten)
We keep our preferred layout while matching the syncroot pattern:

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

Notes:
- `flux/dialogporten` is the app-config source (Kustomize base + overlays).
- `flux/syncroot` defines Namespace + Flux `GitRepository` + `Kustomization` wiring.
- Environment mapping: `test` -> `at23`, `staging` -> `tt02`, `yt01` -> `yt01`, `prod` -> `prod`.
- Only `at23` is required for non-prod right now; add `at22`/`at24` only if DIS requires them later.

## Flux source and versioning
- Flux pulls from `https://github.com/Altinn/dialogporten` (branch `main`).
- The Flux `Kustomization` path is patched per environment to `./flux/dialogporten/overlays/<env>`.
- No app-config or syncroot OCI artifacts are produced.
- Rollback is handled by reverting Git commits or pinning `GitRepository.spec.ref` to a previous commit/tag.

## Flux resources in syncroot (what we need)
- Namespace for dialogporten (include `linkerd.io/inject: enabled`).
- `GitRepository` referencing this repo and branch.
- `Kustomization` referencing the `GitRepository` with `spec.path` pointing to the environment overlay.
- `postBuild.substituteFrom` to pull runtime values from `flux-system`.

Required substitutions (ConfigMap or Secret `dialogporten-flux-substitutions` in `flux-system`):
- `DIALOGPORTEN_APPINSIGHTS_CONNECTION_STRING`
- `DIALOGPORTEN_AZURE_APPCONFIG_URI`
- `DIALOGPORTEN_KEY_VAULT_URL`
- `DIALOGPORTEN_SERVICEBUS_HOST` (format: `sb://<namespace>.servicebus.windows.net/`)
- `DIALOGPORTEN_AZURE_SUBSCRIPTION_ID`
- `DIALOGPORTEN_COST_METRICS_STORAGE_ACCOUNT_NAME` (staging/prod only)

## App-config resources (what we need)
- Deployment, Service, and Traefik IngressRoute per app.
- CronJobs and Jobs for janitor/migration workloads.
- `ApplicationIdentity` resource per app/job (DIS operator).
- ServiceAccount per app/job created by the operator (same name as `ApplicationIdentity`).
- External Secrets for Key Vault integration.
- HPA resources mirroring ACA scale rules.
- Traefik `http`/`https` entrypoints with allowlist for `service` (no internal entrypoint configured in platform Traefik).

Identity details from DIS operator:
- Creates a user-assigned managed identity named `<namespace>-<applicationidentity-name>`.
- Creates federated credentials for the ServiceAccount subject `system:serviceaccount:<namespace>:<name>`.
- Annotates the ServiceAccount with `serviceaccount.azure.com/azure-identity: <clientId>`.
- Pods should set `serviceAccountName` to the ApplicationIdentity name.
Platform examples (`flux/otel-collector`, `flux/lakmus`) use `azure.workload.identity/use: "true"` and `azure.workload.identity/client-id` annotations, with env vars like `AZURE_CLIENT_ID` injected by the workload identity webhook. The ApplicationIdentity operator uses a different annotation, so confirm whether DIS bridges this automatically or we must set env vars and labels ourselves.

## Azure RBAC permissions (role assignments)
The DIS identity operator creates the managed identity, but it does not grant Azure roles. Add RoleAssignment resources using Azure Service Operator (`authorization.azure.com`) in the app-config overlays.

Recommended mapping (matches current Bicep behavior):
- `web-api-so`, `web-api-eu`, `graphql`, `service`: App Configuration Data Reader (App Config).
- `service`: Azure Service Bus Data Owner (Service Bus namespace).
- `aggregate-cost-metrics-job`: Monitoring Reader (App Insights) + Storage Blob Data Contributor (storage account).
- Key Vault access: handled by External Secrets; apps/jobs generally do not need Key Vault roles.
  - If any workload must read Key Vault directly, add Key Vault Secrets User for that identity.

Implementation approach:
- Create `RoleAssignment` CRs in the same namespace as the app.
- Reference the ApplicationIdentity-created managed identity as the principal.
- Scope the role assignment to the target resource (App Config, Service Bus, App Insights, Storage).
TODO: confirm the exact ASO RoleAssignment schema (principal reference vs principalId) in DIS.

### HPA skeleton (CPU/memory)
Match the ACA CPU/memory utilization rules from `.azure/modules/containerApp/main.bicep` and app-specific overrides.

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: <app-name>
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: <deployment-name>
  minReplicas: <min-replicas>
  maxReplicas: <max-replicas>
  metrics:
    - type: Resource
      resource:
        name: cpu
        target:
          type: Utilization
          averageUtilization: <cpu-percent>
    - type: Resource
      resource:
        name: memory
        target:
          type: Utilization
          averageUtilization: <memory-percent>
```

Decision: use per-app HPA manifests under `flux/dialogporten/base/apps/<app>/hpa.yaml`. The thresholds differ per app, so a shared HPA base adds more patching than it saves.

Confirmed per-app values (from `.azure/applications/*/main.bicep`):
- `web-api-so`: max 10, cpu 70, memory 70.
- `web-api-eu`: max 20, cpu 50, memory 70.
- `graphql`: max 10, cpu 70, memory 70.
- `service`: max 10, cpu 70, memory 70.

Per-environment mins (from `.azure/applications/*/*.bicepparam`):
- `prod`: min 2 for `web-api-so`, `web-api-eu`, `graphql`, `service`.
- `staging`, `test`, `yt01`: default min 1 unless overridden.

### Kustomize overlay examples (per-environment patches)
Base HPA (example for `web-api-eu`, match ACA values):
```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: web-api-eu
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: web-api-eu
  minReplicas: 1
  maxReplicas: 20
  metrics:
    - type: Resource
      resource:
        name: cpu
        target:
          type: Utilization
          averageUtilization: 50
    - type: Resource
      resource:
        name: memory
        target:
          type: Utilization
          averageUtilization: 70
```

Prod overlay patch (min replicas to 2):
```yaml
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization
resources:
  - ../../base
patches:
  - target:
      kind: HorizontalPodAutoscaler
      name: web-api-eu
    patch: |-
      apiVersion: autoscaling/v2
      kind: HorizontalPodAutoscaler
      metadata:
        name: web-api-eu
      spec:
        minReplicas: 2
```

Repeat the same patch pattern for `web-api-so`, `graphql`, and `service` in `prod` to match min=2. For `at23` and `tt02`, keep min=1 (base default) unless overridden.
Note: In implementation, add overlays for IP allowlists, resource requests/limits, and job schedules/timeouts. They are not expanded here.

## CI/CD integration (existing + required changes)
Existing workflows:
- `ci-cd-main.yml`: builds/tests and deploys to test; publishes images with version + git short SHA.
- `ci-cd-release-please.yml`: builds and publishes release images via `workflow-publish.yml`, then triggers staging/yt01 deployments.
- `ci-cd-staging.yml`, `ci-cd-yt01.yml`: deploy apps/infra on release creation.
- `ci-cd-prod-dry-run.yml`: production dry run on release creation.
- `ci-cd-prod.yml`: manual production deployment.
- `workflow-publish.yml`: builds/pushes application images.

Required changes:
- No app-config or syncroot OCI workflows are required; Flux pulls directly from Git.
- Config-only changes should merge to `main` and let Flux reconcile.
- Image tag update strategy (workflow-driven or Flux image automation) is still an open decision.
- Add repository dispatch scaffolding to update image tags in a separate `dialogporten-flux-manifests` repo.
  - This repo does not exist yet and must be created before the dispatch can be enabled.
  - Requires a `FLUX_MANIFESTS_DISPATCH_TOKEN` secret with access to that repo.
- Keep the current Bicep/ACA deploy steps in place for now; dispatch runs alongside them.
- Run E2E tests only after a Flux reconciliation success signal; add a Flux Alert + provider webhook to trigger E2E on success.
  - Example Alert (spec outline):
    - `apiVersion: notification.toolkit.fluxcd.io/v1beta3`
    - `kind: Alert`
    - `spec.eventSeverity: info`
    - `spec.eventSources` referencing the app `Kustomization`
    - `spec.inclusionList` with `Reconciliation.*succeeded`

## Implementation steps
1. Create `flux/dialogporten/base` with app and job resources.
2. Add `ApplicationIdentity`, ServiceAccounts, External Secrets, and HPA resources.
3. Add Traefik IngressRoutes and IP allowlist middleware for public apps.
4. Add overlays per env (at23, tt02, yt01, prod) with:
   - replicas, resource requests/limits, HPA CPU/memory targets
   - IP allowlists
   - schedules/timeouts for jobs
   - OTEL sampling ratio
5. Create `flux/syncroot/base` with Namespace + Flux `GitRepository` + `Kustomization`.
6. Create `flux/syncroot/<env>` kustomizations that patch `spec.path` for each environment.
7. Provide `dialogporten-flux-substitutions` in `flux-system` for runtime values.
8. Validate Flux reconciliation and health endpoints after each merge.
9. Define rollback by reverting Git or pinning the GitRepository to a prior commit/tag.

## Validation and rollback
- Run `kustomize build` for each env overlay in CI.
- Confirm Flux reconciles new Git revisions and reports healthy status.
- Verify health endpoints: `/health/startup`, `/health/readiness`, `/health/liveness`.
- Roll back by reverting Git changes or pinning the GitRepository ref to a previous commit/tag.

## Example: flux manifests repo image tag updates
When `dialogporten-flux-manifests` receives a repository dispatch, it should update the image tags
for the target environment overlay. One simple approach is to keep environment overlays with
`images` overrides in their `kustomization.yaml`.

Example repo layout:
```
dialogporten-flux-manifests/
├── overlays
│   ├── at23
│   │   └── kustomization.yaml
│   ├── tt02
│   │   └── kustomization.yaml
│   ├── yt01
│   │   └── kustomization.yaml
│   └── prod
│       └── kustomization.yaml
└── base
    └── kustomization.yaml
```

Example `overlays/at23/kustomization.yaml` (repeat per env with the same tag when deploying
`v.1.100.1`):
```yaml
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization
resources:
  - ../../base
images:
  - name: ghcr.io/altinn/dialogporten-webapi
    newTag: v.1.100.1
  - name: ghcr.io/altinn/dialogporten-graphql
    newTag: v.1.100.1
  - name: ghcr.io/altinn/dialogporten-service
    newTag: v.1.100.1
  - name: ghcr.io/altinn/dialogporten-migration-bundle
    newTag: v.1.100.1
  - name: ghcr.io/altinn/dialogporten-janitor
    newTag: v.1.100.1
```

If you prefer patches instead of `images`, an alternative is a strategic merge patch per env that
targets each Deployment/Job image, but the `images` stanza is the most compact for tag updates.

## TODOs
- Confirm External Secrets store name, Key Vault URL, and service account name per environment.
- Confirm whether `AZURE_CLIENT_ID` is injected from the ServiceAccount annotation or needs to be set explicitly.
- Confirm the ASO RoleAssignment schema (principal reference vs principalId) in DIS.
- Confirm whether GitRepository auth is required for GitHub (public vs private).

## Platform gaps to plan for
- No RoleAssignment CR examples in platform Flux; we need to define our own `authorization.azure.com` RoleAssignments.
- No internal Traefik entrypoint exists; internal-only access must be enforced via allowlists on `http`/`https`.
- If GitRepository auth is required, syncroot must include a secret reference.
