# Health Checks — Implementation Guide

This document explains *how* Dialogporten's health checks are implemented and *why* each
design decision was made.

For the endpoint/status reference tables (per-check status rules, configuration shape), see
[`HealthCheck.md`](./HealthCheck.md). This document focuses on architecture and rationale.

## Big picture

The health-check convention lives in the **[`Altinn.AspNet.HealthChecks`][pkg] NuGet packages**,
which were extracted from this codebase. Dialogporten consumes them rather than hand-rolling the
pattern:

| Package | Owns |
| --- | --- |
| `Altinn.AspNet.HealthChecks` | default endpoint paths, tag → endpoint routing, the `self` check, response detail levels and the negotiated JSON/plain-text writers |
| `Altinn.AspNet.HealthChecks.Probes` | config-driven outbound HTTP probes: binding, base-URI resolution, hard/soft severity, duplicate-name detection |
| `Altinn.AspNet.HealthChecks.Warmup` | warmup state, the hosted service that runs phases and enforces the timeouts, the `warmup` readiness check |
| `Altinn.AspNet.HealthChecks.OpenTelemetry` | the span filter that keeps probe traffic out of traces |

[pkg]: https://www.nuget.org/packages/Altinn.AspNet.HealthChecks

What stays in Dialogporten is **which concrete checks to register, with what severity, and
which warmup phases to run**. That split is the point: the library has no opinion about
PostgreSQL or Redis, and Dialogporten has no opinion about endpoint layout.

Two registration sites, deliberately independent:

- `InfrastructureExtensions.AddCustomHealthChecks` registers `postgres`, `redis` and the warmup
  phases. It runs for every host that calls `AddInfrastructure`, including the Janitor.
- `DialogportenHealthCheckExtensions.AddDialogportenHealthChecks` registers the config-driven
  outbound probes, plus the JWT metadata URLs that live in another settings section. Each of
  WebApi, GraphQL and Service calls it once.

Both begin with `AddAltinnHealthChecks()`, which is idempotent — so neither call site needs to
know about the other, and registration order does not matter. Each host then calls
`app.MapDialogportenHealthChecks()`, which maps the library's endpoints and decides the
exception-detail policy in one place (see *Cross-cutting details*).

## The core pattern: tags, not endpoints

Every check is registered **once** with one or more **tags**. Endpoints are **predicates over
tags**, so one check can appear on several endpoints and a new endpoint needs no change to any
check. The library owns this mapping:

| Endpoint            | Predicate (tags)             | Checks it runs                   | Consumed by                        |
| ------------------- | ---------------------------- | -------------------------------- | ---------------------------------- |
| `/health/liveness`  | `live`                       | always-healthy stub              | Container Apps **Liveness** probe  |
| `/health/readiness` | `critical` OR `warmup`       | postgres + warmup gate           | Container Apps **Readiness** probe |
| `/health/startup`   | `dependencies`               | postgres + redis + servicebus    | Container Apps **Startup** probe   |
| `/health`           | `dependencies`               | postgres + redis + servicebus    | humans / dashboards                |
| `/health/deep`      | `dependencies` OR `external` | the above + outbound HTTP probes | APIM availability test             |

Use the `HealthCheckTags` constants (`Live`, `Dependencies`, `Critical`, `Warmup`, `External`)
rather than string literals.

`/health/liveness` is a pinned path, not the library default: the library moved liveness to
`/alive` in 0.3.0 to match the Aspire service-defaults scaffolding, and
`DialogportenHealthCheckExtensions.LivenessPath` holds it at `/health/liveness` so the Container
Apps probe wiring below keeps working. The same endpoint layout is handed to
`AddHealthCheckActivityFilter`, whose default route suffixes would otherwise stop suppressing
liveness probe spans.

ASP.NET Core's default status → HTTP mapping does the rest: `Healthy`/`Degraded` → **200**,
`Unhealthy` → **503**. A check returning `Degraded` keeps the probe green; only `Unhealthy`
trips it.

> **Health check names must be unique.** `DefaultHealthCheckService` throws on duplicates and is
> resolved per request, so a single collision turns *every* health endpoint — liveness included —
> into a 500. The probes package therefore rejects a duplicate at registration, naming the
> configuration path that introduced it, and `AddDialogportenHealthChecks` refuses to register the
> same configuration section twice so a repeated call stays harmless.

