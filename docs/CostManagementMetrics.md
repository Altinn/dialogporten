# Cost Management Metrics

This document describes the implementation of cost management metrics in Dialogporten as specified in [Issue #2376](https://github.com/Altinn/dialogporten/issues/2376).

## Overview

The cost management metrics system tracks dialog transactions for billing and cost analysis purposes. It records metrics only for endpoints explicitly annotated with the `[CostTracked]` attribute, covering successful (2xx) and client error (4xx) responses for those opted-in endpoints; server errors (5xx) are excluded by design. It also records detailed metadata about the organization making the request and the service being accessed.

## Architecture

### Components

1. **`[CostTracked]` Attribute**: Custom attribute that marks endpoints for cost tracking and specifies the transaction type
2. **`CostManagementMiddleware`**: ASP.NET Core middleware that intercepts requests and queues transactions
3. **`ICostManagementMetricsService`**: Service interface for queueing transaction metrics  
4. **`CostManagementService`**: Service that handles queueing, background processing, and metrics recording
5. **`IApplicationContext`**: Scoped service for passing metadata from handlers to middleware
6. **`TransactionRecord`**: Record type for queuing transaction data
7. **`TransactionType`**: Enum defining all supported transaction types
8. **`CostManagementOptions`**: Configuration options for queue capacity and feature toggles
9. **`CostManagementConstants`**: Constants for metric names, tags, and status values
10. **`CostManagementMetadataKeys`**: Constants for IApplicationContext metadata keys

### Data Flow (Async Queue-Based)

```mermaid
flowchart TD
    A[HTTP Request] --> B[CostManagementMiddleware]
    B --> C[Extract TransactionType from CostTracked attribute]  
    C --> D[Extract tokenOrg from JWT claims]
    D --> E[Read serviceOrg and serviceResource from IApplicationContext]
    E --> F[Queue transaction via ICostManagementMetricsService]
    F --> G[Channel Queue]
    G --> H[CostManagementService Background Processing]
    H --> I[.NET Meter Counters]
    I --> J[OTEL Collector]
    J --> K[Prometheus/Azure Monitor]
```

## Transaction Types

The system tracks various transaction types defined in the `TransactionType` enum:

| Transaction Type | Description | Norwegian Term |
|------------------|-------------|----------------|
| `CreateDialog` | Create dialog operation | Opprette dialog |
| `UpdateDialog` | Update dialog operation | Oppdatere dialog |
| `SoftDeleteDialog` | Soft delete dialog operation | Softslette dialog |
| `HardDeleteDialog` | Hard delete/purge dialog operation | Hardslette dialog |
| `GetDialogServiceOwner` | Get dialog by service owner | Hente dialog tjenesteeier |
| `SearchDialogsServiceOwner` | Service owner search dialogs | Tjenesteeiersøk |
| `SearchDialogsServiceOwnerWithEndUser` | Service owner search dialogs with end user ID | Tjenesteeiersøk m/sluttbruker-id |
| `GetDialogEndUser` | Get dialog by end user | Hente dialog sluttbruker |
| `SetDialogLabel` | Set label on single dialog | Sette label på enkeltdialog |
| `BulkSetLabelsServiceOwnerWithEndUser` | Bulk set labels via service owner API with end user ID | Bulk label setting tjenesteeier m/sluttbruker-id |
| `SearchDialogsEndUser` | End user search dialogs | Sluttbrukersøk |
| `BulkSetLabelsEndUser` | Bulk set labels via end user API | Bulk label setting sluttbruker |

## Endpoint Annotation

Endpoints are marked for cost tracking using the `[CostTracked]` attribute:

```csharp
[CostTracked(TransactionType.CreateDialog)]
public class CreateDialogEndpoint : Endpoint<CreateDialogCommand, CreateDialogResult>
{
    // Endpoint implementation
}
```

## Metadata Structure

The system captures three key pieces of metadata for each transaction (emitted as metric tags `token_org`, `service_org`, and `service_resource`):

### 1. `tokenOrg` (Organization from JWT Token)
- **Source**: `"urn:altinn:org"` claim from the JWT token
- **Example**: `"digdir"`, `"skatteetaten"`
- **Purpose**: Identifies the organization making the API call

### 2. `serviceOrg` (Organization from Dialog Entity)
- **Source**: `dialog.Org` property from the dialog being operated on
- **Example**: `"digdir"`, `"skatteetaten"`
- **Purpose**: Identifies the organization that owns the service being accessed

### 3. `serviceResource` (Service Resource from Dialog Entity)
- **Source**: `dialog.ServiceResource` property from the dialog being operated on
- **Example**: `"skjema/NAV/123"`
- **Purpose**: Identifies the specific service resource being accessed

## Metadata Handling

### Handler Implementation

Handlers set metadata using `IApplicationContext`:

```csharp
public class CreateDialogCommandHandler : IRequestHandler<CreateDialogCommand, CreateDialogResult>
{
    private readonly IApplicationContext _applicationContext;
    
    public async Task<CreateDialogResult> Handle(CreateDialogCommand request, CancellationToken cancellationToken)
    {
        // ... business logic ...
        
        // Set metadata after successful operation
        _applicationContext.AddMetadata(CostManagementMetadataKeys.ServiceOrg, dialog.Org);
        _applicationContext.AddMetadata(CostManagementMetadataKeys.ServiceResource, dialog.ServiceResource);
        
        return result;
    }
}
```

### Special Cases

For operations where metadata cannot be meaningfully attributed:

```csharp
// Search operations affecting multiple entities
_applicationContext.AddMetadata(CostManagementMetadataKeys.ServiceOrg, CostManagementMetadataKeys.SearchOperation);
_applicationContext.AddMetadata(CostManagementMetadataKeys.ServiceResource, CostManagementMetadataKeys.SearchOperation);

// End user operations without organization context
_applicationContext.AddMetadata(CostManagementMetadataKeys.ServiceOrg, CostManagementMetadataKeys.NotApplicable);
_applicationContext.AddMetadata(CostManagementMetadataKeys.ServiceResource, CostManagementMetadataKeys.NotApplicable);

// Bulk operations across multiple organizations
_applicationContext.AddMetadata(CostManagementMetadataKeys.ServiceOrg, CostManagementMetadataKeys.BulkOperation);
_applicationContext.AddMetadata(CostManagementMetadataKeys.ServiceResource, CostManagementMetadataKeys.BulkOperation);
```

## Metrics Schema

### Counter Metric

- **Name**: `dialogporten_transactions_total`
- **Description**: Total number of dialog transactions for cost management
- **Type**: Counter (incremental)

### Tags

| Tag Name | Description | Example Values |
|----------|-------------|----------------|
| `transaction_type` | Type of transaction | `CreateDialog`, `GetDialogServiceOwner` |
| `status` | Operation result | `success` (2xx responses), `failed` (4xx responses) |
| `token_org` | Organization from JWT token | `"digdir"`, `"skatteetaten"` |
| `service_org` | Organization from dialog entity | `"digdir"`, `"skatteetaten"`, `"unknown"`, `"search_operation"`, `"bulk_operation"`, `"not_applicable"` |
| `service_resource` | Service resource from dialog entity | `"skjema/NAV/123"`, `"unknown"`, `"search_operation"`, `"bulk_operation"`, `"not_applicable"` |
| `http_status_code` | HTTP response status code | `200`, `201`, `400`, `404` |
| `environment` | Environment name | `Development`, `Test`, `Production` |

### System Monitoring Metrics

Additional metrics for operational monitoring:

| Metric Name | Type | Description | Tags |
|-------------|------|-------------|------|
| `dialogporten_cost_dropped_transactions_total` | Counter | Total transactions dropped due to queue overflow | `reason`, `transaction_type`, `status_class`, `service_org`, `token_org` |
| `dialogporten_cost_queue_depth` | Observable Gauge | Current number of transactions waiting in queue | |
| `dialogporten_cost_queue_capacity` | Observable Gauge | Maximum capacity of the cost management queue | |

#### Dropped Transaction Tags

The dropped transaction counter includes detailed tags for analysis:

- **`reason`**: `"enqueue_failed"` (queue full) or `"exception"` (processing error)
- **`transaction_type`**: Type of dropped transaction (e.g., `"CreateDialog"`)
- **`status_class`**: HTTP status class (e.g., `"2xx"`, `"4xx"`)
- **`service_org`**: Organization from dialog entity
- **`token_org`**: Organization from JWT token

## Implementation & Configuration

### Configuration Options

Cost management can be configured via `appsettings.json`:

```json
{
  "CostManagement": {
    "Enabled": true,
    "QueueCapacity": 100000
  }
}
```

### Configuration Properties

| Property | Description | Default | Recommended Values |
|----------|-------------|---------|-------------------|
| `Enabled` | Whether cost tracking is enabled | `true` | `false` for development, `true` for production |
| `QueueCapacity` | Maximum queued transactions | `100,000` | Dev: 1,000; Test: 10,000; Prod: 100,000-500,000 |

### Queue Capacity Planning

For **high-traffic scenarios** (tax returns, deadlines):

- **5M users, 3-5 API calls each** = 15-25M transactions
- **Peak window: 2-3 hours** = ~2,000-4,000 TPS sustained  
- **Burst capacity needed**: 10,000+ TPS for spikes
- **Queue buffer recommendations**:
  - **100,000 capacity** ≈ 25 seconds at 4,000 TPS
  - **500,000 capacity** ≈ 2 minutes at 4,000 TPS
  - **Memory impact**: ~200MB for 1M capacity (acceptable)

### Service Registration

In `Program.cs`:

```csharp
// Register and validate CostManagement options (must be before AddCostManagementMetrics)
builder.Services
    .AddOptions<CostManagementOptions>()
    .Bind(builder.Configuration.GetSection(CostManagementOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Register services with configuration
builder.Services.AddScoped<IApplicationContext, ApplicationContext>();
builder.Services.AddCostManagementMetrics();

// Add middleware
app.UseCostManagementMetrics();
```

### OpenTelemetry Integration

The cost management meter is automatically registered:

```csharp
metrics.AddMeter("Dialogporten.CostManagement");
```

### Endpoint Metadata

The middleware reads endpoint metadata to determine transaction types:

```csharp
// Get the endpoint type metadata (FastEndpoints pattern)
var endpoint = context.GetEndpoint();
var endpointTypeMetadata = endpoint?.Metadata.GetMetadata<EndpointTypeMetadata>();

// Use reflection to find the CostTrackedAttribute on the endpoint class
var costTrackedAttr = endpointTypeMetadata?.EndpointType?
    .GetCustomAttribute<CostTrackedAttribute>();

// Determine transaction type (supports case-insensitive query parameter variants)
if (costTrackedAttr.HasVariant &&
    costTrackedAttr.QueryParameterVariant != null &&
    context.Request.Query.Keys.Any(key => 
        string.Equals(key, costTrackedAttr.QueryParameterVariant, StringComparison.OrdinalIgnoreCase)))
{
    return costTrackedAttr.VariantTransactionType;
}
```

### JWT Claims Extraction

Organization information is extracted from JWT claims:

```csharp
private static string? ExtractTokenOrg(HttpContext context)
{
    var user = context.RequestServices.GetRequiredService<IUser>();
    var principal = user.GetPrincipal();
    if (principal.TryGetOrganizationShortName(out var orgShortName))
    {
        return orgShortName;
    }
    return null;
}
```
