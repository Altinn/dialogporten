# Feature Metrics on DIS Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Feature metrics are the records Dialogporten's cost allocation is built from. They must keep arriving in `dp-be-<env>-applicationInsights` when the web API runs on DIS, whose platform OpenTelemetry collector drops every log record below Warning.

**Architecture:** This plan describes two alternatives. Implement one of them.
- **3B (recommended):** the WebApi sends feature-metric log events through a second Serilog OTLP sink to a small collector in the `product-dialogporten` namespace. That collector exports them to the existing App Insights.
- **3A:** the platform collector gets a second logs pipeline that picks out these records and exports them to the same App Insights through a second `azuremonitor` exporter.

With either one, the `aggregate-cost-metrics` job keeps querying the App Insights it queries today. While Container Apps and DIS both run, records from both land in one place.

**Tech Stack:** .NET 10; Serilog with Serilog.Sinks.OpenTelemetry 4.2.0; OpenTelemetry Collector contrib 0.140.1 (`azuremonitor` exporter); Bicep; Kustomize and Flux; Linkerd policy; External Secrets Operator; Terraform (azapi and azurerm).

**Spec:** [Background and design](#background-and-design) below. The problem statement is in Altinn/altinn-platform#4135.

## Global Constraints

- **Record shape.** Records must keep the shape the cost query parses (`src/Digdir.Domain.Dialogporten.Janitor/CostManagementAggregation/ApplicationInsightsService.cs:47-66`):
  - `customDimensions.EventId` is JSON: `{"Id":1000,"Name":"LogFeatureMetric"}`.
  - `customDimensions.AdditionalTags` is a JSON object with `StatusCode`, `CorrelationId` and `Status`.
- **DIS only.** Only DIS sends through the new path. On Container Apps the records already reach `dp-be-<env>-applicationInsights`, so sending them there too would count every request twice.
- **No new NuGet packages in the WebApi.** `Serilog.Sinks.ApplicationInsights` 5.0.1 requires `Microsoft.ApplicationInsights` `[2.23.0,3.0.0)`, and the solution uses `Microsoft.ApplicationInsights.AspNetCore` 3.1.2.
- **Collector image.** Use `opentelemetry-collector-contrib:0.140.1`, the version the platform collector runs.
- **Environment mapping** from DIS to dp-be:

  | DIS | dp-be |
  |---|---|
  | `at23` | `test` |
  | `tt02` | `staging` |
  | `yt01` | `yt01` |
  | `prod` | `prod` |
- **Deadline.** Must be live before Dialogporten is onboarded to DIS in `tt02` and `prod`. Today only `at23` runs Dialogporten on DIS, and the cost job only runs for `staging` and `prod`.
- **C# style** follows `AGENTS.md`: warnings are errors, extension blocks, file-scoped namespaces, and `Snake_Case` test names.

## Review Focus

1. **The pod's `OTEL_EXPORTER_OTLP_ENDPOINT` hijacking the feature-metrics sink.** Serilog.Sinks.OpenTelemetry applies the `OTEL_EXPORTER_OTLP_*` variables *after* the options callback. The platform collector's address would replace ours, and the records would be dropped again.
   - Pinned by `Feature_metric_sink_reads_resource_variables_but_not_exporter_settings_from_the_environment` (Task B2), which records what the real sink reads.
   - Deleting the accessor argument breaks the build with `IDE0060`.
2. **The delivery context moving out of the `FeatureMetric` namespace.** The sink selects events by that namespace, so a move would silently stop the billing data. Pinned by `Logging_delivery_context_is_in_the_feature_metric_namespace` (Task B2).
3. **Other log categories leaking into the billing path.** This covers `...DialogSearch.EndUser`, the other category the WebApi raises to Information, and look-alike namespaces. Pinned by `Only_feature_metric_events_reach_the_feature_metric_sink` (Task B2).
4. **`APPLICATIONINSIGHTS_CONNECTION_STRING` overriding the second exporter in the platform collector.** The `azuremonitor` exporter reads that variable before its own `connection_string` (`exporter/azuremonitorexporter/connection_string_parser.go`, v0.140.1). The feature metrics would then go to the shared platform App Insights. Pinned by the routing run and the control run in Task A2.
5. **A missing or malformed endpoint.**
   - If the endpoint is missing (Container Apps, local runs), no sink may be added.
   - If it is malformed, startup must fail rather than silently drop billing data.
   - Pinned by `Missing_endpoint_adds_no_sink` and `Invalid_endpoint_fails_at_startup` (Task B2).

---

## Background and design

### Problem

`LoggingFeatureMetricDeliveryContext` writes one Information log record per feature, with `EventId` 1000 (`LogFeatureMetric`). Once a day (`0 2 * * *`), the `aggregate-cost-metrics` Janitor job queries these records in `dp-be-<env>-applicationInsights`, summarizes them and writes Parquet files to the `costmetrics` container.

On DIS the WebApi sends its logs to the platform collector (`otel-collector.monitoring`). That collector drops every record below Warning; see `filter/logs` in `dis-way/gitops-manifests:oci/otel-collector/base/collector.yaml:424-429`. Nothing reaches an App Insights.

| | Path |
|---|---|
| Container Apps (today) | WebApi → Serilog → OTLP → Container Apps' managed OTel agent → `dp-be-<env>-applicationInsights` → cost job |
| DIS (today) | WebApi → Serilog → OTLP → `otel-collector.monitoring` → `filter/logs` drops the record |

### Facts that shape the design

- **Emitters.** Only `web-api-so` and `web-api-eu` emit feature metrics (`FeatureMetricMiddleware`).
- **Log levels.** The WebApi logs through Serilog at minimum level Warning. `src/Digdir.Domain.Dialogporten.WebApi/appsettings.json:19-20` raises `...FeatureMetric.LoggingFeatureMetricDeliveryContext` to Information.
- **Scope name.** Serilog.Sinks.OpenTelemetry sends that category (Serilog's `SourceContext`) as the OTLP instrumentation scope name. Serilog's `EventId` arrives as an OTLP map attribute, not as `event_name`.
- **Ingestion.** `dp-be-<env>-applicationInsights` accepts ingestion by connection string over its public endpoint. `.azure/modules/applicationInsights/create.bicep` leaves `DisableLocalAuth` and public network access at their defaults.
- **Connection string location.** The connection string is not in any Key Vault. The Container Apps environment gets it from `appInsights.outputs.connectionString` (`.azure/infrastructure/main.bicep:331`). The apps get it from the GitHub environment secret `AZURE_APP_INSIGHTS_CONNECTION_STRING`.
- **Platform collector.** It accepts OTLP from every pod in the cluster (`accessPolicy: cluster-unauthenticated`). It sets one App Insights connection string for the whole collector.

### Options

| | 3B: Dialogporten-owned collector | 3A: platform collector | Option 1 (not planned) |
|---|---|---|---|
| Records land in | `dp-be-<env>-applicationInsights` | `dp-be-<env>-applicationInsights` | shared `dis-core-<env>-products-ai` |
| Repos changed | dialogporten, dialogporten-manifests | gitops-manifests, terraform-azurerm-dis-modules, dis-way/core | gitops-manifests, dialogporten (cost job), dis-way/core |
| Cost job | unchanged | unchanged | must query two resources during the migration |
| Who can write billing records | `web-api-so` and `web-api-eu` only (Linkerd) | any pod in the cluster | any pod in the cluster |
| Platform config names Dialogporten | no | yes (class name, secret) | yes (class name) |

**Option 1** adds a filter exception in the platform collector, so the records land in DIS's shared products App Insights. That App Insights has the provider-default cap of 100 GB/day. This plan does not cover it.

**Why 3B is recommended:**
- It changes nothing on the platform.
- Only the two web APIs can write billing records.
- The class name appears only in Dialogporten's own code, where a test guards it.
- It moves towards keeping feature metrics apart from other logs (Altinn/dialogporten#3123).

**When 3A fits:** it needs no Dialogporten code change. Pick it if the Dialogporten team should not run a collector.

**Ruled out:**
- **`Serilog.Sinks.ApplicationInsights`:** its package range conflicts with the solution, as described under Global Constraints.
- **Exporting from the app with `Azure.Monitor.OpenTelemetry.Exporter`:**
  - Its log exporter writes `EventId` as the plain value `"1000"`, so the cost query's `extractjson("$.Id", …)` misses it.
  - It writes attribute values with `ToString()`, so `AdditionalTags` arrives as a type name.
  - Serilog doesn't feed the Microsoft.Extensions.Logging providers the exporter would hang off.

### What was checked for this plan

**3B code** builds with SDK 10.0.401 with 0 warnings, and the four tests in Task B2 pass. Each guarded behaviour fails when removed:

| Change | Result |
|---|---|
| Source filter removed | test fails |
| Environment guard passes everything | test fails |
| Accessor argument deleted | build fails with `IDE0060` |
| Missing-endpoint return removed | WebApi build fails, because swagger generation runs `Program.cs` without the setting |

**3B collector**
- `otelcol validate` passes.
- Against the mock in the appendix, the exported record has `EventId` `{"Id":1000,"Name":"LogFeatureMetric"}`, `AdditionalTags` `{"CorrelationId":"0HN1","Status":"success","StatusCode":200}`, `HasAdminScope` `false` and `cloud_RoleName` `dialogporten-web-api-so`.

**3B manifests**
- `kustomize build` passes for all four environments and syncroots.
- The nine new objects pass kubeconform against their CRD schemas.

**Bicep:** `az bicep build` passes with the same diagnostics as `main`.

**3A**
- Every overlay path renders, and `flux envsubst --strict` and `otelcol validate` pass.
- The routing run sends only the feature metric to Dialogporten's endpoint and only Warning records to the platform endpoint.
- The control run, with the old variable still set, sends the feature metric to the platform endpoint instead.

**3A Terraform**
- The module changes pass `terraform fmt -check` and `terraform validate`.
- The variable validation rejects an empty value.

---

## Plan 3B: Dialogporten-owned collector (recommended)

**Order:**
- B1 and B2 can merge in either order.
- B3 lands in `manifests/common/base`, which reaches every bootstrapped environment at once. Before B3 merges, B1 must be deployed to every environment whose syncroot is bootstrapped; today that is only `test`/`at23`. For each later environment, deploy B1 there before its syncroot is bootstrapped. Otherwise the collector sits in `CreateContainerConfigError` until the secret exists.
- B4 comes last.

**Rollback:** remove `FeatureMetrics__OtlpEndpoint` from the two web API manifests. The pods stop adding the sink on their next rollout. The collector can stay or be removed. Container Apps is not affected.

### Task B1: Publish the App Insights connection string to the environment Key Vault

**Repo:** Altinn/dialogporten

**Files:**
- Modify: `.azure/infrastructure/main.bicep`. The new module goes between `module appInsights` (line 153) and `module apimAvailabilityTest` (line 165).

**Interfaces:**
- Produces: Key Vault secret `dialogportenAppInsightsConnectionString` in `dp-be-<env>-kv-*`, read by Task B3's ExternalSecret.

- [ ] **Step 1: Add the secret**

Insert before `module apimAvailabilityTest` in `.azure/infrastructure/main.bicep`:

```bicep
// Read by the feature-metrics collector on DIS (Altinn/dialogporten-manifests), which sends the
// cost-allocation feature metrics to this Application Insights. See docs/FeatureMetrics.md.
module appInsightsConnectionStringSecret '../modules/keyvault/upsertSecret.bicep' = {
  scope: resourceGroup
  name: 'appInsightsConnectionStringSecret'
  params: {
    destKeyVaultName: environmentKeyVault.outputs.name
    secretName: 'dialogportenAppInsightsConnectionString'
    secretValue: appInsights.outputs.connectionString
    tags: tags
  }
}
```

- [ ] **Step 2: Compile**

Run: `az bicep build --file .azure/infrastructure/main.bicep`
Expected: exit code 0, and no diagnostics beyond the `BCP081` warnings `main` already has.

- [ ] **Step 3: Commit**

```bash
git add .azure/infrastructure/main.bicep
git commit -m "feat(infra): publish the App Insights connection string to the environment Key Vault"
```

- [ ] **Step 4: After the infrastructure deploy to test, check the secret and who can read it**

```bash
az keyvault secret show --vault-name dp-be-test-kv-yju3bdib4d --name dialogportenAppInsightsConnectionString --query value -o tsv | cut -d';' -f1
CLIENT_ID=$(kubectl -n product-dialogporten get sa dialogporten-secrets -o jsonpath='{.metadata.annotations.azure\.workload\.identity/client-id}')
az role assignment list --assignee "$CLIENT_ID" --all --query "[].{role:roleDefinitionName, scope:scope}" -o table
```

Expected:
- The first command prints `InstrumentationKey=<guid>`.
- The role list includes `Key Vault Secrets User` with scope `.../vaults/dp-be-test-kv-yju3bdib4d`. That is the identity the SecretStore `dialogporten-azure-kv` uses.
- If the scope ends in `/secrets/<name>` instead, grant the same role on `dialogportenAppInsightsConnectionString`.

### Task B2: Send feature metrics to their own OTLP endpoint (WebApi)

**Repo:** Altinn/dialogporten

**Files:**
- Create: `src/Digdir.Domain.Dialogporten.WebApi/Common/FeatureMetric/FeatureMetricLoggingExtensions.cs`
- Modify: `src/Digdir.Domain.Dialogporten.WebApi/Program.cs:72-79` (the `UseSerilog` call)
- Modify: `docs/FeatureMetrics.md:68-81` (section "3. Delivery Layer")
- Test: `tests/Digdir.Domain.Dialogporten.WebApi.Unit.Tests/FeatureMetricLoggingExtensionsTests.cs`
- Test: `tests/Digdir.Domain.Dialogporten.Application.Unit.Tests/Common/Behaviours/FeatureMetric/FeatureMetricLogCategoryTests.cs`

**Interfaces:**
- Consumes: the public `IFeatureMetricDeliveryContext` (`Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric`).
- Produces:
  - Configuration key `FeatureMetrics:OtlpEndpoint`, set by Task B3 as the environment variable `FeatureMetrics__OtlpEndpoint`.
  - `LoggerConfiguration.WriteFeatureMetricsToOpenTelemetry(IConfiguration)`.
  - Internal, for tests: `LoggerConfiguration.WriteFeatureMetricsToOpenTelemetry(IConfiguration, Func<string, string?>)`, `LoggerConfiguration.WriteFeatureMetricsTo(Action<LoggerSinkConfiguration>)` and `FeatureMetricLoggingExtensions.OtlpEndpointConfigurationKey`.

- [ ] **Step 1: Write the failing tests**

Create `tests/Digdir.Domain.Dialogporten.WebApi.Unit.Tests/FeatureMetricLoggingExtensionsTests.cs`:

```csharp
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.WebApi.Common.FeatureMetric;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Digdir.Domain.Dialogporten.WebApi.Unit.Tests;

public class FeatureMetricLoggingExtensionsTests
{
    private const string FeatureMetricCategory =
        "Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric.LoggingFeatureMetricDeliveryContext";

    private const string DialogSearchCategory =
        "Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser";

    [Fact]
    public void Only_feature_metric_events_reach_the_feature_metric_sink()
    {
        var sink = new CollectingSink();
        using var logger = new LoggerConfiguration()
            // Mirrors Program.cs and appsettings.json: Warning by default, Information for these two categories.
            .MinimumLevel.Warning()
            .MinimumLevel.Override(FeatureMetricCategory, LogEventLevel.Information)
            .MinimumLevel.Override(DialogSearchCategory, LogEventLevel.Information)
            .WriteFeatureMetricsTo(writeTo => writeTo.Sink(sink))
            .CreateLogger();

        logger.ForContext(Constants.SourceContextPropertyName, FeatureMetricCategory).Information("feature metric");
        logger.ForContext(Constants.SourceContextPropertyName, DialogSearchCategory).Information("dialog search");
        logger.ForContext(Constants.SourceContextPropertyName, "Microsoft.AspNetCore.Hosting.Diagnostics").Warning("framework warning");
        logger.ForContext(Constants.SourceContextPropertyName, "Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetricLookalike").Warning("lookalike namespace");
        logger.Warning("no source context");

        sink.Events.Should().ContainSingle()
            .Which.MessageTemplate.Text.Should().Be("feature metric");
    }

    [Fact]
    public void Feature_metric_sink_reads_resource_variables_but_not_exporter_settings_from_the_environment()
    {
        List<string> requested = [];

        using var logger = new LoggerConfiguration()
            .WriteFeatureMetricsToOpenTelemetry(EndpointConfiguration("http://feature-metrics-collector:4317"), name =>
            {
                requested.Add(name);
                return null;
            })
            .CreateLogger();

        requested.Should().Contain(["OTEL_SERVICE_NAME", "OTEL_RESOURCE_ATTRIBUTES"])
            .And.NotContain(name => name.StartsWith("OTEL_EXPORTER_OTLP_", StringComparison.Ordinal));
    }

    [Fact]
    public void Missing_endpoint_adds_no_sink()
    {
        List<string> requested = [];

        using var logger = new LoggerConfiguration()
            .WriteFeatureMetricsToOpenTelemetry(new ConfigurationBuilder().Build(), name =>
            {
                requested.Add(name);
                return null;
            })
            .CreateLogger();

        // An OTLP sink reads the OTEL_* variables when it is created, so no reads means no sink.
        requested.Should().BeEmpty();
    }

    [Fact]
    public void Invalid_endpoint_fails_at_startup()
    {
        var act = () => new LoggerConfiguration().WriteFeatureMetricsToOpenTelemetry(EndpointConfiguration("not-a-uri"));

        act.Should().Throw<InvalidOperationException>();
    }

    private static IConfiguration EndpointConfiguration(string endpoint) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection([new(FeatureMetricLoggingExtensions.OtlpEndpointConfigurationKey, endpoint)])
            .Build();

    private sealed class CollectingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];
        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }
}
```

- [ ] **Step 2: Run the tests and see them fail**

Run: `dotnet build tests/Digdir.Domain.Dialogporten.WebApi.Unit.Tests --configuration Release`
Expected: the build fails because `FeatureMetricLoggingExtensions`, `WriteFeatureMetricsTo` and `WriteFeatureMetricsToOpenTelemetry` don't exist yet (`CS0103`, `CS1061`).

- [ ] **Step 3: Write the implementation**

Create `src/Digdir.Domain.Dialogporten.WebApi/Common/FeatureMetric/FeatureMetricLoggingExtensions.cs`:

```csharp
using System.Diagnostics;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Filters;
using Serilog.Sinks.OpenTelemetry;

namespace Digdir.Domain.Dialogporten.WebApi.Common.FeatureMetric;

internal static class FeatureMetricLoggingExtensions
{
    internal const string OtlpEndpointConfigurationKey = "FeatureMetrics:OtlpEndpoint";

    // LoggingFeatureMetricDeliveryContext (internal to Application) logs the feature metrics. It shares
    // its namespace with the public IFeatureMetricDeliveryContext, which is what we can reference here.
    private static readonly string FeatureMetricSourceContext =
        typeof(IFeatureMetricDeliveryContext).Namespace ?? throw new UnreachableException();

    private static readonly Func<LogEvent, bool> IsFeatureMetricEvent = Matching.FromSource(FeatureMetricSourceContext);

    extension(LoggerConfiguration configuration)
    {
        /// <summary>
        /// Also sends feature-metric log events to the OTLP endpoint in <c>FeatureMetrics:OtlpEndpoint</c>, when set.
        /// On DIS the platform collector drops logs below Warning, so the cost-allocation records need their own path.
        /// </summary>
        public LoggerConfiguration WriteFeatureMetricsToOpenTelemetry(IConfiguration appConfiguration) =>
            configuration.WriteFeatureMetricsToOpenTelemetry(appConfiguration, Environment.GetEnvironmentVariable);

        internal LoggerConfiguration WriteFeatureMetricsToOpenTelemetry(
            IConfiguration appConfiguration,
            Func<string, string?> getEnvironmentVariable)
        {
            var endpoint = appConfiguration[OtlpEndpointConfigurationKey];
            if (endpoint is null)
            {
                return configuration;
            }

            if (!Uri.IsWellFormedUriString(endpoint, UriKind.Absolute))
            {
                throw new InvalidOperationException($"Invalid {OtlpEndpointConfigurationKey}: {endpoint}");
            }

            return configuration.WriteFeatureMetricsTo(writeTo => writeTo.OpenTelemetry(
                options =>
                {
                    options.Endpoint = endpoint;
                    options.Protocol = OtlpProtocol.Grpc;
                },
                ResourceVariablesOnly(getEnvironmentVariable)));
        }

        internal LoggerConfiguration WriteFeatureMetricsTo(Action<LoggerSinkConfiguration> writeTo) =>
            configuration.WriteTo.Logger(featureMetrics =>
            {
                featureMetrics.Filter.ByIncludingOnly(IsFeatureMetricEvent);
                writeTo(featureMetrics.WriteTo);
            });
    }

    // Serilog.Sinks.OpenTelemetry applies the OTEL_* environment variables after the options callback, so
    // the pod's OTEL_EXPORTER_OTLP_ENDPOINT (the platform collector) would replace the endpoint above. Only
    // let through the variables that describe the resource, never the exporter settings.
    private static Func<string, string?> ResourceVariablesOnly(Func<string, string?> getVariable) =>
        name => name is "OTEL_SERVICE_NAME" or "OTEL_RESOURCE_ATTRIBUTES" ? getVariable(name) : null;
}
```

- [ ] **Step 4: Run the tests and see them pass**

Run:
```bash
dotnet build tests/Digdir.Domain.Dialogporten.WebApi.Unit.Tests --configuration Release
dotnet test tests/Digdir.Domain.Dialogporten.WebApi.Unit.Tests --configuration Release --no-build --filter "FullyQualifiedName~FeatureMetricLoggingExtensionsTests"
```
Expected: `Passed!  - Failed: 0, Passed: 4`.

- [ ] **Step 5: Guard the namespace the filter depends on**

Create `tests/Digdir.Domain.Dialogporten.Application.Unit.Tests/Common/Behaviours/FeatureMetric/FeatureMetricLogCategoryTests.cs`:

```csharp
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Common.Behaviours.FeatureMetric;

public class FeatureMetricLogCategoryTests
{
    // The WebApi selects feature-metric log events by the namespace of IFeatureMetricDeliveryContext
    // (FeatureMetricLoggingExtensions). Moving the logging delivery context out of that namespace would
    // silently stop the cost-allocation records on DIS.
    [Fact]
    public void Logging_delivery_context_is_in_the_feature_metric_namespace() =>
        typeof(LoggingFeatureMetricDeliveryContext).Namespace
            .Should().Be(typeof(IFeatureMetricDeliveryContext).Namespace);
}
```

Run: `dotnet test tests/Digdir.Domain.Dialogporten.Application.Unit.Tests --configuration Release --filter "FullyQualifiedName~FeatureMetricLogCategoryTests"`
Expected: `Passed: 1`. The test pins today's layout, so it passes straight away. It fails if the delivery context moves out of the namespace.

- [ ] **Step 6: Wire it into the WebApi**

In `src/Digdir.Domain.Dialogporten.WebApi/Program.cs`, extend the `UseSerilog` call. `Digdir.Domain.Dialogporten.WebApi.Common.FeatureMetric` is already imported.

```csharp
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .MinimumLevel.Warning()
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.WithEnvironmentName()
        .Enrich.FromLogContext()
        .Filter.WithHandledPostgresExceptionFilter()
        .WriteTo.OpenTelemetryOrConsole(context)
        .WriteFeatureMetricsToOpenTelemetry(context.Configuration));
```

The sub-logger sees events after the root logger's level overrides, filters and enrichers. Feature metrics therefore keep `EnvironmentName` and the Information override from `appsettings.json`.

- [ ] **Step 7: Update the feature metrics docs**

Replace section "3. Delivery Layer" (`docs/FeatureMetrics.md:68-81`) with the text below. The current text names an `OtelFeatureMetricLoggingDeliveryContext` that no longer exists.

```markdown
### 3. Delivery Layer

#### Delivery Context
`LoggingFeatureMetricDeliveryContext` is the only implementation, in every environment. It logs each
record at Information with `EventId` 1000 (`LogFeatureMetric`). The WebApi's minimum level is Warning;
`appsettings.json` raises this category to Information, and it must stay that way.

#### Where the records go
- **Container Apps:** Serilog sends all log events over OTLP to the environment's managed OpenTelemetry
  agent, which exports them to `dp-be-<env>-applicationInsights`.
- **DIS:** the platform collector drops log records below Warning. The WebApi therefore also sends the
  feature-metric events, and only those, to `FeatureMetrics:OtlpEndpoint`
  (`FeatureMetricLoggingExtensions`). On DIS that is `feature-metrics-collector` in `product-dialogporten`,
  which exports them to the same `dp-be-<env>-applicationInsights`. The setting is not set on Container
  Apps; setting it there would count every request twice.

The `aggregate-cost-metrics` Janitor job queries `dp-be-<env>-applicationInsights` for `EventId` 1000 once
a day and writes the aggregate to the `costmetrics` storage container.
```

- [ ] **Step 8: Build and run the default test suite (AGENTS.md)**

Run:
```bash
dotnet build Digdir.Domain.Dialogporten.slnx --configuration Release
dotnet test Digdir.Domain.Dialogporten.slnx --configuration Release --no-build --filter 'FullyQualifiedName!~E2E'
```
Expected: the build succeeds with 0 warnings, and all tests pass.

- [ ] **Step 9: Commit**

```bash
git add src/Digdir.Domain.Dialogporten.WebApi/Common/FeatureMetric/FeatureMetricLoggingExtensions.cs \
        src/Digdir.Domain.Dialogporten.WebApi/Program.cs \
        tests/Digdir.Domain.Dialogporten.WebApi.Unit.Tests/FeatureMetricLoggingExtensionsTests.cs \
        tests/Digdir.Domain.Dialogporten.Application.Unit.Tests/Common/Behaviours/FeatureMetric/FeatureMetricLogCategoryTests.cs \
        docs/FeatureMetrics.md
git commit -m "feat(webapi): send feature metrics to a dedicated OTLP endpoint when configured"
```

### Task B3: Feature-metrics collector and WebApi wiring

**Repo:** Altinn/dialogporten-manifests

**Files:**
- Create: `manifests/common/base/feature-metrics-collector.yaml`
- Create: `manifests/common/base/feature-metrics-collector-config.yaml`
- Modify: `manifests/common/base/kustomization.yaml`
- Modify: `manifests/apps/web-api-so/base/resources.yaml:73-85` (container `env`)
- Modify: `manifests/apps/web-api-eu/base/resources.yaml` (container `env`, same place)
- Modify: `docs/summary.md` (`## Structure`), `AGENTS.md:15` (`## Registry model`) and `README.md:19`. The repo's `AGENTS.md` requires all three for structural changes.

**Interfaces:**
- Consumes: Key Vault secret `dialogportenAppInsightsConnectionString` (Task B1) and configuration key `FeatureMetrics:OtlpEndpoint` (Task B2).
- Produces: OTLP gRPC endpoint `feature-metrics-collector.product-dialogporten.svc.cluster.local:4317`.

Why the manifests look the way they do:
- **Placement.** `common/base` is included by every environment. The Key Vault URL comes from the per-environment SecretStore patch, so no per-environment overlay is needed.
- **Image pinning.** The image is pinned in the manifest. CI rewrites every `newTag` under `images:` in the environment kustomizations to the app version (`workflow-update-all-image-tags.yml`).
- **Linkerd.** Inbound traffic is denied by default. The OTLP port admits only the `web-api-so` and `web-api-eu` identities. The health port admits only the kubelet, as the apps' probe routes do.
- **No scope filter in the collector.** Filtering by class name here would copy that name into a second repo, where no test guards it. The WebApi decides what to send, and Linkerd decides who may send.

- [ ] **Step 1: Add the collector config**

Create `manifests/common/base/feature-metrics-collector-config.yaml`:

```yaml
# Collector config for feature-metrics-collector.yaml. Kustomize's configMapGenerator
# hashes the ConfigMap name, so a change here rolls the collector pods.
extensions:
  health_check:
    endpoint: 0.0.0.0:13133
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317
processors:
  memory_limiter:
    check_interval: 1s
    limit_percentage: 75
    spike_limit_percentage: 15
  batch: {}
exporters:
  # Reads APPLICATIONINSIGHTS_CONNECTION_STRING.
  azuremonitor: {}
service:
  extensions: [health_check]
  pipelines:
    logs:
      receivers: [otlp]
      processors: [memory_limiter, batch]
      exporters: [azuremonitor]
```

- [ ] **Step 2: Add the collector workload, secret and Linkerd policy**

Create `manifests/common/base/feature-metrics-collector.yaml`:

```yaml
# Receives Dialogporten's cost-allocation feature metrics from web-api-so and web-api-eu
# and sends them to Dialogporten's own Application Insights (dp-be-<env>-applicationInsights),
# which the aggregate-cost-metrics job queries. The platform collector
# (otel-collector.monitoring) drops logs below Warning, so these records cannot go that way.
# See docs/FeatureMetrics.md in Altinn/dialogporten.
#
# It forwards whatever it receives: the WebApi decides what to send (by logger namespace),
# and the Linkerd policy below decides who may send.
apiVersion: apps/v1
kind: Deployment
metadata:
  name: feature-metrics-collector
  labels:
    app.kubernetes.io/name: feature-metrics-collector
    app.kubernetes.io/part-of: dialogporten
spec:
  replicas: 2
  selector:
    matchLabels:
      app.kubernetes.io/name: feature-metrics-collector
  template:
    metadata:
      labels:
        app.kubernetes.io/name: feature-metrics-collector
        app.kubernetes.io/part-of: dialogporten
      annotations:
        cluster-autoscaler.kubernetes.io/safe-to-evict: "true"
        linkerd.io/inject: enabled
        # As on the platform collector: Azure Monitor ingestion is reached without the proxy.
        config.linkerd.io/skip-outbound-ports: "443"
    spec:
      automountServiceAccountToken: false
      containers:
        - name: otel-collector
          # Pinned here, not in the env kustomization's images: block, which CI overwrites.
          image: altinncr.azurecr.io/ghcr.io/open-telemetry/opentelemetry-collector-releases/opentelemetry-collector-contrib:0.140.1
          args:
            - --config=/conf/config.yaml
          ports:
            - name: otlp-grpc
              containerPort: 4317
            - name: health
              containerPort: 13133
          env:
            - name: APPLICATIONINSIGHTS_CONNECTION_STRING
              valueFrom:
                secretKeyRef:
                  name: feature-metrics-collector-secrets
                  key: dialogportenAppInsightsConnectionString
          resources:
            requests:
              cpu: 50m
              memory: 128Mi
            limits:
              memory: 256Mi
          readinessProbe:
            httpGet:
              path: /
              port: health
          livenessProbe:
            httpGet:
              path: /
              port: health
          volumeMounts:
            - name: config
              mountPath: /conf
      volumes:
        - name: config
          configMap:
            name: feature-metrics-collector-config
---
apiVersion: v1
kind: Service
metadata:
  name: feature-metrics-collector
  labels:
    app.kubernetes.io/name: feature-metrics-collector
    app.kubernetes.io/part-of: dialogporten
spec:
  selector:
    app.kubernetes.io/name: feature-metrics-collector
  ports:
    - name: otlp-grpc
      port: 4317
      targetPort: otlp-grpc
---
apiVersion: external-secrets.io/v1
kind: ExternalSecret
metadata:
  name: feature-metrics-collector-secrets
  labels:
    app.kubernetes.io/name: feature-metrics-collector
    app.kubernetes.io/part-of: dialogporten
spec:
  refreshInterval: 1h
  secretStoreRef:
    kind: SecretStore
    name: dialogporten-azure-kv
  target:
    name: feature-metrics-collector-secrets
    creationPolicy: Owner
  data:
    - secretKey: dialogportenAppInsightsConnectionString
      remoteRef:
        key: dialogportenAppInsightsConnectionString
---
# OTLP from web-api-so and web-api-eu only: everything that arrives here is billed.
apiVersion: policy.linkerd.io/v1beta3
kind: Server
metadata:
  name: feature-metrics-collector-otlp
  labels:
    app.kubernetes.io/part-of: dialogporten
spec:
  podSelector:
    matchLabels:
      app.kubernetes.io/name: feature-metrics-collector
  port: 4317
  proxyProtocol: gRPC
---
apiVersion: policy.linkerd.io/v1alpha1
kind: MeshTLSAuthentication
metadata:
  name: feature-metrics-senders
  labels:
    app.kubernetes.io/part-of: dialogporten
spec:
  identities:
    - "web-api-so.product-dialogporten.serviceaccount.identity.linkerd.cluster.local"
    - "web-api-eu.product-dialogporten.serviceaccount.identity.linkerd.cluster.local"
---
apiVersion: policy.linkerd.io/v1alpha1
kind: AuthorizationPolicy
metadata:
  name: feature-metrics-collector-allow-web-api
  labels:
    app.kubernetes.io/part-of: dialogporten
spec:
  targetRef:
    group: policy.linkerd.io
    kind: Server
    name: feature-metrics-collector-otlp
  requiredAuthenticationRefs:
    - group: policy.linkerd.io
      kind: MeshTLSAuthentication
      name: feature-metrics-senders
---
# The kubelet probes :13133 unmeshed from the node, as it does the apps' :8080 probes.
apiVersion: policy.linkerd.io/v1beta3
kind: Server
metadata:
  name: feature-metrics-collector-health
  labels:
    app.kubernetes.io/part-of: dialogporten
spec:
  podSelector:
    matchLabels:
      app.kubernetes.io/name: feature-metrics-collector
  port: 13133
  proxyProtocol: HTTP/1
---
apiVersion: policy.linkerd.io/v1alpha1
kind: AuthorizationPolicy
metadata:
  name: feature-metrics-collector-allow-kubelet
  labels:
    app.kubernetes.io/part-of: dialogporten
spec:
  targetRef:
    group: policy.linkerd.io
    kind: Server
    name: feature-metrics-collector-health
  requiredAuthenticationRefs:
    - group: policy.linkerd.io
      kind: NetworkAuthentication
      name: dialogporten-kubelet
```

- [ ] **Step 3: Register both in the common base**

Replace `manifests/common/base/kustomization.yaml` with:

```yaml
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization
resources:
  - applicationidentity-secrets.yaml
  - external-secrets.yaml
  - feature-metrics-collector.yaml
  - linkerd-policies.yaml
  - runtime-config.yaml
configMapGenerator:
  - name: feature-metrics-collector-config
    files:
      - config.yaml=feature-metrics-collector-config.yaml
```

- [ ] **Step 4: Point the web APIs at the collector**

In `manifests/apps/web-api-so/base/resources.yaml`, add this entry to the container's `env`, directly after `OTEL_RESOURCE_ATTRIBUTES`. Do the same in `manifests/apps/web-api-eu/base/resources.yaml`:

```yaml
            # Feature metrics (cost allocation) go to feature-metrics-collector, not the platform
            # collector, which drops logs below Warning. See FeatureMetricLoggingExtensions.
            - name: FeatureMetrics__OtlpEndpoint
              value: http://feature-metrics-collector.product-dialogporten.svc.cluster.local:4317
```

These manifests only run on DIS, so Container Apps never gets the setting.

- [ ] **Step 5: Document it**

In `docs/summary.md`, add a bullet to `## Structure`:

```markdown
- `manifests/common/base/feature-metrics-collector.yaml`: OpenTelemetry Collector that sends the web APIs' feature metrics (cost allocation) to `dp-be-<env>-applicationInsights`, since the platform collector drops logs below Warning. Its image is pinned in the manifest, not in the env `images:` block.
```

In `AGENTS.md`, replace the first bullet of `## Registry model` (line 15) with:

```markdown
- Runtime app images: GHCR tags set in `manifests/environments/<env>/kustomization.yaml`. Exception: the feature-metrics collector image is pinned in `manifests/common/base/feature-metrics-collector.yaml`; do not move it to `images:`, which CI overwrites with the app tag.
```

In `README.md`, replace line 19 with:

```markdown
Application runtime images remain GHCR-hosted and are pinned by tags in `manifests/environments/<env>/kustomization.yaml`. The feature-metrics collector image is the exception: it is pinned in `manifests/common/base/feature-metrics-collector.yaml`.
```

- [ ] **Step 6: Build every environment and validate**

Run:
```bash
for env in at23 tt02 yt01 prod; do kustomize build manifests/environments/$env > /tmp/$env.yaml && echo "$env ok"; done
for env in at23 tt02 yt01 prod; do kustomize build flux/syncroot/$env > /dev/null && echo "syncroot $env ok"; done
kubeconform -strict -summary -skip ScaledObject,HTTPRoute,ApplicationIdentity \
  -schema-location default \
  -schema-location 'https://raw.githubusercontent.com/datreeio/CRDs-catalog/main/{{.Group}}/{{.ResourceKind}}_{{.ResourceAPIVersion}}.json' \
  /tmp/at23.yaml
docker run --rm -e APPLICATIONINSIGHTS_CONNECTION_STRING='InstrumentationKey=00000000-0000-0000-0000-000000000000' \
  -v "$PWD/manifests/common/base/feature-metrics-collector-config.yaml:/conf/config.yaml:ro" \
  otel/opentelemetry-collector-contrib:0.140.1 validate --config=/conf/config.yaml
```

Expected:
- Every build prints `ok`.
- kubeconform reports `Invalid: 0, Errors: 0`.
- `validate` exits with code 0.
- `/tmp/at23.yaml` contains `feature-metrics-collector-config-<hash>` and two `FeatureMetrics__OtlpEndpoint` entries.

Optional: check what the collector exports, using the mock in the [appendix](#appendix-local-ingestion-mock), which must be running:

```bash
yq '.receivers.otlp.protocols.http.endpoint = "0.0.0.0:4318"' manifests/common/base/feature-metrics-collector-config.yaml > /tmp/fm-mock/collector-3b-local.yaml
docker run -d --name otel-3b -p 24318:4318 --add-host host.docker.internal:host-gateway \
  -e 'APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=00000000-0000-0000-0000-00000000dddd;IngestionEndpoint=http://host.docker.internal:18082/' \
  -v /tmp/fm-mock/collector-3b-local.yaml:/etc/otelcol-contrib/config.yaml:ro otel/opentelemetry-collector-contrib:0.140.1
sleep 5 && curl -s -X POST -H 'Content-Type: application/json' --data @/tmp/fm-mock/payload.json http://localhost:24318/v1/logs && sleep 4
grep DP-FEATURE-METRIC-INFO /tmp/fm-mock/mock.log
docker rm -f otel-3b
```

Expected: `[dialogporten-ai] ... EventId={"Id":1000,"Name":"LogFeatureMetric"} AdditionalTags={"CorrelationId":"0HN1","Status":"success","StatusCode":200} HasAdminScope=false`.

- [ ] **Step 7: Commit**

```bash
git add manifests/common/base docs/summary.md AGENTS.md README.md \
        manifests/apps/web-api-so/base/resources.yaml manifests/apps/web-api-eu/base/resources.yaml
git commit -m "feat: send feature metrics to dp-be Application Insights via a dedicated collector"
```

### Task B4: Verify in at23, then roll out

**Deploy order in at23:**
1. Task B1: infrastructure deploy to `test`.
2. Task B3: Flux applies it from `main`.
3. Task B2: the next WebApi image.

The web APIs ignore `FeatureMetrics__OtlpEndpoint` until they run the Task B2 code.

- [ ] **Step 1: Secret, collector and wiring are in place**

Run:
```bash
kubectl -n product-dialogporten get externalsecret feature-metrics-collector-secrets
kubectl -n product-dialogporten get pods -l app.kubernetes.io/name=feature-metrics-collector
kubectl -n product-dialogporten exec deploy/web-api-so -c web-api-so -- printenv FeatureMetrics__OtlpEndpoint
```

Expected:
- The ExternalSecret shows `SecretSynced` and `True`.
- Two collector pods are `2/2 Running`.
- The endpoint URL is printed.

- [ ] **Step 2: Records arrive from DIS**

In `dp-be-test-applicationInsights` → Logs, run:

```kusto
traces
| where timestamp > ago(1h)
| extend EventIdNum = toint(extractjson("$.Id", tostring(customDimensions.EventId)))
| where EventIdNum == 1000
| summarize count() by cloud_RoleName, bin(timestamp, 5m)
```

Expected: rows with `cloud_RoleName` `dialogporten-web-api-so` or `dialogporten-web-api-eu` (from `OTEL_SERVICE_NAME`) while at23 has traffic.

- [ ] **Step 3: The cost query reads them**

Run the query from `ApplicationInsightsService.cs:47-66`, with its time filter replaced by the last hour:

```kusto
traces
| where timestamp > ago(1h)
| extend EventIdNum = toint(extractjson("$.Id", tostring(customDimensions.EventId)))
| where isnotnull(EventIdNum) and EventIdNum == 1000
| extend FeatureType = tostring(customDimensions["FeatureType"])
| extend HasAdminScope = tobool(customDimensions["HasAdminScope"])
| extend CallerOrg = tostring(customDimensions["CallerOrg"])
| extend OwnerOrg = tostring(customDimensions["OwnerOrg"])
| extend ServiceResource = tostring(customDimensions["ServiceResource"])
| extend PresentationTag = tostring(customDimensions["PresentationTag"])
| extend AdditionalTags = tostring(customDimensions["AdditionalTags"])
| extend AdditionalTagsParsed = parse_json(AdditionalTags)
| extend Status = tostring(AdditionalTagsParsed["Status"])
| extend StatusCode = tostring(AdditionalTagsParsed["StatusCode"])
| summarize count() by FeatureType, HasAdminScope, CallerOrg, OwnerOrg, ServiceResource, PresentationTag, Status, StatusCode
| where isnotempty(FeatureType)
```

Expected: rows with a non-empty `FeatureType`, and with `Status` and `StatusCode` filled in.

- [ ] **Step 4: Container Apps does not send twice**

Run: `grep -rn "FeatureMetrics__OtlpEndpoint\|FeatureMetrics:OtlpEndpoint" .azure src/*/appsettings*.json`
Expected: no matches.

- [ ] **Step 5: Roll out**

The manifests already cover `yt01`, `tt02` and `prod`. For each of these, before its syncroot is bootstrapped (`product-dialogporten.tf` in that environment's dis-way/core stack):
- Deploy Task B1 there.
- Run Step 4 of Task B1 against that environment's Key Vault.

After bootstrap, repeat Steps 1-3 against `dp-be-<env>-applicationInsights`. From the `staging` cutover on, the staging cost job counts records from both platforms without any change.

---

## Plan 3A: Platform collector exports to dp-be

**Order:**
- A1 goes out as its own release, because it reaches every cluster.
- A2 and A3 can merge in either order.
- A4 needs both, plus a release containing A2 that has been promoted to the cluster's ring.
- A5 comes last.

**Known limitations:**
- **Who can send.** The platform collector accepts OTLP from every pod in the cluster (`accessPolicy: cluster-unauthenticated`). Any workload that sends records with the feature-metric scope name ends up in Dialogporten's billing data.
- **Startup coupling.** In clusters that use the overlay, the cluster-wide collector can only start its new pods with a valid Dialogporten connection string. A4 validates the value, and a rolling update keeps the old pods running.

**Rollback:**
- Set `otel_collector_path` back to `./multitenancy` in the cluster's stack and apply.
- The Flux configuration has `prune = false`, so the overlay's ExternalSecret stays behind in `monitoring`. Remove it with `kubectl -n monitoring delete externalsecret dialogporten-app-insights-connstring-external-secret`.

### Task A1: Stop the platform connection string from overriding other exporters

**Repo:** dis-way/gitops-manifests

**Files:**
- Modify: `oci/otel-collector/base/collector.yaml:9-14` (`spec.env`)
- Modify: `oci/otel-collector/base/collector.yaml:437` (`exporters.azuremonitor`)

**Interfaces:**
- Produces: environment variable `DIS_APPLICATIONINSIGHTS_CONNECTION_STRING`, read explicitly by `exporters.azuremonitor`. Nothing in the collector sets `APPLICATIONINSIGHTS_CONNECTION_STRING` any more, so a second `azuremonitor` exporter keeps its own `connection_string`. Task A2 depends on this.

- [ ] **Step 1: Rename the variable**

In `oci/otel-collector/base/collector.yaml`, replace the first entry of `spec.env`:

```yaml
  env:
    # Not APPLICATIONINSIGHTS_CONNECTION_STRING: the azuremonitor exporter reads that variable
    # before its own connection_string, so it would apply to every azuremonitor exporter here.
    - name: DIS_APPLICATIONINSIGHTS_CONNECTION_STRING
      valueFrom:
        secretKeyRef:
          name: app-insights-connstring
          key: connectionString
```

- [ ] **Step 2: Read it explicitly**

Replace `      azuremonitor: {}` under `exporters:` with:

```yaml
      azuremonitor:
        connection_string: "$${env:DIS_APPLICATIONINSIGHTS_CONNECTION_STRING}"
```

The `$$` escapes Flux's post-build substitution, the same way `azureauth` does in this file.

- [ ] **Step 3: Render every path**

Run from the repository root:

```bash
(cd oci/otel-collector && for p in . apps multitenancy adminservices; do kustomize build $p > /dev/null && echo "$p ok"; done)
kustomize build oci/otel-collector/multitenancy \
  | KV_URI=https://kv.example CLIENT_ID=x TENANT_ID=x AMW_WRITE_ENDPOINT=https://amw.example flux envsubst --strict \
  | yq 'select(.kind == "OpenTelemetryCollector") | .spec.config' > /tmp/rendered-config.yaml
docker run --rm -e AZURE_TENANT_ID=t -e AZURE_CLIENT_ID=c -e AZURE_FEDERATED_TOKEN_FILE=/tmp/f \
  -v /tmp/rendered-config.yaml:/cfg.yaml:ro otel/opentelemetry-collector-contrib:0.140.1 validate --config=/cfg.yaml
```

Expected:
- All four paths print `ok`.
- `flux envsubst --strict` does not fail.
- `validate` exits with code 0.

- [ ] **Step 4: Commit and release**

```bash
git add oci/otel-collector/base/collector.yaml
git commit -m "fix(otel-collector): read the App Insights connection string from a dedicated variable"
```

Release `oci-otel-collector` and promote it through the rings. The platform App Insights should keep receiving Warning logs from every cluster.

### Task A2: Overlay with the Dialogporten feature-metrics pipeline

**Repo:** dis-way/gitops-manifests

**Files:**
- Create: `oci/otel-collector/multitenancy-dialogporten/kustomization.yaml`
- Create: `oci/otel-collector/multitenancy-dialogporten/dialogporten-external-secret.yaml`
- Create: `oci/otel-collector/multitenancy-dialogporten/collector-env-patch.yaml`
- Create: `oci/otel-collector/multitenancy-dialogporten/collector-patch.yaml`
- Modify: `oci/otel-collector/README.md`, the `## Layers` table (lines 157-165)
- Modify: `.github/workflows/pull-request.yml:15` (`build-paths`)

**Interfaces:**
- Consumes: `DIS_APPLICATIONINSIGHTS_CONNECTION_STRING` (Task A1) and the SecretStore `otel-azure-kv-store` from `base`.
- Produces:
  - Overlay path `./multitenancy-dialogporten`, selected in Task A4.
  - Name of the observability Key Vault secret, `dialogporten-connectionString`, written in Task A4.

This has to be an overlay. The exporter refuses to start without a connection string, and only clusters that host Dialogporten have one.

- [ ] **Step 1: Kustomization**

Create `oci/otel-collector/multitenancy-dialogporten/kustomization.yaml`:

```yaml
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization
resources:
  - ../multitenancy
  - dialogporten-external-secret.yaml
patches:
  - path: collector-env-patch.yaml
    target:
      kind: OpenTelemetryCollector
      name: otel
  - path: collector-patch.yaml
    target:
      kind: OpenTelemetryCollector
      name: otel
```

- [ ] **Step 2: Secret**

Create `oci/otel-collector/multitenancy-dialogporten/dialogporten-external-secret.yaml`:

```yaml
apiVersion: external-secrets.io/v1
kind: ExternalSecret
metadata:
  name: dialogporten-app-insights-connstring-external-secret
  namespace: monitoring
spec:
  refreshInterval: 1h
  secretStoreRef:
    kind: SecretStore
    name: otel-azure-kv-store
  target:
    name: dialogporten-app-insights-connstring
    creationPolicy: Owner
  data:
    - secretKey: connectionString
      remoteRef:
        key: dialogporten-connectionString
```

- [ ] **Step 3: Env patch**

Create `oci/otel-collector/multitenancy-dialogporten/collector-env-patch.yaml`:

```yaml
# JSON6902: a merge patch would replace the whole env list.
- op: add
  path: /spec/env/-
  value:
    name: DIALOGPORTEN_APPLICATIONINSIGHTS_CONNECTION_STRING
    valueFrom:
      secretKeyRef:
        name: dialogporten-app-insights-connstring
        key: connectionString
```

- [ ] **Step 4: Pipeline**

Create `oci/otel-collector/multitenancy-dialogporten/collector-patch.yaml`:

```yaml
apiVersion: opentelemetry.io/v1beta1
kind: OpenTelemetryCollector
metadata:
  name: otel
  namespace: monitoring
spec:
  config:
    processors:
      # Dialogporten's cost allocation ("feature metrics") is INFO logs, which filter/logs drops.
      # This pipeline keeps only those records and sends them to Dialogporten's own
      # Application Insights (Altinn/altinn-platform#4135).
      filter/dialogporten-feature-metrics:
        error_mode: ignore
        logs:
          log_record:
            - 'instrumentation_scope.name != "Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric.LoggingFeatureMetricDeliveryContext"'
    exporters:
      azuremonitor/dialogporten:
        connection_string: "$${env:DIALOGPORTEN_APPLICATIONINSIGHTS_CONNECTION_STRING}"
    service:
      pipelines:
        logs/dialogporten-feature-metrics:
          receivers: [otlp]
          processors: [filter/dialogporten-feature-metrics, memory_limiter, batch]
          exporters: [azuremonitor/dialogporten]
```

The main `logs` pipeline is unchanged and still drops these records, so they are not duplicated into the platform App Insights.

- [ ] **Step 5: Document the layer and build it on PRs**

Add this row to the `## Layers` table in `oci/otel-collector/README.md`:

```markdown
| `multitenancy-dialogporten` | `multitenancy` plus a `logs/dialogporten-feature-metrics` pipeline that sends Dialogporten's feature metrics (INFO, which `filter/logs` drops) to Dialogporten's own Application Insights; reads `dialogporten-connectionString` from the Key Vault |
```

In `.github/workflows/pull-request.yml`, change `build-paths` so that PR CI builds the new overlay. Its globs don't match it today.

```yaml
      build-paths: "oci/*,oci/*/apps,oci/*/multitenancy,oci/*/multitenancy-dialogporten,oci/*/adminservices,oci/*/platform-aks,oci/*/edge"
```

- [ ] **Step 6: Render and validate**

Run from the repository root:

```bash
(cd oci/otel-collector && for p in . apps multitenancy adminservices multitenancy-dialogporten; do kustomize build $p > /dev/null && echo "$p ok"; done)
kustomize build oci/otel-collector/multitenancy-dialogporten \
  | KV_URI=https://kv.example CLIENT_ID=x TENANT_ID=x AMW_WRITE_ENDPOINT=https://amw.example flux envsubst --strict \
  | yq 'select(.kind == "OpenTelemetryCollector") | .spec.config' > /tmp/rendered-config.yaml
docker run --rm -e AZURE_TENANT_ID=t -e AZURE_CLIENT_ID=c -e AZURE_FEDERATED_TOKEN_FILE=/tmp/f \
  -v /tmp/rendered-config.yaml:/cfg.yaml:ro otel/opentelemetry-collector-contrib:0.140.1 validate --config=/cfg.yaml
```

Expected: five paths print `ok`, and `validate` exits with code 0.

- [ ] **Step 7: Prove the routing locally**

With the [mock](#appendix-local-ingestion-mock) running, keep the rendered logs pipelines. Drop only what needs a cluster:

```bash
yq '{
  "receivers": {"otlp": {"protocols": {"http": {"endpoint": "0.0.0.0:4318"}}}},
  "processors": (.processors | pick(["filter/logs", "filter/dialogporten-feature-metrics", "memory_limiter", "transform/drop", "batch"])),
  "exporters": (.exporters | pick(["azuremonitor", "azuremonitor/dialogporten"])),
  "service": {"pipelines": {
    "logs": (.service.pipelines.logs | .processors |= map(select(. != "resourcedetection/aks" and . != "k8sattributes"))),
    "logs/dialogporten-feature-metrics": .service.pipelines["logs/dialogporten-feature-metrics"]}}
}' /tmp/rendered-config.yaml > /tmp/fm-mock/collector-3a-local.yaml

docker run -d --name otel-3a -p 34318:4318 --add-host host.docker.internal:host-gateway \
  -e 'DIS_APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=00000000-0000-0000-0000-00000000aaaa;IngestionEndpoint=http://host.docker.internal:18081/' \
  -e 'DIALOGPORTEN_APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=00000000-0000-0000-0000-00000000dddd;IngestionEndpoint=http://host.docker.internal:18082/' \
  -v /tmp/fm-mock/collector-3a-local.yaml:/etc/otelcol-contrib/config.yaml:ro otel/opentelemetry-collector-contrib:0.140.1
sleep 5 && curl -s -X POST -H 'Content-Type: application/json' --data @/tmp/fm-mock/payload.json http://localhost:34318/v1/logs && sleep 4
cat /tmp/fm-mock/mock.log
docker rm -f otel-3a
```

Expected in `mock.log`:

```text
[platform-ai] ... msg='DP-FRAMEWORK-WARN' ...
[platform-ai] ... msg='OTHER-WARN' ...
[dialogporten-ai] ... msg='DP-FEATURE-METRIC-INFO' ... EventId={"Id":1000,"Name":"LogFeatureMetric"} ...
```

Control run, reproducing the state before A1: truncate `mock.log`, then repeat the `docker run` with one extra argument:

```bash
-e 'APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=00000000-0000-0000-0000-00000000aaaa;IngestionEndpoint=http://host.docker.internal:18081/'
```

`DP-FEATURE-METRIC-INFO` now arrives at `[platform-ai]`, and nothing arrives at `[dialogporten-ai]`. That is why Task A1 exists.

- [ ] **Step 8: Commit, release and promote**

```bash
git add oci/otel-collector/multitenancy-dialogporten oci/otel-collector/README.md .github/workflows/pull-request.yml
git commit -m "feat(otel-collector): send Dialogporten feature metrics to its own Application Insights"
```

Release `oci-otel-collector`, and promote the release through `oci/releaseconfig.json` at least to the ring of the cluster that opts in first: `at_ring2` for at23. Clusters opt in through Task A4.

### Task A3: Module inputs

**Repo:** dis-way/terraform-azurerm-dis-modules

**Files:**
- Modify: `modules/dis_aks_resources_multitenancy/otel-collector.tf:11` (`path  = "./multitenancy"`)
- Modify: `modules/dis_aks_resources_multitenancy/variables-otel.tf`
- Modify: `modules/dis_monitoring_resources/output.tf`
- Modify: `modules/dis_aks_resources_multitenancy/README.md` and `modules/dis_monitoring_resources/README.md` (generated)

**Interfaces:**
- Produces: variable `otel_collector_path` (`string`, default `"./multitenancy"`) and output `key_vault_id` on `dis_monitoring_resources`.

- [ ] **Step 1: Make the collector path configurable**

In `modules/dis_aks_resources_multitenancy/otel-collector.tf`, replace `path  = "./multitenancy"` with `path  = var.otel_collector_path`. Then append to `modules/dis_aks_resources_multitenancy/variables-otel.tf`:

```hcl
variable "otel_collector_path" {
  type        = string
  default     = "./multitenancy"
  description = "Path in the otel-collector OCI artifact that Flux reconciles. Clusters that host Dialogporten use ./multitenancy-dialogporten, which also sends its feature metrics to Dialogporten's own Application Insights."
}
```

- [ ] **Step 2: Expose the observability Key Vault ID**

Append to `modules/dis_monitoring_resources/output.tf`:

```hcl
output "key_vault_id" {
  value       = azurerm_key_vault.obs_kv.id
  description = "ID of the observability Key Vault that the otel collector reads its secrets from"
}
```

- [ ] **Step 3: Validate and regenerate the docs**

Run:

```bash
for m in dis_aks_resources_multitenancy dis_monitoring_resources; do
  (cd modules/$m && terraform fmt -check && terraform init -backend=false -input=false > /dev/null && terraform validate)
done
./generate-docs.sh --module dis_aks_resources_multitenancy
./generate-docs.sh --module dis_monitoring_resources
```

Expected: `Success! The configuration is valid.` for both modules, and two regenerated READMEs. CI fails if they are stale.

- [ ] **Step 4: Commit**

```bash
git add modules/dis_aks_resources_multitenancy modules/dis_monitoring_resources
git commit -m "feat(dis_aks_resources_multitenancy): make the otel collector path configurable"
```

### Task A4: Wire at23

**Repo:** dis-way/core

**Files:**
- Modify: every `?ref=` in `tf/dis-core-test/dis-core-at23-aks-rg/*.tf`. Bump it to the Task A3 merge commit; Renovate usually opens this as `deps: update dis-modules`.
- Modify: `tf/dis-core-test/dis-core-at23-aks-rg/dis-aks-resources.tf` (module `dis_aks_resources_multitenancy`)
- Modify: `tf/dis-core-test/dis-core-at23-aks-rg/dis-monitoring.tf`
- Modify: `tf/dis-core-test/dis-core-at23-aks-rg/variables.tf`
- Modify: `.github/workflows/dis-core-at23-aks-rg.yaml` (`env:`)

**Interfaces:**
- Consumes: `otel_collector_path` and `module.monitoring.key_vault_id` (Task A3); overlay `./multitenancy-dialogporten` and secret name `dialogporten-connectionString` (Task A2).

- [ ] **Step 1: Check that at23's ring has the overlay**

In dis-way/gitops-manifests, run: `jq -r '."otel-collector".at_ring2' oci/releaseconfig.json`

Expected: the version released in Task A2 Step 8, or later.

The Flux configuration pulls the ring's tag and waits for reconciliation (`wait = true`). Pointing at a path the artifact doesn't have yet fails the at23 apply.

- [ ] **Step 2: Store Dialogporten's connection string for the collector**

Append to `variables.tf`:

```hcl
variable "dialogporten_app_insights_connection_string" {
  type        = string
  sensitive   = true
  description = "Connection string of Dialogporten's Application Insights for this environment (dp-be-test-applicationInsights). The otel collector sends Dialogporten's feature metrics there."

  validation {
    # An empty or malformed value would stop the cluster's otel collector from starting.
    condition     = startswith(var.dialogporten_app_insights_connection_string, "InstrumentationKey=")
    error_message = "Must be an Application Insights connection string (InstrumentationKey=...)."
  }
}
```

Append to `dis-monitoring.tf`:

```hcl
resource "azurerm_key_vault_secret" "dialogporten_app_insights_connection_string" {
  name            = "dialogporten-connectionString"
  value           = var.dialogporten_app_insights_connection_string
  key_vault_id    = module.monitoring.key_vault_id
  expiration_date = timeadd(timestamp(), "8760h") # 1 year, as module.monitoring does for connectionString

  lifecycle {
    ignore_changes = [expiration_date]
  }
}
```

Add this line to the `env:` block of `.github/workflows/dis-core-at23-aks-rg.yaml`:

```yaml
  TF_VAR_dialogporten_app_insights_connection_string: ${{ secrets.DIALOGPORTEN_APPINSIGHTS_CONNECTION_STRING_AT23 }}
```

Create the repository secret `DIALOGPORTEN_APPINSIGHTS_CONNECTION_STRING_AT23`. Its value comes from one of:
- `az monitor app-insights component show -g dp-be-test-rg -a dp-be-test-applicationInsights --query connectionString -o tsv`
- the Task B1 Key Vault secret, if it exists

- [ ] **Step 3: Select the overlay once the secret exists**

In `dis-aks-resources.tf`, add the input next to the other `otel_*` inputs of the `dis_aks_resources_multitenancy` module. Also add the secret to its `depends_on`, so the collector does not come up before the secret it reads:

```hcl
  depends_on                                   = [module.aks, azapi_resource.platform_system, azurerm_key_vault_secret.dialogporten_app_insights_connection_string]
  ...
  otel_collector_path                          = "./multitenancy-dialogporten"
```

- [ ] **Step 4: Plan**

Run: `terraform fmt -check` and `terraform plan` in `tf/dis-core-test/dis-core-at23-aks-rg`, through the workflow as usual.

Expected:
- `azurerm_key_vault_secret.dialogporten_app_insights_connection_string` is created.
- `module.dis_aks_resources_multitenancy.azapi_resource.otel_collector` is updated in place, with `path` going from `./multitenancy` to `./multitenancy-dialogporten`.
- Nothing else changes apart from what the module bump brings.
- If the repository secret is missing, the plan stops at the variable validation.

- [ ] **Step 5: Commit and apply after review**

```bash
git add tf/dis-core-test/dis-core-at23-aks-rg .github/workflows/dis-core-at23-aks-rg.yaml
git commit -m "feat(dis-core-at23): send Dialogporten feature metrics to its Application Insights"
```

### Task A5: Verify in at23, then roll out

- [ ] **Step 1: Secret and pipeline are live**

Run:

```bash
kubectl -n monitoring get externalsecret dialogporten-app-insights-connstring-external-secret
kubectl -n monitoring get opentelemetrycollector otel -o jsonpath='{.spec.config.service.pipelines}' | grep -o 'logs/dialogporten-feature-metrics'
```

Expected: the ExternalSecret shows `SecretSynced` and `True`, and the pipeline name is printed.

- [ ] **Step 2: Records arrive, and the platform App Insights is unchanged**

Run the Task B4 Step 2 and Step 3 queries against `dp-be-test-applicationInsights`. Expected: the same results as described there.

In `dis-core-at23-products-ai` → Logs, run:

```kusto
traces
| where timestamp > ago(1h)
| extend EventIdNum = toint(extractjson("$.Id", tostring(customDimensions.EventId)))
| summarize warnings = countif(severityLevel >= 2),
            featureMetrics = countif(EventIdNum == 1000 and cloud_RoleName startswith "dialogporten-web-api")
```

Expected: `warnings` is above 0, and `featureMetrics` is 0.

- [ ] **Step 3: Roll out**

Do this for each environment below when Dialogporten is onboarded there, using the values in its row:

1. Run `jq -r '."otel-collector".<Ring>' oci/releaseconfig.json` in dis-way/gitops-manifests. It must return the Task A2 release or later.
2. Create the repository secret `<GitHub secret>` in dis-way/core, with the value from `<Connection string source>`.
3. Append the `variable "dialogporten_app_insights_connection_string"` block from Task A4 Step 2 to `<Stack>/variables.tf`.
4. Append the `azurerm_key_vault_secret` block from Task A4 Step 2 to `<Stack>/dis-monitoring.tf`, unchanged.
5. Add the `depends_on` entry and `otel_collector_path` from Task A4 Step 3 to `<Stack>/dis-aks-resources.tf`.
6. Add `TF_VAR_dialogporten_app_insights_connection_string: ${{ secrets.<GitHub secret> }}` to the `env:` block of `<Workflow>`.
7. Plan, review and apply as in Task A4 Steps 4 and 5.
8. Run Steps 1 and 2 of this task against that environment's App Insights.

| Env | Stack | Workflow | Ring in releaseconfig | GitHub secret | Connection string source |
|---|---|---|---|---|---|
| yt01 | `tf/dis-core-test/dis-core-yt01-aks-rg` | `.github/workflows/dis-core-yt01-aks-rg.yaml` | `at_ring2` | `DIALOGPORTEN_APPINSIGHTS_CONNECTION_STRING_YT01` | `az monitor app-insights component show -g dp-be-yt01-rg -a dp-be-yt01-applicationInsights --query connectionString -o tsv` |
| tt02 | `tf/dis-core-staging/dis-core-tt02-aks-rg` | `.github/workflows/dis-core-tt02-aks-rg.yaml` | `tt_ring2` | `DIALOGPORTEN_APPINSIGHTS_CONNECTION_STRING_TT02` | `az monitor app-insights component show -g dp-be-staging-rg -a dp-be-staging-applicationInsights --query connectionString -o tsv` |
| prod | `tf/dis-core-prod/dis-core-prod-aks-rg` | `.github/workflows/dis-core-prod-aks-rg.yaml` | `prod_ring2` | `DIALOGPORTEN_APPINSIGHTS_CONNECTION_STRING_PROD` | `az monitor app-insights component show -g dp-be-prod-rg -a dp-be-prod-applicationInsights --query connectionString -o tsv` |

In each stack's `variables.tf` description, replace `dp-be-test-applicationInsights` with that environment's App Insights.

---

## Appendix: local ingestion mock

This is a fake Application Insights ingestion endpoint (`/v2.1/track`). It prints what each port receives: 18081 stands in for the platform App Insights, and 18082 for Dialogporten's. Tasks A2 and B3 use it. Save both files in `/tmp/fm-mock`, then start the mock:

```bash
mkdir -p /tmp/fm-mock && cd /tmp/fm-mock   # save mock_ingestion.py and payload.json here first
python3 mock_ingestion.py > mock.log 2>&1 &
```

`/tmp/fm-mock/mock_ingestion.py`:

```python
import gzip, json, threading
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

def make_handler(label):
    class H(BaseHTTPRequestHandler):
        def do_POST(self):
            body = self.rfile.read(int(self.headers.get("Content-Length", 0)))
            if self.headers.get("Content-Encoding") == "gzip":
                body = gzip.decompress(body)
            items = [json.loads(l) for l in body.decode().splitlines() if l.strip()]
            for it in items:
                bd = it.get("data", {}).get("baseData", {})
                p = bd.get("properties", {})
                print(f"[{label}] {self.path} sev={bd.get('severityLevel')} msg={bd.get('message')!r} "
                      f"role={it.get('tags', {}).get('ai.cloud.role')} EventId={p.get('EventId')} "
                      f"AdditionalTags={p.get('AdditionalTags')} HasAdminScope={p.get('HasAdminScope')}", flush=True)
            resp = json.dumps({"itemsReceived": len(items), "itemsAccepted": len(items), "errors": []}).encode()
            self.send_response(200); self.send_header("Content-Type", "application/json")
            self.send_header("Content-Length", str(len(resp))); self.end_headers(); self.wfile.write(resp)
        def log_message(self, *a): pass
    return H

for label, port in (("platform-ai", 18081), ("dialogporten-ai", 18082)):
    s = ThreadingHTTPServer(("0.0.0.0", port), make_handler(label))
    threading.Thread(target=s.serve_forever, daemon=True).start()
threading.Event().wait()
```

`/tmp/fm-mock/payload.json` contains:
- one feature metric shaped like the WebApi's (Serilog's `EventId` and the `AdditionalTags` dictionary arrive as OTLP maps)
- a framework record at Information and one at Warning
- two records from another service

```json
{"resourceLogs":[
 {"resource":{"attributes":[{"key":"service.name","value":{"stringValue":"dialogporten-web-api-so"}},{"key":"k8s.pod.uid","value":{"stringValue":"0f6c5c1e"}}]},
  "scopeLogs":[
   {"scope":{"name":"Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric.LoggingFeatureMetricDeliveryContext"},
    "logRecords":[{"severityNumber":9,"severityText":"Information","body":{"stringValue":"DP-FEATURE-METRIC-INFO"},"attributes":[
      {"key":"FeatureType","value":{"stringValue":"Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get.GetDialogQuery"}},
      {"key":"HasAdminScope","value":{"boolValue":false}},
      {"key":"CallerOrg","value":{"stringValue":"digdir"}},
      {"key":"EventId","value":{"kvlistValue":{"values":[{"key":"Id","value":{"intValue":"1000"}},{"key":"Name","value":{"stringValue":"LogFeatureMetric"}}]}}},
      {"key":"AdditionalTags","value":{"kvlistValue":{"values":[{"key":"StatusCode","value":{"intValue":"200"}},{"key":"CorrelationId","value":{"stringValue":"0HN1"}},{"key":"Status","value":{"stringValue":"success"}}]}}}]}]},
   {"scope":{"name":"Microsoft.AspNetCore.Hosting.Diagnostics"},
    "logRecords":[{"severityNumber":9,"body":{"stringValue":"DP-FRAMEWORK-INFO"}},{"severityNumber":13,"body":{"stringValue":"DP-FRAMEWORK-WARN"}}]}]},
 {"resource":{"attributes":[{"key":"service.name","value":{"stringValue":"other-app"}}]},
  "scopeLogs":[{"scope":{"name":"Other.Category"},"logRecords":[{"severityNumber":9,"body":{"stringValue":"OTHER-INFO"}},{"severityNumber":13,"body":{"stringValue":"OTHER-WARN"}}]}]}
]}
```

## Out of scope and follow-ups

- **Alerting on the billing path.** Today a broken path only shows up as lower cost figures. A scheduled log alert on `dp-be-<env>-applicationInsights` would page instead. It should fire when requests flow but no feature metrics arrive, for example when `traces | where timestamp > ago(1h) | where toint(extractjson("$.Id", tostring(customDimensions.EventId))) == 1000 | count` is 0 while `requests` in the same window is not.
- **Running `aggregate-cost-metrics` on DIS.** The job runs on Container Apps and keeps doing so. Its DIS CronJob is not usable yet:
  - It is suspended in tt02 and prod, and deleted in at23 and yt01.
  - Its `${...}` placeholders are never substituted, because Flux does no post-build substitution in dialogporten-manifests.
  - The pod lacks `azure.workload.identity/use`.
  - Its identity has no role on `dp-be-<env>-applicationInsights`.

  This is only needed once Container Apps is switched off.
- **Moving feature metrics off logs** (Altinn/dialogporten#3123).
- **The WebApi's other Information category.** The WebApi also raises `...DialogSearch.EndUser` to Information, and those records are dropped on DIS as well.
