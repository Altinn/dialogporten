# DIS deployment requirements (Dialogporten)

This document captures what we need to provide to deploy Dialogporten on DIS using Flux + OCI images, based on the DIS documentation provided. It complements `container-runtime.md` (which covers the current Azure Container Apps footprint and AKS mapping).

## What DIS provides
- Platform-managed Kubernetes cluster with Flux and common components (ingress controller, service mesh, shared operators).
- A container registry namespace for the team to push OCI images.
- Reconciliation of a team "syncroot" OCI image per environment.
- Platform OpenTelemetry Collector (see `altinn-platform/flux/otel-collector`) with OTLP receivers and Azure Monitor exports.
- AKS node pool configuration is owned by the platform; current module provisions `syspool` and `workpool` with a single `vm_size` per pool.
  - If Dialogporten needs more than one worker machine size, the platform must support multiple worker pools.

## What we must provide
- A signed, immutable syncroot OCI image that contains our Kubernetes configuration.
- A folder at the root of the image for each environment, each containing `kustomization.yaml`.
- Kustomize overlays for application workloads.
- Helm releases for non-app resources (only if not already provided by DIS).
- OTLP exporter configuration in workloads so metrics, logs, and traces flow to the platform collector.

## Required OCI image layout
DIS reconciles the `kustomization.yaml` in the folder matching the environment name. The root folders must match the DIS environment names.

Minimum layout (environments we use):
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

We can place base and overlay directories anywhere else in the image. Each environment folder should reference the correct overlay for that environment.
Add `at22`/`at24` only if DIS requires them later.

### Suggested internal structure
```
/
├── base
│   ├── apps
│   │   ├── web-api-so
│   │   ├── web-api-eu
│   │   ├── graphql
│   │   └── service
│   ├── jobs
│   │   ├── web-api-migration-job
│   │   ├── sync-resource-policy-information-job
│   │   ├── sync-subject-resource-mappings-job
│   │   ├── reindex-dialogsearch-job
│   │   └── aggregate-cost-metrics-job
│   └── kustomization.yaml
├── overlays
│   ├── prod
│   ├── tt02
│   ├── at23
│   └── yt01
├── prod
│   └── kustomization.yaml
├── tt02
│   └── kustomization.yaml
└── at23
    └── kustomization.yaml
```

## Environment mapping
Our current environments are `test`, `staging`, `yt01`, `prod` (from `.azure/*/*.bicepparam`).

Confirmed mapping:
- `test` -> `at23`
- `staging` -> `tt02`
- `yt01` -> `yt01`
- `prod` -> `prod`

If DIS requires `at22` or `at24`, add overlays that reuse the `at23` or `tt02` values, or keep them empty depending on platform expectations.

## Traefik ingress requirements
DIS uses Traefik. Ingresses must be defined as Traefik `IngressRoute` resources.

Key points for our apps:
- `web-api-so`, `web-api-eu`, and `graphql` require IP allowlists (from `.azure/applications/*/*.bicepparam`).
- The allowlists should be enforced via Traefik middleware (for example, `ipAllowList` middleware).
- `service` should be exposed via Traefik `IngressRoute`, but only on an internal entrypoint or allowlist (not public internet).
Platform Traefik entrypoints are `http` and `https` (custom ports) per `altinn-platform/flux/traefik/helmrelease.yaml`.

Example structure (shape only; values per environment):
```
apiVersion: traefik.io/v1alpha1
kind: IngressRoute
metadata:
  name: dialogporten-web-api-so
spec:
  entryPoints:
    - http
    - https
  routes:
    - kind: Rule
      match: Host(`<env-host>`) && PathPrefix(`/dialogporten`)
      middlewares:
        - name: dialogporten-web-api-so-ip-allowlist
      services:
        - name: dialogporten-web-api-so
          port: 80
```

### Traefik middleware skeletons
IP allowlist middleware:
```yaml
apiVersion: traefik.io/v1alpha1
kind: Middleware
metadata:
  name: dialogporten-ip-allowlist
spec:
  ipAllowList:
    sourceRange:
      - 51.120.88.54/32
      - 51.13.86.131/32
```

