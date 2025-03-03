# Monitoring

## Overview Dashboard

Quick monitoring dashboard for Dialogporten services showing:

### Main Metrics
- **System Health**: Availability, request stats, latency
- **Container Apps**: CPU, memory, requests (GraphQL, Web APIs)
- **Infrastructure**: PostgreSQL, Redis, Service Bus status

### Usage
- Select environment (test, yt01, staging, prod)
- Default view: Last 24 hours
- Start with system health, then drill down as needed

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

