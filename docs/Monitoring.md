# Monitoring

## Overview Dashboard

Dialogporten's monitoring dashboards are hosted in Grafana and provide comprehensive insights into system performance, health metrics, and operational status. The dashboards are accessible at [Grafana Altinn Cloud](https://grafana.altinn.cloud/dashboards/f/ce99lm57b1gcgd/).

### Main Metrics
- **System Health**: Availability, request stats, latency
- **Container Apps**: CPU, memory, requests (GraphQL, Web APIs)
- **Infrastructure**: PostgreSQL, Redis, Service Bus status

### Usage
- Select environment (test, yt01, staging, prod)
- Default view: Last 24 hours
- Start with system health, then drill down as needed

## Telemetry Collection

Dialogporten uses OpenTelemetry for collecting and routing telemetry data:

### OpenTelemetry Integration
- Utilizes Azure Container Apps' managed OpenTelemetry agent
- Automatically collects traces and logs from container apps
- Routes telemetry data to Azure Application Insights
- Configured through Container Apps Environment settings

### Data Flow
1. Applications emit OpenTelemetry-compliant telemetry
2. Container Apps OpenTelemetry agent collects the data
3. Data is sent to Azure Application Insights
4. Grafana visualizes the data through Azure Monitor data source

### Implementation Details
- Traces and logs are configured to use Application Insights as destination
- Uses standard OpenTelemetry instrumentation for .NET
- Automatic correlation of distributed traces across services
- Custom metrics and traces can be added through the OpenTelemetry SDK

### Production ingestion transformations

Production's Log Analytics workspace uses the `FIlter-GraphQL-Subscriptions` workspace transformation DCR. Its existing name and destination alias are preserved in [prod.bicepparam](../.azure/infrastructure/prod.bicepparam), and its KQL is stored in [.azure/infrastructure/workspaceTransforms/prod](../.azure/infrastructure/workspaceTransforms/prod):

- `AppDependencies`: drops PostgreSQL dependencies shorter than 100 ms and dependency types other than `postgresql` or `Other`. For eligible HTTP dependencies, adds the URL path to the name, except for the resource-policy sync job to limit cardinality.
- `AppRequests`: drops requests from the Altinn production API role, GraphQL transport/subscription requests, and health checks.
- `AppExceptions`: drops exceptions from the Altinn production API role.
- `AppTraces`: drops command and batch completion messages.

These are the existing production filters; other environments leave `appInsightsWorkspaceTransform` unset and do not deploy a DCR. Change the checked-in KQL to update ingestion behavior, since infrastructure deployments overwrite edits made directly in Azure.

The [Application Insights module](../.azure/modules/applicationInsights/create.bicep) creates the workspace before deploying the DCR, then links it through `defaultDataCollectionRuleResourceId`. The linking update reuses the workspace's managed properties to preserve its retention, SKU, quota and purge settings. This order follows Azure's [workspace transformation setup](https://learn.microsoft.com/en-us/azure/azure-monitor/logs/tutorial-workspace-transformations-api).

Before deployment, compile `.azure/infrastructure/main.bicep` with `az bicep build` and inspect the production infrastructure dry run. Adoption should retain the existing DCR, destination, four transforms and workspace settings, with the standard resource tags added to the DCR. No manual deletion or relinking of the production DCR is needed.

## Redis Dashboard

Detailed monitoring of Redis cache performance and health:

### Key Metrics
- **Memory Usage**: Total and percentage used memory
- **Operations**: Commands executed, cache hits/misses
- **Keys**: Total keys, expired vs evicted keys
- **Connections**: Connected clients, server load
- **Performance**: Cache hit ratio, command processing rate

### Usage
- Select subscription, environment, and Redis resource
- Default view: Last 24 hours
- Refresh interval: 30 seconds

## Container Apps Dashboard

Monitoring of Azure Container Apps deployments and performance:

### Key Metrics
- **System Logs**: Container app system events and logs
- **Application Logs**: Service-specific application traces
- **Deployment Status**: Revision tracking and deployment logs

### Usage
- Filter by service name and revision
- View logs by deployment or system events
- Track service-specific metrics and traces

## Service Bus Dashboard

Azure Service Bus monitoring for message processing:

### Key Metrics
- **Queue/Topic Health**: Message counts, processing rates
- **Resource Usage**: Namespace metrics
- **Performance**: Throughput, latency, request rates

### Usage
- Select namespace and queue/topic
- Monitor message processing status
- Track service bus resource utilization

## PostgreSQL Dashboard

Azure Database for PostgreSQL Flexible Server monitoring:

### Key Metrics
- **Server Health**: CPU, memory, IOPS
- **Database Performance**: Connections, throughput
- **Storage**: Usage and performance metrics
- **Latency**: Query response times

### Usage
- Select server instance and database
- Monitor resource utilization
- Track query performance and connections
