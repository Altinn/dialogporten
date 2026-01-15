# DIS deployment implementation plan (Dialogporten)

This document expands on `docs/DeploymentDIS.md` and translates the DIS model into a concrete implementation plan for Dialogporten. It uses the layout and Flux pattern from `/Users/arealmaas/code/digdir/altinn-correspondence/flux` as a reference, while keeping our preferred internal structure.

## Reference pattern (altinn-correspondence)
The correspondence repo uses two OCI images and a syncroot that bootstraps Flux:
- `flux/syncroot/base` contains `Namespace`, `OCIRepository`, and `Kustomization` resources.
- `flux/syncroot/<env>/kustomization.yaml` references `../base`.
- `flux/correspondence/` contains the app deployment resources (Deployment, Service, IngressRoute, ApplicationIdentity).

Key takeaways for Dialogporten:
- Syncroot is responsible for wiring Flux to the app-config OCI image.
- The app-config OCI image is separate from application container images.
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
    │   ├── dialogporten-oci-repository.yaml
    │   ├── dialogporten-flux-kustomize.yaml
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
- `flux/dialogporten` is the app-config OCI image (Kustomize base + overlays).
- `flux/syncroot` is the DIS syncroot OCI image (Flux wiring only).
- Environment mapping: `test` -> `at23`, `staging` -> `tt02`, `yt01` -> `yt01`, `prod` -> `prod`.
- Only `at23` is required for non-prod right now; add `at22`/`at24` only if DIS requires them later.

## OCI images and registry convention
We publish three kinds of OCI artifacts:
1. Application container images (already in GHCR via `workflow-publish.yml`):
   - `ghcr.io/altinn/dialogporten-webapi`
   - `ghcr.io/altinn/dialogporten-graphql`
   - `ghcr.io/altinn/dialogporten-service`
   - `ghcr.io/altinn/dialogporten-migration-bundle`
   - `ghcr.io/altinn/dialogporten-janitor`

2. App-config OCI image (new):
   - `ghcr.io/altinn/dialogporten-config`
   - Built from `flux/dialogporten`.

3. Syncroot OCI image (new):
   - `ghcr.io/altinn/dialogporten-syncroot`
   - Built from `flux/syncroot`.

Registry note: these URLs are defined in the `OCIRepository` resources under `flux/syncroot`, not in Bicep. We will use GHCR unless DIS requires ACR.

Tagging strategy:
- Use the same version tag as application container images.
- `ci-cd-main.yml` uses `<semver>-<sha>` for test; release workflows use `<semver>`.

## Flux resources in syncroot (what we need)
- Namespace for dialogporten (include `linkerd.io/inject: enabled`).
- `OCIRepository` referencing the app-config OCI image and tag.
- `Kustomization` referencing the `OCIRepository` with `spec.path` pointing to the environment overlay.

For env-specific overlays, patch `spec.path` in each `syncroot/<env>/kustomization.yaml` to point to `./overlays/<env>`.

## App-config resources (what we need)
- Deployment, Service, and Traefik IngressRoute per app.
- CronJobs and Jobs for janitor/migration workloads.
- `ApplicationIdentity` resource per app/job (DIS operator).
- ServiceAccount per app/job created by the operator (same name as `ApplicationIdentity`).
- External Secrets for Key Vault integration.
- KEDA ScaledObject resources mirroring ACA scale rules.
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

### KEDA ScaledObject skeleton (CPU/memory)
Match the ACA CPU/memory utilization rules from `.azure/modules/containerApp/main.bicep` and app-specific overrides.

```yaml
apiVersion: keda.sh/v1alpha1
kind: ScaledObject
metadata:
  name: <app-name>
spec:
  scaleTargetRef:
    name: <deployment-name>
  minReplicaCount: <min-replicas>
  maxReplicaCount: <max-replicas>
  triggers:
    - type: cpu
      metadata:
        type: Utilization
        value: "<cpu-percent>"
    - type: memory
      metadata:
        type: Utilization
        value: "<memory-percent>"
```

