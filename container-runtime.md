# Container runtime: Azure Container Apps to AKS

This document inventories the current Azure Container Apps configuration in this repo and maps it to AKS with Workload Identity. Per-environment differences are defined in `.azure/infrastructure/*.bicepparam` and `.azure/applications/*/*.bicepparam` and should remain the source of truth.

## Sources reviewed
- `.azure/modules/containerApp/main.bicep`
- `.azure/modules/containerAppJob/main.bicep`
- `.azure/modules/containerAppEnv/main.bicep`
- `.azure/infrastructure/main.bicep`
- `.azure/infrastructure/*.bicepparam`
- `.azure/applications/*/main.bicep`
- `.azure/applications/*/*.bicepparam`
- `README.md` (health endpoints, OTEL)
- `docs/Monitoring.md`

## Current Azure Container Apps footprint

### Container Apps Environment (CAE)
- Name pattern: `dp-be-<environment>-cae` (resource group `dp-be-<environment>-rg`).
- Location: `norwayeast` (all environments).
- VNet integration: `containerAppEnvSubnet` in `dp-be-<environment>-vnet`, address space `10.0.0.0/16`, subnet size `/23`.
- Ingress: environment is not internal (`internal: false`), so apps can be externally reachable.
- Logging/OTEL: Log Analytics workspace from App Insights; OpenTelemetry traces and logs routed to App Insights.
- Identity: user-assigned managed identity `dp-be-<environment>-cae-id` attached to the CAE.
- Workload profiles: `Consumption` always; `Dedicated-D8` enabled in prod/yt01 with min 3 and max 10.
- Zone redundancy: enabled in prod/staging (zones 1-3), disabled in test/yt01.
- Tags: `Environment`, `Product`.

### Shared container app spec (module)
- Ingress: external, target port 8080, optional IP allowlist.
- Health probes:
  - Startup: `/health/startup`, period 10s, delay 10s, timeout 2s.
  - Readiness: `/health/readiness`, period 5s, delay 15s, timeout 2s.
  - Liveness: `/health/liveness`, period 5s, delay 20s, timeout 2s.
- Scaling: min/max replicas plus CPU/memory utilization rules per app.
- Resources: optional `cpu` and `memory`.
- Identity: user-assigned per app; `AZURE_CLIENT_ID` passed to workloads.
- Revision suffix: `REVISION_SUFFIX` cleaned to form ACA revision name.

### Shared container app job spec (module)
- Trigger: scheduled (cron) or manual (parallelism 1, completion count 1).
- Secrets: Key Vault references with managed identity, exposed as env `secretRef`.
- Retry/timeout: `replicaRetryLimit` 1, `replicaTimeout` per job.
- Workload profile: defaults to `Consumption` unless overridden.

### Applications (Container Apps)

#### web-api-so
- Image: `ghcr.io/altinn/dialogporten-webapi:<imageTag>`
- Ingress: external + IP allowlist (per environment).
- Scale: minReplicas param, max 10, CPU 70 percent, memory 70 percent.
- Env vars: `ASPNETCORE_ENVIRONMENT`, `APPLICATIONINSIGHTS_CONNECTION_STRING`, `AZURE_APPCONFIG_URI`, `ASPNETCORE_URLS=http://+:8080`, `AZURE_CLIENT_ID`, `OTEL_TRACES_SAMPLER`, `OTEL_TRACES_SAMPLER_ARG`.
- Identity: `dp-be-<env>-webapi-so-identity` with Key Vault + App Configuration reader roles.

#### web-api-eu
- Image: `ghcr.io/altinn/dialogporten-webapi:<imageTag>`
- Ingress: external + IP allowlist (per environment).
- Scale: minReplicas param, max 20, CPU 50 percent, memory 70 percent.
- Env vars: same as web-api-so.
- Identity: `dp-be-<env>-webapi-eu-identity` with Key Vault + App Configuration reader roles.

#### graphql
- Image: `ghcr.io/altinn/dialogporten-graphql:<imageTag>`
- Ingress: external + IP allowlist (per environment).
- Scale: minReplicas param, max 10, CPU 70 percent, memory 70 percent.
- Env vars: `ASPNETCORE_ENVIRONMENT`, `APPLICATIONINSIGHTS_CONNECTION_STRING`, `AZURE_APPCONFIG_URI`, `AZURE_CLIENT_ID`, `OTEL_TRACES_SAMPLER`, `OTEL_TRACES_SAMPLER_ARG`.
- Identity: `dp-be-<env>-graphql-identity` with Key Vault + App Configuration reader roles.

