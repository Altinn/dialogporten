# Health Checks

Dialogporten exposes ASP.NET Core health checks through the
[`Altinn.AspNet.HealthChecks`](https://www.nuget.org/packages/Altinn.AspNet.HealthChecks)
packages, which own the endpoint layout, the tag routing and the JSON response format.
The implementation separates local process liveness, readiness, infrastructure dependencies,
and deeper external dependency checks.

By default, ASP.NET Core returns HTTP 200 for `Healthy` and `Degraded`, and HTTP 503 for
`Unhealthy`.

For the architecture and the reasoning behind each severity, see
[`HealthCheckImplementation.md`](./HealthCheckImplementation.md).

## Endpoints

| Endpoint | Included tags | Purpose |
| --- | --- | --- |
| `/health/liveness` | `live` | Process liveness only. This should not include external dependencies. |
| `/health/readiness` | `critical`, `warmup` | Kubernetes readiness. A failing check here should mean the pod should stop receiving traffic. |
| `/health/startup` | `dependencies` | Dependency startup visibility. |
| `/health` | `dependencies` | Standard dependency health endpoint. |
| `/health/deep` | `dependencies`, `external` | Dependency health plus configured outbound HTTP endpoint checks. |

## Registered Checks

### Self

The `self` check always returns `Healthy` and is registered by `AddAltinnHealthChecks()`. It
carries the `live` tag, so it is used only by `/health/liveness`.

### PostgreSQL

PostgreSQL is registered with `AddNpgSql`, using the factory overload so the probe shares the
application's `NpgsqlDataSource` — the same pool and the same credentials.

It is the only `critical` infrastructure dependency today, so it is included in
`/health/readiness`. PostgreSQL failures make readiness `Unhealthy`, which is appropriate
because the application cannot serve normal requests or preserve outbox messages without
PostgreSQL.

### Redis

Redis is registered with `AddRedis` and an explicit `failureStatus` of `Degraded`.

| Condition | Result |
| --- | --- |
| Ping succeeds | `Healthy` |
| Timeout, connection failure or any other error | `Degraded` |

Redis is not tagged `critical`, so Redis problems do not affect `/health/readiness`.

### Azure Service Bus

Service Bus health comes from **MassTransit's own** bus-state check. Dialogporten does not add a
check of its own; it only configures how MassTransit's is exposed — named `servicebus`, tagged
`dependencies`, with `MinimalFailureStatus = Degraded`. It is therefore only present in
applications that enable MassTransit publish or publish/subscribe capabilities.

| Condition | Result |
| --- | --- |
| Bus is healthy | `Healthy` |
| Bus is degraded or unhealthy | `Degraded` |

MassTransit's check reports the broker host address and its queue names as entry `data`. That is
suppressed outside `Development` — see below.

Azure Service Bus outages are reported as `Degraded`, not `Unhealthy`. The system can continue
accepting requests because the PostgreSQL outbox preserves outbound messages until broker
connectivity recovers. Restarting pods is not expected to fix broker connectivity problems.

### External HTTP Endpoints

Outbound endpoint probes come from
[`Altinn.AspNet.HealthChecks.Probes`](https://www.nuget.org/packages/Altinn.AspNet.HealthChecks.Probes).
Each configured entry is registered as its own health check tagged `external`, so they appear
individually by name and are included only in `/health/deep`.

Configured probes use this shape:

```json
{
  "Name": "Some external API",
  "RelativePath": "somecomponent/api/v1/health",
  "Hard": true
}
```

Each entry must set exactly one of:

| Property | Meaning |
| --- | --- |
| `Url` | Absolute URL to check. |
| `RelativePath` | Relative path resolved against `Infrastructure:Altinn:BaseUri`. Absolute values and leading slashes are rejected rather than resolved. |

`Name` must be unique within an application. A duplicate — including one colliding with `self`,
`warmup`, `postgres`, `redis` or `servicebus` — fails startup with a message naming the
configuration path, because duplicate health check names otherwise break every health endpoint.

`Hard` controls the failure status of that probe:

| `Hard` | Failure status | Effect on `/health/deep` |
| --- | --- | --- |
| `true` | `Unhealthy` | 503 |
| `false` (default) | `Degraded` | 200 |

Each probe is checked with HTTP `GET`, expects a 2xx response, and times out after 10 seconds.

WebApi and GraphQL also register their configured JWT bearer well-known metadata endpoints as
probes, using each schema's name. These are always soft: `Hard = false`.

Use `Hard = true` only when a failing endpoint means the system should be considered unhealthy.
Use `Hard = false` when the dependency affects functionality or observability but should not
cause a monitor to page. Note that `Hard` is a different axis from the `critical` tag: `Hard`
only fails `/health/deep`, while `critical` fails readiness and takes the instance out of
rotation. Outbound probes are never `critical` — an upstream outage must not de-pool our own
healthy replicas.

### Response detail outside Development

The library gates the response body behind a `DetailLevel`. We set it explicitly:
`Full` in `Development`, `Summary` everywhere else. Outside `Development` the body carries each
entry's name, status, duration, tags, and its description only when the check did not throw:

- **Exception messages** are omitted, and so is the description of a check that threw — the health
  check service uses the exception message as the description, so dropping only the message would
  still leak it. Npgsql, Redis and the outbound probes routinely name hosts and connection strings
  in theirs.
- **Entry data** is omitted entirely. This is not about failures: MassTransit's bus-state check
  publishes the Service Bus host address and the queue names it knows while perfectly healthy, and
  MassTransit offers no way to trim that at the source.

The health endpoints are publicly reachable — through APIM for WebApi and GraphQL, and directly on
the container app ingress — which is what makes both worth suppressing. In `Development` the body
keeps everything, including exception stack traces.

Setting the level explicitly matters. Left unset, the library derives it from `IHostEnvironment`:
`Development` → `Full`, `Production` → `Summary`, anything else → `Diagnostic` (which publishes
entry data and exception messages). Our container apps run with `ASPNETCORE_ENVIRONMENT` set to
`prod`, `staging`, `test` or `yt01` — none of which is the literal `Production` — so every
deployed environment would land on `Diagnostic`.

## Warmup

A booting pod reports 503 on `/health/readiness` until warmup completes, so it does not receive
traffic before its connection pool and EF model are primed. A failed warmup is retried with
backoff until it succeeds, so a transient failure at boot costs a pod seconds rather than its
whole life. See
[`HealthCheckImplementation.md`](./HealthCheckImplementation.md#the-warmup-subsystem) for the
phases and their configuration.

## Configuration

WebApi and GraphQL bind their probe list from their own settings section — `WebApi:HealthProbes`
and `GraphQl:HealthProbes`. The Service binds the **top-level** `HealthProbes` section. No
Service appsettings file defines it, but Azure App Configuration can supply it at runtime, so the
key is registered regardless.

The section binds directly to an array; there is no wrapper object.

Example WebApi configuration:

```json
{
  "WebApi": {
    "HealthProbes": [
      {
        "Name": "Altinn CDN",
        "Url": "https://altinncdn.no/orgs/altinn-orgs.json",
        "Hard": false
      },
      {
        "Name": "Altinn Access Management API",
        "RelativePath": "accessmanagement/api/v1/meta/info/roles",
        "Hard": true
      }
    ]
  }
}
```