## Endpoint → Kubernetes probe mapping

The Azure Container Apps probe wiring lives in `.azure/modules/containerApp/main.bicep`:

| Probe type | Path                | Selects                       |
| ---------- | ------------------- | ----------------------------- |
| Startup    | `/health/startup`   | dependency visibility         |
| Readiness  | `/health/readiness` | only what should pull traffic |
| Liveness   | `/health/liveness`  | process liveness only         |

The APIM availability test points at `/health/deep` (`.azure/infrastructure/main.bicep`).

This creates a natural ordering: the Startup probe (`dependencies`) effectively waits on
PostgreSQL (Redis/Service Bus only ever degrade — see below); once startup passes, Readiness
adds the warmup gate before the pod receives traffic, while Liveness stays green throughout so
the pod is not killed during a slow dependency outage. The readiness probe's `failureThreshold`
of 45 at a 2s period is what gives warmup its budget.

## Registered checks and severity rationale

The severity philosophy is the most transferable decision: **choose the status by asking
"what should this failure actually do?"**

### `self` (tag: `live`)

Registered by `AddAltinnHealthChecks()`. Always returns `Healthy`. Liveness must answer only "is
the process wedged?" — never include dependencies, or pods get restarted for downstream outages
they cannot fix.

### PostgreSQL (tags: `dependencies`, `critical`)

`AddNpgSql(sp => sp.GetRequiredService<NpgsqlDataSource>(), name: "postgres", …)`. The factory
overload matters: the probe then uses the *same* `NpgsqlDataSource`, and therefore the same
pool and credentials, as the application.

It is the **only** `critical` dependency, so the only infra check that can fail readiness.
Rationale: without PostgreSQL the app can neither serve requests nor preserve outbox messages,
so it *should* be pulled from traffic.

### Redis (tag: `dependencies`)

`AddRedis(…, failureStatus: HealthStatus.Degraded)`. Registering it with an explicit
`failureStatus` is what encodes the policy: **every** Redis failure degrades, never fails.
Redis is not `critical`, so Redis problems never pull a pod from traffic — the app degrades to
cache-miss behaviour instead.

### Azure Service Bus (tag: `dependencies`)

There is no Dialogporten check here at all. MassTransit already ships a bus-state check, so we
only configure how it is exposed:

```csharp
configurator.ConfigureHealthCheckOptions(options =>
{
    options.Name = "servicebus";
    options.MinimalFailureStatus = HealthStatus.Degraded;
    options.Tags.Add(HealthCheckTags.Dependencies);
});
```

`MinimalFailureStatus` clamps the worst status the check can report. Service Bus outages are
`Degraded`, not `Unhealthy`: the PostgreSQL outbox preserves outbound messages until broker
connectivity recovers, and restarting pods does not fix broker connectivity.

> Earlier versions wrapped MassTransit's check in an application-level `ServiceBusHealthCheck`
> purely to re-map its severity, which in turn required renaming the inner check so the public
> endpoints would not report both. `MinimalFailureStatus` does the same job with no custom code
> and no hidden second registration.

### Warmup (tag: `warmup`)

Registered by `AddWarmup`. Gates readiness during cold start — see the next section.

### External HTTP endpoints (tag: `external`)

`AddOutboundProbes(section, probes => probes.BaseUri = …)` from the probes package: one
registration **per configured entry**, included only in `/health/deep`, each with a 10s timeout.
`Hard` selects the failure status — `Unhealthy` for hard, `Degraded` for soft — so a soft
dependency can never trip the APIM availability test. WebApi and GraphQL also register their JWT
bearer `WellKnown` metadata URLs as soft probes via `AddOutboundProbe`.

`Hard` and `critical` are different axes, and conflating them is the mistake worth naming: `Hard`
decides how loudly `/health/deep` complains, `critical` decides whether the instance is de-pooled.
Outbound probes are never `critical` — otherwise an upstream outage pulls our own healthy replicas
out of rotation, turning someone else's incident into ours.

