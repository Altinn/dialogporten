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