Internal-only exposure example (no separate internal entrypoint is defined in platform Traefik):
```yaml
apiVersion: traefik.io/v1alpha1
kind: IngressRoute
metadata:
  name: dialogporten-service-internal
spec:
  entryPoints:
    - https
  routes:
    - kind: Rule
      match: Host(`internal.example`) && PathPrefix(`/service`)
      middlewares:
        - name: dialogporten-ip-allowlist
      services:
        - name: dialogporten-service
          port: 80
```

## Kustomize (apps)
Use Kustomize for application workloads:
- Deployments, Services, IngressRoutes, HorizontalPodAutoscalers, Jobs/CronJobs.
- ApplicationIdentity resources (DIS operator) + ServiceAccounts per app/job.
- ConfigMaps or Secrets references (actual secrets via External Secrets).
- Namespace manifest with `linkerd.io/inject: enabled`.

### Jobs vs scheduled jobs
We must support both patterns in DIS:
- Scheduled jobs -> Kubernetes `CronJob` (sync-resource-policy-information, sync-subject-resource-mappings, aggregate-cost-metrics).
- Manual jobs -> Kubernetes `Job` with no schedule (web-api-migration, reindex-dialogsearch).
Manual jobs should be triggered via CI:
- `web-api-migration` is run in the CI pipeline.
- `reindex-dialogsearch` is triggered via workflow dispatch.

Per-environment overrides (from `container-runtime.md`):
- IP allowlists per app.
- HPA min/max and CPU/memory utilization targets.
- Resource requests/limits.
- Workload profile mapping (dedicated node pool vs default).
- OTEL trace sampler ratio.
- Job schedules and timeouts.

## OpenTelemetry metrics (DIS)
DIS uses a shared OpenTelemetry Collector defined in `altinn-platform/flux/otel-collector`.
- Receivers: OTLP gRPC (`4317`) and OTLP HTTP (`4318`).
- Exporters:
  - Traces/logs -> Azure Application Insights (collector uses `APPLICATIONINSIGHTS_CONNECTION_STRING` from Key Vault).
  - Metrics -> Azure Monitor Workspace via Prometheus remote write (`AMW_WRITE_ENDPOINT` + `azureauth` workload identity).
- Workload requirements (ACA injects these; in DIS we must set them explicitly):
  - Set `OTEL_EXPORTER_OTLP_ENDPOINT` to the collector service in the `monitoring` namespace.
  - Set `OTEL_EXPORTER_OTLP_PROTOCOL=grpc`.
  - Provide `OTEL_SERVICE_NAME` and `OTEL_RESOURCE_ATTRIBUTES` as needed.
  - Do not set `APPLICATIONINSIGHTS_CONNECTION_STRING` on app/job pods in DIS; it causes metrics to bypass OTLP and go directly to App Insights.

Example env var injection (Deployment/CronJob):
```yaml
env:
  - name: OTEL_EXPORTER_OTLP_ENDPOINT
    value: http://<otel-collector-service>.monitoring.svc:4317
  - name: OTEL_EXPORTER_OTLP_PROTOCOL
    value: grpc
  - name: OTEL_SERVICE_NAME
    value: dialogporten-web-api
  - name: OTEL_RESOURCE_ATTRIBUTES
    value: service.namespace=dialogporten,service.version=<image-tag>
```
Note: replace `<otel-collector-service>` with the actual Service name created by the platform (OTEL CR name is `otel`).

## ApplicationIdentity (DIS operator)
The DIS identity operator creates:
- A user-assigned managed identity named `<namespace>-<applicationidentity-name>`.
- Federated credentials for the ServiceAccount subject `system:serviceaccount:<namespace>:<name>`.
- A ServiceAccount annotated with `serviceaccount.azure.com/azure-identity: <clientId>`.