Because each endpoint is its own check, `/health/deep` reports them individually by name
instead of as one aggregate entry.

## The warmup subsystem

This solves cold-start latency: a fresh pod should not take production traffic until its
connection pool and EF model are primed. The library owns the machinery; Dialogporten supplies
the phases in `src/Digdir.Domain.Dialogporten.Infrastructure/HealthChecks/WarmupPhases.cs`:

| Phase | Optional | What it primes |
| --- | --- | --- |
| `db-pool` | no | opens N pooled connections in parallel, each running `SELECT 1` |
| `ef-model` | no | forces EF model compilation via a trivial query |
| `service-resource-metadata` | yes | populates the service-resource catalogue cache |
| `end-user-search` | yes | a real search under a synthetic principal |

Phases run **sequentially in registration order, sharing one DI scope**, under a **run budget**
(`Infrastructure:Warmup:TimeoutSeconds`, 80 in the shipped appsettings, validated 1–3600 at host
startup) covering all of them, with a **per-phase budget** layered underneath (20s for the two
required phases, 15s for the optional ones). The per-phase budget is what keeps one slow phase from
spending the whole run: without it a hung optional phase starves whatever comes after it. Both
budgets work by cancelling the token handed to the phase, so they bound only work that observes
cancellation.

The run budget must stay **larger than the sum of the per-phase budgets** (70s today with
`RunEndUserSearch` on, 55s without, hence 80s) — `WarmupSettingsValidator` enforces this at
startup against the same `WarmupPhases` budget constants the registration uses.
`optional: true` only covers a phase *failing*; when the **run** budget fires it is recorded as a
warmup failure regardless of which phase it interrupted, and that failure is terminal — the state
never returns to Healthy, so `/health/readiness` stays 503 for the life of the process and the
replica never joins rotation. Keeping the run budget above the phase budgets means that, for a
phase observing cancellation, only its per-phase budget can ever fire, and an optional phase
that overruns is skipped as intended. A phase that ignores its token forfeits that guarantee:
it can burn through its own budget into the run budget — escalating an optional overrun into
the terminal failure — or, if it never observes cancellation at all, hold readiness at Pending
indefinitely. All four phases above pass their token through to the queries they run.

`optional: true` means a phase failure is logged and warmup continues, so the phase cannot fail
readiness. Optional phases therefore contain **no exception handling of their own** — catching
there would only hide the failure the library is about to log.

Because readiness routes `critical` **OR** `warmup`, a booting pod reports **503 on
`/health/readiness`** until warmup finishes — so the platform withholds traffic until the pod is
actually warm — while `/health/liveness` stays 200 the whole time so it is not killed.

Setting `Infrastructure:Warmup:Enabled = false` marks warmup complete immediately. The Janitor
does this: it runs the same `AddInfrastructure` registrations but maps no health endpoints and
has no cold-start traffic to gate.

## Cross-cutting details

- **Response format**: all endpoints use the library's `HealthReportResponseWriter`, which
  negotiates on `Accept` between `HealthReportJsonFormatter` and `HealthReportTextFormatter`,
  JSON first. JSON is labelled `application/vnd.altinn.health.v1+json` — a versioned vendor type
  so the payload shape can be versioned independently of the package — and a client asking for
  plain `application/json` still lands there, since a `+json` type is a subset of it. The shape is
  `{"status","totalDuration","entries":{name:{"status","duration","description","data","tags"}}}`
  with lowercase statuses and every field but `status` and `duration` omitted when absent or
  withheld; it is the library's own format, not the HealthChecks UI one. `text/plain` gets the
  overall status as a single lowercase word and never any entry detail.
- **Telemetry noise suppression**: `AddHealthCheckActivityFilter()` drops the ASP.NET Core
  server span for all five `/health*` routes. Register it **before** any exporter on the same
  `TracerProviderBuilder` — processors only affect exporters added after them. Matching is by
  case-insensitive route *suffix*, so a business route ending in `/health` would also be
  dropped; pass explicit suffixes if that ever matters. Child spans (for example DB calls made
  by a deep check) are not affected.