#### service
- Image: `ghcr.io/altinn/dialogporten-service:<imageTag>`
- Ingress: external (no IP allowlist).
- Scale: minReplicas param, max 10, CPU 70 percent, memory 70 percent.
- Env vars: `ASPNETCORE_ENVIRONMENT`, `APPLICATIONINSIGHTS_CONNECTION_STRING`, `AZURE_APPCONFIG_URI`, `ASPNETCORE_URLS=http://+:8080`, `AZURE_CLIENT_ID`, `Infrastructure__MassTransit__Host=sb://<namespace>.servicebus.windows.net/`, `OTEL_TRACES_SAMPLER`, `OTEL_TRACES_SAMPLER_ARG`.
- Identity: `dp-be-<env>-service-identity` with Key Vault + App Configuration reader roles, plus Service Bus Data Owner.

### Jobs (Container App Jobs)

#### web-api-migration-job (manual)
- Name: `dp-be-<env>-db-migration-job`
- Image: `ghcr.io/altinn/dialogporten-migration-bundle:<imageTag>`
- Env vars: `Infrastructure__DialogDbConnectionString` (Key Vault secret), `AZURE_CLIENT_ID`.
- Identity: `dp-be-<env>-migration-job-identity` with Key Vault reader.

#### sync-resource-policy-information-job (scheduled)
- Name: `dp-be-<env>-sync-rp-info`
- Image: `ghcr.io/altinn/dialogporten-janitor:<imageTag>`
- Args: `sync-resource-policy-information`
- Env vars: DB/Redis connection strings (Key Vault), `DOTNET_ENVIRONMENT`, `APPLICATIONINSIGHTS_CONNECTION_STRING`, `AZURE_CLIENT_ID`.
- Identity: `dp-be-<env>-sync-rp-info-identity` with Key Vault reader.

#### sync-subject-resource-mappings-job (scheduled)
- Name: `dp-be-<env>-sync-sr-mappings`
- Image: `ghcr.io/altinn/dialogporten-janitor:<imageTag>`
- Args: `sync-subject-resource-mappings`
- Env vars: DB/Redis connection strings (Key Vault), `DOTNET_ENVIRONMENT`, `APPLICATIONINSIGHTS_CONNECTION_STRING`, `AZURE_CLIENT_ID`.
- Identity: `dp-be-<env>-sync-sr-mappings-identity` with Key Vault reader.

#### reindex-dialogsearch-job (manual)
- Name: `dp-be-<env>-reindex-search`
- Image: `ghcr.io/altinn/dialogporten-janitor:<imageTag>`
- Args: `reindex-dialogsearch`
- Resources: 4 CPU, 8Gi memory.
- Env vars: DB/Redis connection strings (Key Vault), `DOTNET_ENVIRONMENT`, `APPLICATIONINSIGHTS_CONNECTION_STRING`, `AZURE_CLIENT_ID`.
- Identity: `dp-be-<env>-reindex-search-identity` with Key Vault reader.

#### aggregate-cost-metrics-job (scheduled, prod/staging only)
- Name: `dp-be-<env>-cost-metrics`
- Image: `ghcr.io/altinn/dialogporten-janitor:<imageTag>`
- Args: `aggregate-cost-metrics`
- Env vars: DB/Redis connection strings (Key Vault), `DOTNET_ENVIRONMENT`, `APPLICATIONINSIGHTS_CONNECTION_STRING`, `AZURE_CLIENT_ID`, `MetricsAggregation__StorageAccountName`, `MetricsAggregation__StorageContainerName`, `MetricsAggregation__SubscriptionId`.
- Extra infra: storage account + container per env; RBAC: storage blob data contributor, App Insights monitoring reader.
- Identity: `dp-be-<env>-cost-metrics-identity` with Key Vault reader.

## Per-environment overrides (bicepparam)
All items below are the environment-specific differences defined in `.azure/infrastructure/*.bicepparam` and `.azure/applications/*/*.bicepparam`.

### Container Apps Environment (infra)
- prod: zone redundancy enabled, workload profiles include `Dedicated-D8` (min 3 max 10).
- staging: zone redundancy enabled, only `Consumption` workload profile.
- test: zone redundancy disabled, only `Consumption` workload profile.
- yt01: zone redundancy disabled, `Dedicated-D8` workload profile (min 3 max 10), App Insights purge after 30 days.

