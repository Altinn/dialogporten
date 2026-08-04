# Health Checks — Implementation Guide

This document explains *how* Dialogporten's health checks are implemented and *why* each
design decision was made. It is intended both as a reference for this codebase and as a
template for implementing the same pattern in other services.

For the endpoint/status reference tables (per-check status rules, configuration shape), see
[`HealthCheck.md`](./HealthCheck.md). This document focuses on architecture and rationale.

## Big picture

There are **two layers**, deliberately split:

1. **A reusable library** — `Digdir.Library.Utils.AspNet` — owns the *endpoint shape*, the
   *tag → endpoint routing*, the always-healthy `self` check, and the generic external-HTTP
   endpoint check. This is the portable part.
2. **Per-app infrastructure checks** — `Digdir.Domain.Dialogporten.Infrastructure/HealthChecks`
   — registers the concrete dependency checks (PostgreSQL, Redis, Azure Service Bus, warmup)
   that are specific to this application.

Each of the three hosted services (WebApi, GraphQL, Service) calls the same two extension
methods: `AddAspNetHealthChecks(...)` during registration and `MapAspNetHealthChecks()` during
routing. The infrastructure checks are wired in via `AddCustomHealthChecks()` inside
`InfrastructureExtensions`.

## The core pattern: tags, not endpoints

This is the key idea. Every check is registered **once** with one or more **tags**. Endpoints
are then defined as **predicates over tags**, so a single check can appear in multiple
endpoints, and a new endpoint can be added without touching any check.

Registration (`src/Digdir.Library.Utils.AspNet/AspNetUtilitiesExtensions.cs`):

```csharp
services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["self"])
    .AddCheck<EndpointsHealthCheck>("Endpoints", failureStatus: HealthStatus.Unhealthy, tags: ["external"]);
```

Routing — each endpoint is just a tag filter, all sharing the HealthChecks-UI JSON response
writer:

| Endpoint            | Predicate (tags)         | Checks it runs                  | Consumed by                          |
| ------------------- | ------------------------ | ------------------------------- | ------------------------------------ |
| `/health/liveness`  | `self`                   | always-healthy stub             | Container Apps **Liveness** probe    |
| `/health/readiness` | `critical` OR `warmup`   | postgres + warmup gate          | Container Apps **Readiness** probe   |
| `/health/startup`   | `dependencies`           | postgres + redis + servicebus   | Container Apps **Startup** probe     |
| `/health`           | `dependencies`           | postgres + redis + servicebus   | humans / dashboards                  |
| `/health/deep`      | `dependencies` OR `external` | the above + outbound HTTP checks | APIM availability test               |

> Note: an earlier version of [`HealthCheck.md`](./HealthCheck.md) lists `/health/readiness`
> as `critical`-only. The implementation actually routes `critical` **OR** `warmup`, so the
> warmup readiness-gate (see below) is part of readiness.

ASP.NET Core's default status → HTTP mapping does the rest: `Healthy`/`Degraded` → **200**,
`Unhealthy` → **503**. So a check returning `Degraded` keeps the probe green; only `Unhealthy`
trips it.

## Endpoint → Kubernetes probe mapping

The Azure Container Apps probe wiring lives in `.azure/modules/containerApp/main.bicep`:

| Probe type | Path                | Selects                         |
| ---------- | ------------------- | ------------------------------- |
| Startup    | `/health/startup`   | dependency visibility           |
| Readiness  | `/health/readiness` | only what should pull traffic   |
| Liveness   | `/health/liveness`  | process liveness only           |

The APIM availability test points at `/health/deep` (`.azure/infrastructure/main.bicep`).

This creates a natural ordering: the Startup probe (`dependencies`) effectively waits on
PostgreSQL (Redis/Service Bus only ever degrade — see below); once startup passes, Readiness
adds the warmup gate before the pod receives traffic, while Liveness stays green throughout so
the pod is not killed during a slow dependency outage.

## Registered checks and severity rationale

The severity philosophy is the most transferable decision: **choose the status by asking
"what should this failure actually do?"**

### `self` (tag: `self`)

Always returns `Healthy`. Liveness must answer only "is the process wedged?" — never include
dependencies, or pods get restarted for downstream outages they cannot fix.

### PostgreSQL (tags: `dependencies`, `critical`)

Registered via `AddDbContextCheck<DialogDbContext>("postgres", ...)`. It is the **only**
`critical` dependency, so the only infra check that can fail readiness. Rationale: without
PostgreSQL the app can neither serve requests nor preserve outbox messages, so it *should* be
pulled from traffic.

### Redis (tag: `dependencies`)

`RedisHealthCheck` connects and runs `PING`. **Every failure path returns `Degraded`, never
`Unhealthy`** (timeout, connection failure, unexpected exception — all `Degraded`; a response
slower than 5s is also `Degraded`). Redis is not `critical`, so Redis problems never pull a pod
from traffic — the app degrades to cache-miss behaviour instead.

### Azure Service Bus (tag: `dependencies`)

`ServiceBusHealthCheck` does **not** call the Azure SDK. It is a thin application-level wrapper
over **MassTransit's own** health check, registered only in apps that enable MassTransit
publish/subscribe:

- MassTransit's check is renamed/retagged internally to `masstransit-servicebus` /
  `masstransit-servicebus-internal` so it is **never** selected by any public endpoint's tag
  predicate.
- The wrapper (`servicebus`, tag `dependencies`) calls `HealthCheckService.CheckHealthAsync`
  with a predicate matching only that inner check, then re-maps the result:

| Inner MassTransit result    | Wrapper returns | Why                                                                 |
| --------------------------- | --------------- | ------------------------------------------------------------------- |
| Healthy                     | Healthy         |                                                                     |
| Degraded **or** Unhealthy   | **Degraded**    | the PostgreSQL outbox buffers outbound messages until the broker recovers; restarting pods won't fix broker connectivity |
| Missing (not registered)    | **Unhealthy**   | that is local misconfiguration — a different class of problem       |

This "wrap a library's own check so you control how it is exposed" pattern avoids
double-reporting (raw + app-level) and lets you reinterpret severity.

### Warmup (tag: `warmup`)

Gates readiness during cold start — see the next section.

### External HTTP endpoints (tag: `external`)

`EndpointsHealthCheck`, included only in `/health/deep`. It fans out parallel `GET`s over a
configured list, each with a per-endpoint timeout (20s) and a slow threshold (5s → `Degraded`).
Each entry carries a `HardDependency` flag:

- 2xx fast → Healthy; 2xx slow → `Degraded` (regardless of hard/soft)
- non-2xx / exception / timeout → `Unhealthy` if `HardDependency`, else `Degraded`

It emits rich diagnostic `data` (`checkedEndpoints`, `totalCount`, `hardFailureCount`,
`softFailureCount`, `slowCount`) in the JSON response. WebApi and GraphQL also auto-append their
JWT bearer `WellKnown` metadata URLs as **soft** dependencies.

## The warmup subsystem

This solves cold-start latency: a fresh pod should not take production traffic until its
connection pool and EF model are primed. Three collaborating pieces:

1. **`WarmupState`** — a thread-safe singleton holding `Pending | Healthy | Failed` plus the
   current/failed phase.
2. **`WarmupService : IHostedService`** — on startup runs phases on a background task:
   `db-pool` (opens N pooled connections in parallel, each running `SELECT 1`), `ef-model`
   (forces EF model compilation via a trivial query), and optionally `end-user-search` (a real
   query under a synthetic principal). It marks state Healthy/Failed, with a configurable
   timeout. If warmup is disabled it immediately marks Healthy.
3. **`WarmupHealthCheck`** (tag `warmup`) — `Pending` → `Unhealthy`, `Failed` → `Unhealthy`,
   `Healthy` → `Healthy`.

Because readiness routes `critical` **OR** `warmup`, a booting pod reports **503 on
`/health/readiness`** until warmup finishes — so the platform withholds traffic until the pod
is actually warm — while `/health/liveness` stays 200 the whole time so it is not killed.

## Cross-cutting details

- **Response format**: all endpoints use `UIResponseWriter.WriteHealthCheckUIResponse` (from
  `AspNetCore.HealthChecks.UI.Client`) for structured JSON instead of the bare status string.
- **Telemetry noise suppression**: `HealthCheckFilter` is an OpenTelemetry
  `BaseProcessor<Activity>` that drops spans for `/health` and `/health/deep` routes so probe
  traffic does not flood traces.
- **Per-service configuration**: WebApi/GraphQL bind external endpoints from their own settings
  section (`WebApi:HealthCheckSettings`, etc.) and append well-known auth URLs; the Service
  binds from a top-level `HealthCheckSettings`. `ResolveHttpGetEndpointsToCheck` normalizes each
  entry — either an absolute `Url` or an `AltinnPlatformRelativePath` resolved against the
  Altinn base URI.

## Reusable patterns to carry into another product

The transferable skeleton, independent of Dialogporten's specific dependencies:

1. **Register checks once with tags; define endpoints as tag predicates.** Decouples "what you
   probe" from "what you expose."
2. **Map the three probe types to three tag sets**: liveness = a `self` stub (no dependencies,
   ever); readiness = only what should pull you from traffic; startup = dependency visibility.
3. **Decide severity by consequence**: use `Unhealthy` only when restarting/depooling the pod
   *helps*. If the app has a fallback (outbox, cache-miss), the dependency is `Degraded` and
   **not** `critical`. This single rule is most of the design.
4. **Wrap third-party health checks** when you need to rename, retag, or reinterpret their
   severity, and hide the raw one from public endpoints via tags.
5. **Gate readiness on a warmup state object** if cold-start latency matters: an `IHostedService`
   does the warming, a singleton holds the state, a health check tagged into readiness exposes
   it.
6. Add a **deep** endpoint for outbound dependency visibility that dashboards can hit but
   liveness/readiness never do, and **filter probe spans** out of telemetry.

## Key source files

| Concern                                   | File                                                                       |
| ----------------------------------------- | -------------------------------------------------------------------------- |
| Endpoint/tag routing, `self`, resolver    | `src/Digdir.Library.Utils.AspNet/AspNetUtilitiesExtensions.cs`             |
| External HTTP endpoint check              | `src/Digdir.Library.Utils.AspNet/HealthChecks/EndpointsHealthCheck.cs`     |
| Settings shape                            | `src/Digdir.Library.Utils.AspNet/AspNetUtilitiesSettings.cs`              |
| Telemetry span filter                     | `src/Digdir.Library.Utils.AspNet/HealthCheckFilter.cs`                     |
| Infra check registration                  | `src/Digdir.Domain.Dialogporten.Infrastructure/InfrastructureExtensions.cs` |
| Redis / Service Bus / Warmup checks       | `src/Digdir.Domain.Dialogporten.Infrastructure/HealthChecks/`             |
| Container Apps probes                      | `.azure/modules/containerApp/main.bicep`                                   |