- **Response detail**: `MapDialogportenHealthChecks` sets `DetailLevel` to
  `HealthReportDetailLevel.Full` in development and `Summary` outside it. `Summary` covers two
  different leaks. Exception details: the body carries neither the exception message nor the
  description of a check that threw — the health check service uses the exception message as the
  description, so suppressing one field alone would still leak it. Entry data: a check chooses its
  own `data`, and MassTransit's bus-state check publishes the Service Bus host address and its
  queue names there *while healthy*, with no knob to trim it (`ConfigureHealthCheckOptions` offers
  only `Name`, `Tags`, `FailureStatus`, `MinimalFailureStatus`). Withheld fields are omitted from
  the JSON rather than written empty. Worth suppressing because the endpoints are public: WebApi
  and GraphQL through APIM, and the Service directly on its container app ingress, which carries
  no IP allow-list. The level is set explicitly rather than left to the library's default
  derivation, which only recognises the literal environment name `Production` and would resolve
  our `prod`/`staging`/`test`/`yt01` container apps to `Diagnostic` — exactly the level that
  publishes entry data and exception messages.
- **Per-service configuration**: WebApi and GraphQL bind their probe list from their own settings
  section (`WebApi:HealthProbes`, `GraphQl:HealthProbes`) and add their well-known auth URLs; the
  Service binds the top-level `HealthProbes`. That asymmetry is deliberate — Azure App
  Configuration can inject keys that appear in no appsettings file, so an unused-looking section
  is still registered. Each entry sets exactly one of an absolute `Url` or a `RelativePath`
  resolved against `Infrastructure:Altinn:BaseUri`, which differs per environment.
- **Registration-time binding**: the probe list is bound during registration rather than through
  `IOptions<T>`, because each probe needs its own health check registration and there is no
  service provider yet. Validation therefore happens at registration too: a missing name, both or
  neither address, a `RelativePath` that is absolute or leading-slashed, or a duplicate name all
  throw with the offending configuration path in the message. `Infrastructure:Altinn:BaseUri` is
  read the same way, and is only required when some entry actually uses `RelativePath`.

## Reusable patterns

The transferable skeleton, independent of Dialogporten's specific dependencies:

1. **Register checks once with tags; define endpoints as tag predicates.** Decouples "what you
   probe" from "what you expose."
2. **Map the three probe types to three tag sets**: liveness = a `self` stub (no dependencies,
   ever); readiness = only what should pull you from traffic; startup = dependency visibility.
3. **Decide severity by consequence**: use `Unhealthy` only when restarting/depooling the pod
   *helps*. If the app has a fallback (outbox, cache-miss), the dependency is `Degraded` and
   **not** `critical`. This single rule is most of the design.
4. **Configure a third-party check rather than wrapping it.** If a library ships its own check,
   look for knobs to rename, retag and clamp its severity before writing an adapter around it.
5. **Gate readiness on a warmup check** if cold-start latency matters, and express the warming
   as ordered phases where the non-essential ones are explicitly optional.
6. Add a **deep** endpoint for outbound dependency visibility that dashboards can hit but
   liveness/readiness never do, and **filter probe spans** out of telemetry.

## Key source files

| Concern                                | File                                                                        |
| -------------------------------------- | --------------------------------------------------------------------------- |
| Endpoint/tag routing, `self`, JSON     | `Altinn.AspNet.HealthChecks` (NuGet)                                        |
| Warmup machinery                       | `Altinn.AspNet.HealthChecks.Warmup` (NuGet)                                 |
| Telemetry span filter                  | `Altinn.AspNet.HealthChecks.OpenTelemetry` (NuGet)                          |
| Config-driven outbound probes          | `Altinn.AspNet.HealthChecks.Probes` (NuGet)                                 |
| Probe + endpoint wiring for the hosts  | `src/Digdir.Library.Utils.AspNet/DialogportenHealthCheckExtensions.cs`      |
| Infra check + warmup registration      | `src/Digdir.Domain.Dialogporten.Infrastructure/InfrastructureExtensions.cs` |
| Warmup phase bodies                    | `src/Digdir.Domain.Dialogporten.Infrastructure/HealthChecks/WarmupPhases.cs`|
| Container Apps probes                  | `.azure/modules/containerApp/main.bicep`                                    |