### Ingress IP allowlist (used by web-api-so, web-api-eu, graphql)
- prod: `51.120.88.54`
- staging: `51.13.86.131`
- test: `51.13.79.23`, `51.120.88.69`
- yt01: `51.13.85.197`

### web-api-so
- prod: minReplicas 2, resources 2 CPU / 4Gi, workloadProfileName `Dedicated-D8`, otelTraceSamplerRatio 0.2.
- staging: resources 1 CPU / 2Gi, otelTraceSamplerRatio 1.
- test: defaults (minReplicas 1, no resources override), otelTraceSamplerRatio 1.
- yt01: resources 2 CPU / 4Gi, workloadProfileName `Dedicated-D8`, otelTraceSamplerRatio 1.

### web-api-eu
- prod: minReplicas 2, resources 2 CPU / 4Gi, workloadProfileName `Dedicated-D8`, otelTraceSamplerRatio 0.2.
- staging: defaults (minReplicas 1, no resources override), otelTraceSamplerRatio 1.
- test: defaults (minReplicas 1, no resources override), otelTraceSamplerRatio 1.
- yt01: resources 2 CPU / 4Gi, workloadProfileName `Dedicated-D8`, otelTraceSamplerRatio 1.

### graphql
- prod: minReplicas 2, resources 2 CPU / 4Gi, workloadProfileName `Dedicated-D8`, otelTraceSamplerRatio 0.2.
- staging: defaults (minReplicas 1, no resources override), otelTraceSamplerRatio 1.
- test: defaults (minReplicas 1, no resources override), otelTraceSamplerRatio 1.
- yt01: resources 2 CPU / 4Gi, workloadProfileName `Dedicated-D8`, otelTraceSamplerRatio 1.

### service
- prod: minReplicas 2, resources 2 CPU / 4Gi, otelTraceSamplerRatio 0.2.
- staging: defaults (minReplicas 1, no resources override), otelTraceSamplerRatio 1.
- test: defaults (minReplicas 1, no resources override), otelTraceSamplerRatio 1.
- yt01: resources 2 CPU / 4Gi, otelTraceSamplerRatio 1.

### web-api-migration-job
- All envs: replicaTimeOutInSeconds 86400.

### sync-resource-policy-information-job
- prod: cron `10 3 * * *`, timeout 600.
- staging: cron `15 3 * * *`, timeout 600.
- test: cron `20 3 * * *`, timeout 600.
- yt01: cron `25 3 * * *`, timeout 600.

### sync-subject-resource-mappings-job
- All envs: cron `*/5 * * * *`, timeout 600.

### reindex-dialogsearch-job
- prod: timeout 172800.
- staging: timeout 600.
- test: timeout 600.
- yt01: timeout 86400.

### aggregate-cost-metrics-job
- prod: cron `0 2 * * *`, timeout 1800, storageContainerName `costmetrics`.
- staging: cron `0 2 * * *`, timeout 1800, storageContainerName `costmetrics`.
- Not deployed in test/yt01.

## Azure Container Apps vs AKS (differences that matter here)
- Abstraction level: ACA is fully managed (ingress, autoscaling, revisions); AKS requires explicit control plane setup (ingress controller, HPA, rollout strategy).
- Revisions/traffic splitting: ACA has built-in revision management; AKS uses Deployment rollout history and optional progressive delivery.
- Autoscaling: ACA uses CPU/memory utilization rules per app (as defined in this repo); AKS should use HPA (autoscaling/v2) with CPU/memory targets.
- Jobs: ACA has first-class Jobs (manual/scheduled); AKS uses CronJobs and Jobs, with separate RBAC and scheduling controls.
- Networking: ACA ingress is managed and IP allowlists are per app; AKS needs Ingress or LoadBalancer configuration plus NetworkPolicy or ingress annotations to enforce allowlists.
- Identity/secrets: ACA uses managed identities and Key Vault references; AKS should use Workload Identity plus Key Vault CSI or External Secrets.
- Observability: ACA provides managed OpenTelemetry integration; AKS needs an OpenTelemetry Collector (plus Azure Monitor for containers if desired) to handle traces, logs, and metrics.

## Translation to AKS (what we need)

### Cluster-level setup
- AKS cluster in the existing VNet (subnet sizing comparable to ACA `/23` for apps; separate subnets for nodes and ingress as needed).
- System node pool plus a dedicated node pool matching `Dedicated-D8`; use taints and labels for scheduling.
- Platform AKS module (`altinn-platform/infrastructure/modules/aks`) currently provisions two pools (`syspool`, `workpool`) with a single `vm_size` per pool via `pool_configs`.
  - Map `Dedicated-D8` workloads to the `workpool` size if that matches expectations.
  - If we need more than one worker machine size (for example a small pool plus a D8 pool), the AKS module must support multiple worker pools.