Per-app values (match current Bicep):
- `web-api-so`: max 10, cpu 70, memory 70.
- `web-api-eu`: max 20, cpu 50, memory 70.
- `graphql`: max 10, cpu 70, memory 70.
- `service`: max 10, cpu 70, memory 70.

Per-environment mins (from `.azure/applications/*/*.bicepparam`):
- `prod`: min 2 for `web-api-so`, `web-api-eu`, `graphql`, `service`.
- `staging`, `test`, `yt01`: default min 1 unless overridden.

### Kustomize overlay examples (per-environment patches)
Base ScaledObject (example for `web-api-eu`, match ACA values):
```yaml
apiVersion: keda.sh/v1alpha1
kind: ScaledObject
metadata:
  name: web-api-eu
spec:
  scaleTargetRef:
    name: web-api-eu
  minReplicaCount: 1
  maxReplicaCount: 20
  triggers:
    - type: cpu
      metadata:
        type: Utilization
        value: "50"
    - type: memory
      metadata:
        type: Utilization
        value: "70"
```

Prod overlay patch (min replicas to 2):
```yaml
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization
resources:
  - ../../base
patches:
  - target:
      kind: ScaledObject
      name: web-api-eu
    patch: |-
      apiVersion: keda.sh/v1alpha1
      kind: ScaledObject
      metadata:
        name: web-api-eu
      spec:
        minReplicaCount: 2
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

Required additions:
- Add `workflow-build-app-config.yml` to build and push the app-config OCI image from `flux/dialogporten`.
- Add `workflow-build-syncroot.yml` to build and push the syncroot OCI image from `flux/syncroot`, embedding the app-config tag.
- Extend `ci-cd-main.yml` to publish app-config + syncroot for `at23` after `workflow-publish.yml`.
- Extend `ci-cd-release-please.yml` to publish app-config + syncroot for `tt02` and `yt01` when a release is created.
- Extend `ci-cd-prod.yml` to publish app-config + syncroot for `prod` on manual release.
- Optional: add `dispatch-syncroot.yml` for manual syncroot pushes.

## Implementation steps
1. Create `flux/dialogporten/base` with app and job resources.
2. Add `ApplicationIdentity`, ServiceAccounts, External Secrets, and KEDA ScaledObject resources.
3. Add Traefik IngressRoutes and IP allowlist middleware for public apps.
4. Add overlays per env (at23, tt02, yt01, prod) with:
   - replicas, resource requests/limits, KEDA CPU/memory thresholds
   - IP allowlists
   - schedules/timeouts for jobs
   - OTEL sampling ratio
5. Create `flux/syncroot/base` with Namespace + Flux `OCIRepository` + `Kustomization`.
6. Create `flux/syncroot/<env>` kustomizations that patch `spec.path` and `ref.tag` for each environment.
7. Implement CI workflows to build and push app-config and syncroot images.
8. Validate Flux reconciliation and health endpoints after each publish.
9. Define rollback by re-pinning the syncroot to a prior app-config tag.

## Validation and rollback
- Run `kustomize build` for each env overlay in CI.
- Confirm Flux reconciles new tags and reports healthy status.
- Verify health endpoints: `/health/startup`, `/health/readiness`, `/health/liveness`.
- Roll back by re-publishing the syncroot with a previous app-config tag.

## TODOs
- Confirm External Secrets store name, Key Vault URL, and service account name per environment.
- Confirm whether `AZURE_CLIENT_ID` is injected from the ServiceAccount annotation or needs to be set explicitly.
- Confirm the ASO RoleAssignment schema (principal reference vs principalId) in DIS.

## Platform gaps to plan for
- KEDA is not referenced in the platform repo; confirm if DIS provides it or add it (else use HPA).
- No RoleAssignment CR examples in platform Flux; we need to define our own `authorization.azure.com` RoleAssignments.
- No internal Traefik entrypoint exists; internal-only access must be enforced via allowlists on `http`/`https`.
- GHCR OCIRepositorys are not used in platform; confirm Flux auth for GHCR or use ACR if required.