Use `serviceAccountName: <applicationidentity-name>` in each Deployment/Job.
Platform examples (`flux/otel-collector`, `flux/lakmus`) use `azure.workload.identity/use: "true"` and `azure.workload.identity/client-id` annotations with env var injection. The ApplicationIdentity operator uses `serviceaccount.azure.com/azure-identity`, so confirm whether DIS bridges this automatically or if we must set env vars and labels explicitly.

## Azure RBAC permissions (role assignments)
Role assignments are not handled by the ApplicationIdentity operator. Use Azure Service Operator RoleAssignment resources (`authorization.azure.com`) in the app-config overlays.

Recommended mapping (matches current Bicep behavior):
- `web-api-so`, `web-api-eu`, `graphql`, `service`: App Configuration Data Reader (App Config).
- `service`: Azure Service Bus Data Owner (Service Bus namespace).
- `aggregate-cost-metrics-job`: Monitoring Reader (App Insights) + Storage Blob Data Contributor (storage account).
- Key Vault access: handled by External Secrets; apps/jobs generally do not need Key Vault roles.
  - If any workload must read Key Vault directly, add Key Vault Secrets User for that identity.

TODO: confirm the exact ASO RoleAssignment schema (principal reference vs principalId) in DIS.

## Helm (non-app resources)
Use Helm for resources not already provided by DIS (if needed):
- Namespace-scoped support components (for example, Key Vault CSI driver per namespace if not platform-managed).
- Any additional operators not covered by DIS defaults.

## External Secrets skeleton (Azure Key Vault)
DIS uses `SecretStore` with `authType: WorkloadIdentity` and `serviceAccountRef` (see `altinn-platform/flux/otel-collector/external-secrets.yaml`).

```yaml
apiVersion: external-secrets.io/v1
kind: SecretStore
metadata:
  name: dialogporten-azure-kv-store
  namespace: <namespace>
spec:
  provider:
    azurekv:
      authType: WorkloadIdentity
      vaultUrl: <key-vault-url>
      serviceAccountRef:
        name: <service-account>
        namespace: <namespace>
---
apiVersion: external-secrets.io/v1beta1
kind: ExternalSecret
metadata:
  name: dialogporten-secrets
spec:
  refreshInterval: 1h
  secretStoreRef:
    kind: SecretStore
    name: dialogporten-azure-kv-store
  target:
    name: dialogporten-secrets
    creationPolicy: Owner
  data:
    - secretKey: dialogportenAdoConnectionString
      remoteRef:
        key: dialogportenAdoConnectionString
    - secretKey: dialogportenRedisConnectionString
      remoteRef:
        key: dialogportenRedisConnectionString
```

## HPA skeletons (CPU/memory)
ACA defines CPU/memory utilization rules per app. Mirror those rules with HPA and set min/max replicas per environment (from `.azure/applications/*/*.bicepparam`).

Template:
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

Per-app values (match current Bicep):
- `web-api-so`: max 10, cpu 70, memory 70.
- `web-api-eu`: max 20, cpu 50, memory 70.
- `graphql`: max 10, cpu 70, memory 70.
- `service`: max 10, cpu 70, memory 70.

Note: set `minReplicaCount` from env overlays (prod uses 2 for web-api-so, web-api-eu, graphql, service; other envs default to 1 unless overridden).

## OCI image build and publishing
- Build OCI images using the Flux CLI and push to GHCR (`ghcr.io/altinn/dialogporten-config` and `ghcr.io/altinn/dialogporten-syncroot`).
- Ensure images are immutable and signed as per DIS requirements.
- Each environment folder must contain a `kustomization.yaml` entry point.
Tagging: use the same version tags as application images (`<semver>` for releases, `<semver>-<sha>` for main/test).

## Platform gaps to plan for
- No RoleAssignment CR examples in platform Flux; we need to define our own `authorization.azure.com` RoleAssignments.
- No internal Traefik entrypoint exists; internal-only access must be enforced via allowlists on `http`/`https`.
- GHCR OCIRepositorys are not used in platform; confirm Flux auth for GHCR or use ACR if required.

## Open questions
- None.
