# Health Checks

Dialogporten exposes ASP.NET Core health checks through the shared `Digdir.Library.Utils.AspNet` health-check setup. The implementation separates local process liveness, Kubernetes readiness, infrastructure dependencies, and deeper external dependency checks.

Health check responses use the HealthChecks UI JSON response writer. By default, ASP.NET Core returns HTTP 200 for `Healthy` and `Degraded`, and HTTP 503 for `Unhealthy`.

## Endpoints

| Endpoint | Included tags | Purpose |
| --- | --- | --- |
| `/health/liveness` | `self` | Process liveness only. This should not include external dependencies. |
| `/health/readiness` | `critical` | Kubernetes readiness. A failing check here should mean the pod should stop receiving traffic. |
| `/health/startup` | `dependencies` | Dependency startup visibility. |
| `/health` | `dependencies` | Standard dependency health endpoint. |
| `/health/deep` | `dependencies`, `external` | Dependency health plus configured outbound HTTP endpoint checks. |

## Registered Checks

### Self

The `self` check always returns `Healthy`. It is used only by `/health/liveness`.

### PostgreSQL

PostgreSQL is registered through `AddDbContextCheck<DialogDbContext>("postgres", tags: ["dependencies", "critical"])`.

It is the only `critical` infrastructure dependency today, so it is included in `/health/readiness`. PostgreSQL failures make readiness `Unhealthy`, which is appropriate because the application cannot serve normal requests or preserve outbox messages without PostgreSQL.

### Redis

Redis is implemented by `RedisHealthCheck` and registered as `redis` with the `dependencies` tag.

The check creates a Redis connection and runs `PING`.

| Condition | Result |
| --- | --- |
| Ping succeeds within 5 seconds | `Healthy` |
| Ping succeeds but takes more than 5 seconds | `Degraded` |
| Redis timeout | `Degraded` |
| Redis connection failure | `Degraded` |
| Unexpected exception | `Degraded` |

Redis is not tagged `critical`, so Redis problems do not affect `/health/readiness`.

### Azure Service Bus

Azure Service Bus is implemented by `ServiceBusHealthCheck` and registered as `servicebus` with the `dependencies` tag. It is only registered in applications that enable MassTransit publish or publish/subscribe capabilities.

The check does not use the Azure SDK directly. It wraps MassTransit's built-in health check through ASP.NET Core's generic `HealthCheckService`.

There are two related checks in the container:

| Check | Public endpoint visibility | Purpose |
| --- | --- | --- |
| `masstransit-servicebus` | Not selected by public health endpoints | Internal MassTransit bus-state check. |
| `servicebus` | Included by `dependencies` endpoints | Dialogporten-level Service Bus health result. |

The MassTransit check is deliberately named and tagged internally so `/health` does not show both the raw MassTransit result and the app-level `servicebus` result.

| Condition | Result |
| --- | --- |
| Inner MassTransit check is `Healthy` | `Healthy` |
| Inner MassTransit check is `Degraded` | `Degraded` |
| Inner MassTransit check is `Unhealthy` | `Degraded` |
| Inner MassTransit check is missing | `Unhealthy` |

Azure Service Bus outages are reported as `Degraded`, not `Unhealthy`. The system can continue accepting requests because the PostgreSQL outbox preserves outbound messages until broker connectivity recovers. Restarting pods is not expected to fix broker connectivity problems.

The missing-inner-check case is different: it indicates local application misconfiguration, so it is `Unhealthy`.

### External HTTP Endpoints

External HTTP endpoint checks are implemented by `EndpointsHealthCheck` and registered as `Endpoints` with the `external` tag. They are included only in `/health/deep`.

Configured endpoints use this shape:

```json
{
  "Name": "Some external API",
  "AltinnPlatformRelativePath": "somecomponent/api/v1/health",
  "HardDependency": true
}
```

Each entry must set exactly one of:

| Property | Meaning |
| --- | --- |
| `Url` | Absolute URL to check. |
| `AltinnPlatformRelativePath` | Relative path resolved against `InfrastructureSettings.Altinn.BaseUri`. |

`HardDependency` controls whether an endpoint failure makes the aggregate `Endpoints` check `Unhealthy` or only `Degraded`.

WebApi and GraphQL also append configured JWT bearer well-known metadata endpoints to this list. These appended endpoints are soft dependencies: `HardDependency = false`.

## External Endpoint Status Rules

Each configured endpoint is checked with HTTP `GET`.

| Condition | Per-endpoint status | Aggregate effect |
| --- | --- | --- |
| HTTP 2xx within 5 seconds | `Healthy` | No degradation |
| HTTP 2xx after more than 5 seconds | `Slow` | `Degraded`, regardless of `HardDependency` |
| Non-2xx response | `Failed` | `Unhealthy` if `HardDependency = true`, otherwise `Degraded` |
| Exception | `Failed` | `Unhealthy` if `HardDependency = true`, otherwise `Degraded` |
| Timeout after 20 seconds | `Failed` | `Unhealthy` if `HardDependency = true`, otherwise `Degraded` |

Slow responses never make the aggregate check `Unhealthy`. `HardDependency` only affects actual failures.

The `Endpoints` check includes diagnostic data:

| Data field | Meaning |
| --- | --- |
| `checkedEndpoints` | List of endpoint name, URL, hard-dependency flag, status, and duration. |
| `totalCount` | Total endpoints checked. |
| `hardFailureCount` | Failed endpoints where `HardDependency = true`. |
| `softFailureCount` | Failed endpoints where `HardDependency = false`. |
| `slowCount` | Endpoints that succeeded but exceeded the 5 second degradation threshold. |

## Configuration

WebApi and GraphQL bind external endpoint checks from their app-specific settings sections. Service binds them from the top-level `HealthCheckSettings` section.

Example WebApi configuration:

```json
{
  "WebApi": {
    "HealthCheckSettings": {
      "HttpGetEndpointsToCheck": [
        {
          "Name": "Altinn CDN",
          "Url": "https://altinncdn.no/orgs/altinn-orgs.json",
          "HardDependency": false
        },
        {
          "Name": "Altinn Access Management API",
          "AltinnPlatformRelativePath": "accessmanagement/api/v1/meta/info/roles",
          "HardDependency": true
        }
      ]
    }
  }
}
```

Use `HardDependency = true` only when a failing endpoint means the system should be considered unhealthy. Use `HardDependency = false` when the dependency affects functionality or observability but should not cause Kubernetes to restart pods or remove the application from traffic.