- OpenTelemetry Collector configured to send traces/logs to the App Insights instance per environment and metrics to Azure Monitor Workspace (Prometheus remote write).

### Ingress and networking
- Ingress controller: Traefik with a public LoadBalancer IP for external endpoints.
- IP allowlists for web-api-so, web-api-eu, graphql enforced at ingress (NGINX `whitelist-source-range` or App Gateway WAF rules).
- TLS termination and routing equivalent to ACA external ingress (mirror current APIM exposure if required).
- Expose `service` through Traefik `IngressRoute` on an internal-only entrypoint or allowlist; not public internet.

### Identity and secrets
- One Kubernetes ServiceAccount per app/job with Azure Workload Identity.
- Federated identity credentials mapped to existing user-assigned managed identities:
  - web-api-so, web-api-eu, graphql, service
  - migration, sync, reindex, cost-metrics jobs
- Key Vault secrets via External Secrets:
  - `dialogportenAdoConnectionString`
  - `dialogportenRedisConnectionString`
- App Configuration access via `AZURE_APPCONFIG_URI` and Workload Identity (keep `AZURE_CLIENT_ID`).

### OpenTelemetry metrics (AKS/DIS)
The platform OTEL collector in `flux/otel-collector` is the source of truth for how metrics are handled in AKS/DIS.
- Collector receives OTLP (gRPC 4317 / HTTP 4318) and exports:
  - Traces/logs -> Azure Application Insights (via `APPLICATIONINSIGHTS_CONNECTION_STRING` on the collector).
  - Metrics -> Azure Monitor Workspace via Prometheus remote write (`AMW_WRITE_ENDPOINT` + `azureauth` workload identity).
- Workloads must export OTLP to the collector (ACA injects these automatically; AKS/DIS must set them explicitly):
  - `OTEL_EXPORTER_OTLP_ENDPOINT` -> collector service endpoint (monitoring namespace).
  - `OTEL_EXPORTER_OTLP_PROTOCOL=grpc`.
  - `OTEL_SERVICE_NAME` and `OTEL_RESOURCE_ATTRIBUTES` as needed.
- Metrics exporter behavior in code:
  - If `APPLICATIONINSIGHTS_CONNECTION_STRING` is set in the workload, metrics go directly to App Insights and bypass the OTLP metrics pipeline.
  - For AKS/DIS, omit `APPLICATIONINSIGHTS_CONNECTION_STRING` from app/job pods to ensure metrics flow to the collector.

### Workloads
- Deployments:
  - web-api-so, web-api-eu, graphql, service
  - Configure probes with the same paths and timing from ACA.
  - Set resource requests and limits matching the `resources` overrides per environment.
  - HPA per app using CPU/memory targets and min/max replicas matching ACA scale settings.
  - Use nodeSelector or affinity to map `Dedicated-D8` workloads to the dedicated node pool.
- CronJobs:
  - sync-resource-policy-information-job
  - sync-subject-resource-mappings-job
  - aggregate-cost-metrics-job (prod/staging only)
  - Use `spec.timeZone` if required; ACA cron uses UTC by default.
- Jobs:
  - web-api-migration-job (manual)
  - reindex-dialogsearch-job (manual)
  - Provide a manual trigger path (kubectl or CI workflow) to replace ACA manual jobs.

### Per-environment configuration surfaces (keep in config or overlays)
- Ingress IP allowlists.
- minReplicas, maxReplicas, CPU/memory utilization targets (HPA resource metrics).
- Resource requests and limits (CPU/memory).
- Workload profile mapping (dedicated node pool vs consumption).
- OTEL trace sampler ratio.
- Job schedules and timeouts.
- Image tags and revision suffix or labels.

## Delivery approach
- Use Kustomize overlays for app workloads (Deployments, Services, Ingress, HPA, CronJobs/Jobs).
- Use Helm charts for supporting resources (ingress controller, observability stack, CSI drivers, cluster-level add-ons).

## Notes and risks
- The `service` app is currently exposed externally in ACA; in AKS it should be internal-only but still use Traefik `IngressRoute`.
- ACA revision suffix has no direct AKS equivalent; consider a label or annotation to preserve traceability.
- Ensure private endpoint connectivity to Key Vault, App Configuration, Service Bus, Redis, and PostgreSQL from AKS subnets.
