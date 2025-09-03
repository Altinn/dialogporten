# Dialogporten - Dependencies Overview

This documentation provides a complete overview of all dependencies that Dialogporten has, both internal and external, to ensure effective risk management and change management.

*Last updated: 2025-09-03 13:32*

## 📋 Table of Contents

- [.NET Runtime and SDK](#net-runtime-and-sdk)
- [NuGet Packages](#nuget-packages)
- [External Services](#external-services)
- [Azure Infrastructure](#azure-infrastructure)
- [Development Tools](#development-tools)
- [Container Dependencies](#container-dependencies)
- [Automated Tracking](#automated-tracking)

## 🔧 .NET Runtime and SDK

| Component | Version | Comment |
|-----------|---------|---------|
| .NET SDK | 9.0.304 | Defined in global.json |
| Target Framework | net9.0 | All projects |
| Roll Forward | disable | May affect CI/CD |

## 📦 NuGet Packages

*Note: See [Auto-generated Dependencies](#auto-generated-dependencies) section below for current versions*

### Core Components

| Package | Used in | Support |
|---------|---------|---------|
| **Microsoft.AspNetCore.*** | WebApi, GraphQL | Microsoft LTS |
| **Microsoft.EntityFrameworkCore.*** | Infrastructure | Microsoft LTS |
| **Npgsql.EntityFrameworkCore.PostgreSQL** | Infrastructure | Community |
| **Npgsql** | Infrastructure | Community |

### Security and Authentication

| Package | Used in | Comment |
|---------|---------|---------|
| **Microsoft.AspNetCore.Authentication.JwtBearer** | WebApi, GraphQL | OAuth2/OIDC |
| **Azure.Identity** | WebApi, Janitor | Managed Identity |
| **Altinn.Authorization.ABAC** | Infrastructure | Altinn-specific |
| **Altinn.ApiClients.Maskinporten** | Infrastructure | Maskinporten integration |

### Logging and Monitoring

| Package | Used in | Comment |
|---------|---------|---------|
| **Serilog.AspNetCore** | All | Structured logging |
| **Serilog.Sinks.OpenTelemetry** | All | Telemetry |
| **Serilog.Enrichers.Environment** | All | Environment info |

### Caching and Messaging

| Package | Used in | Comment |
|---------|---------|---------|
| **ZiggyCreatures.FusionCache** | Infrastructure | Multi-layer cache |
| **ZiggyCreatures.FusionCache.Backplane.StackExchangeRedis** | Infrastructure | Redis backplane |
| **Microsoft.Extensions.Caching.StackExchangeRedis** | Infrastructure | Redis cache |
| **MassTransit.Azure.ServiceBus.Core** | Infrastructure | Message bus |
| **MassTransit.EntityFrameworkCore** | Infrastructure | Saga persistence |

### GraphQL

| Package | Used in | Comment |
|---------|---------|---------|
| **HotChocolate.Subscriptions.Redis** | Infrastructure | GraphQL subscriptions |

### API and HTTP

| Package | Used in | Comment |
|---------|---------|---------|
| **FastEndpoints.Swagger** | WebApi | API documentation |
| **NSwag.MSBuild** | WebApi | Build-time only |
| **Microsoft.Extensions.Http.Polly** | Infrastructure | HTTP resilience |
| **Polly.Contrib.WaitAndRetry** | Infrastructure | Retry policies |

### Validation and Serialization

| Package | Used in | Comment |
|---------|---------|---------|
| **Microsoft.AspNetCore.Mvc.NewtonsoftJson** | WebApi | JSON serialization |
| **ZiggyCreatures.FusionCache.Serialization.NeueccMessagePack** | Infrastructure | Cache serialization |

### Error and Exception Handling

| Package | Used in | Comment |
|---------|---------|---------|
| **EntityFrameworkCore.Exceptions.PostgreSQL** | Infrastructure | DB exception mapping |

### Health Checks

| Package | Used in | Comment |
|---------|---------|---------|
| **Microsoft.Extensions.Diagnostics.HealthChecks** | Infrastructure | Health monitoring |
| **Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore** | Infrastructure | EF health checks |

### Configuration Management

| Package | Used in | Comment |
|---------|---------|---------|
| **Microsoft.Azure.AppConfiguration.AspNetCore** | WebApi | Azure App Config |
| **Microsoft.Extensions.Configuration.UserSecrets** | Infrastructure | Development secrets |

### Tools and Development Support

| Package | Used in | Comment |
|---------|---------|---------|
| **Microsoft.EntityFrameworkCore.Tools** | Infrastructure | EF migrations |
| **Microsoft.Extensions.Hosting.Abstractions** | Infrastructure | Hosting model |

## 🌐 External Services

### Altinn Platform

| Service | Environment | Base URL | Contact |
|---------|-------------|----------|---------|
| **Altinn Platform** | Prod | https://platform.altinn.no/ | Digdir |
| **Altinn Platform** | Test (TT02) | https://platform.tt02.altinn.no/ | Digdir |
| **Altinn Platform** | AT23 | https://platform.at23.altinn.cloud/ | Digdir |
| **Altinn Events** | Prod | https://platform.altinn.no/ | Digdir |
| **Altinn CDN** | All | https://altinncdn.no/ | Digdir |

### Maskinporten

| Service | Environment | Base URL | Contact |
|---------|-------------|----------|---------|
| **Maskinporten** | Prod | https://maskinporten.no/ | Digdir |
| **Maskinporten** | Test | https://test.maskinporten.no/ | Digdir |

### ID-porten

| Service | Environment | Base URL | Contact |
|---------|-------------|----------|---------|
| **ID-porten** | Prod | https://idporten.no/ | Digdir |
| **ID-porten** | Test | https://test.idporten.no/ | Digdir |

## ☁️ Azure Infrastructure

### Core Services

| Service | SKU/Tier | Environment | Comment |
|---------|-----------|-------------|---------|
| **Azure Container Apps** | Consumption/Dedicated-D8 | Prod, YT01 | Hosting platform |
| **Azure Container Apps** | Consumption | Test, Staging | Hosting platform |
| **Azure PostgreSQL** | Standard_D8ads_v5 | Prod, YT01 | Primary database |
| **Azure PostgreSQL** | Standard_D4ads_v5 | Staging | Primary database |
| **Azure PostgreSQL** | Standard_B2s (Burstable) | Test | Primary database |
| **Azure Redis Cache** | Basic C1 | All | Cache and sessions |
| **Azure Service Bus** | Premium | All | Message queue |
| **Azure Key Vault** | Standard | All | Secret management |
| **Azure App Configuration** | Standard | All | Configuration |
| **Azure Application Insights** | PerGB2018 | All | Telemetry |

### Network and Security

| Service | Configuration | Environments | Comment |
|---------|---------------|--------------|---------|
| **Virtual Network** | 10.0.0.0/16 | All | Network isolation |
| **Private Endpoints** | All PaaS services | All | Security |
| **Private DNS Zones** | Service-specific | All | Name resolution |
| **SSH Jumper** | Standard_B1s | All | Debug access |

### Backup and Security

| Service | Configuration | Environments | Comment |
|---------|---------------|--------------|---------|
| **PostgreSQL Backup** | 7-32 days | All | Data protection |
| **Long-term Backup** | 12 months | Prod | Compliance |
| **Managed Identity** | System-assigned | All | Authentication |

## 🛠️ Development Tools

### CI/CD Pipeline

| Tool | Version/Config | Comment |
|------|----------------|---------|
| **GitHub Actions** | SHA-pinned versions | Deployment pipeline |
| **Docker** | Multi-stage builds | Containerization |
| **Bicep** | v2.2.0 (GitHub Action) | Infrastructure as Code |

### Local Development

| Tool | Version | Comment |
|------|---------|---------|
| **Docker Compose** | v2+ | Local development |
| **PostgreSQL** | 16.8 | Local database |
| **Redis** | 6.0-alpine | Local cache |
| **Nginx** | 1.29.1 | Local ingress |

## 🐳 Container Dependencies

### Observability Stack (Local Development)

| Container | Image | Version | Port |
|-----------|-------|---------|------|
| **OpenTelemetry Collector** | otel/opentelemetry-collector-contrib | 0.131.1 | 4317/4318 |
| **Jaeger** | jaegertracing/all-in-one | 1.72.0 | 16686 |
| **Prometheus** | prom/prometheus | v3.5.0 | 9090 |
| **Loki** | grafana/loki | 3.5.3 | 3100 |
| **Grafana** | grafana/grafana | 11.6.3 | 3000 |
| **RedisInsight** | redis/redisinsight | latest | 7216 |

## 🔄 Automated Tracking

### Implemented Mechanisms

1. **NuGet Package Tracking**
   - All packages defined in .csproj files
   - Version management via Directory.Build.props
   - Renovate Bot for automatic updates

2. **Container Image Tracking**
   - Docker images defined in docker-compose files
   - Version locking for all production images
   - Renovate Bot for updates

3. **Infrastructure Tracking**
   - Azure resources defined in Bicep templates
   - Version management of SKUs and configurations
   - Environment-specific parameter files

### Recommended Improvements

1. **SBOM (Software Bill of Materials)**
   - Implement automatic SBOM generation
   - Integrate with security scanning
   - Store SBOM as build artifacts

2. **Dependency Vulnerability Scanning**
   - GitHub Dependabot already active
   - Consider Snyk or WhiteSource integration
   - Automatic security scanning in pipeline

3. **License Compliance**
   - Implement license scanning
   - Document approved licenses
   - Automatic alerts for new licenses

## 📞 Contact Information

### Internal Teams
- **Dialogporten Team**: @elsand, @arealmaas
- **Infrastructure**: Altinn Platform Team
- **Security**: Digdir Security Team

### External Vendors
- **Microsoft Azure**: Support via Azure Portal
- **Altinn**: platform@altinn.no
- **Digdir**: Maskinporten/ID-porten support

---

## 🤖 Auto-generated Dependencies

*This section is automatically updated by Update-Dependencies.ps1*

### Automatically Updated NuGet Overview

*Generated automatically from all .csproj files*

| Package | Version |
|---------|---------|
| **Altinn.ApiClients.Maskinporten** | 9.2.1 |
| **Altinn.Authorization.ABAC** | 0.0.8 |
| **AppAny.HotChocolate.FluentValidation** | 0.12.0 |
| **AspNetCore.HealthChecks.UI.Client** | 9.0.0 |
| **AutoMapper** | 14.0.0 |
| **Azure.Identity** | 1.15.0 |
| **Azure.Monitor.OpenTelemetry.AspNetCore** | 1.2.0 |
| **Bogus** | 35.6.3 |
| **Cocona** | 2.2.0 |
| **CommandLineParser** | 2.9.1 |
| **coverlet.collector** | 6.0.4 |
| **EntityFrameworkCore.Exceptions.PostgreSQL** | 8.1.3 |
| **FastEndpoints.Swagger** | 5.35.0 |
| **FluentAssertions** | 7.2.0 |
| **FluentValidation.DependencyInjectionExtensions** | 12.0.0 |
| **HotChocolate.AspNetCore** | 15.1.8 |
| **HotChocolate.AspNetCore.Authorization** | 15.1.8 |
| **HotChocolate.Data.EntityFramework** | 15.1.8 |
| **HotChocolate.Diagnostics** | 15.1.8 |
| **HotChocolate.Subscriptions.Redis** | 15.1.8 |
| **HtmlAgilityPack** | 1.12.2 |
| **MassTransit.Azure.ServiceBus.Core** | 8.5.2 |
| **MassTransit.EntityFrameworkCore** | 8.5.2 |
| **MediatR** | 12.5.0 |
| **MediatR.Contracts** | 2.0.1 |
| **Medo.Uuid7** | 3.1.0 |
| **Microsoft.ApplicationInsights.AspNetCore** | 2.23.0 |
| **Microsoft.AspNetCore.Authentication.JwtBearer** | 9.0.8 |
| **Microsoft.AspNetCore.Mvc.NewtonsoftJson** | 9.0.8 |
| **Microsoft.AspNetCore.Mvc.Testing** | 9.0.8 |
| **Microsoft.AspNetCore.OpenApi** | 9.0.8 |
| **Microsoft.Azure.AppConfiguration.AspNetCore** | 8.3.0 |
| **Microsoft.Build** | 17.14.8 |
| **Microsoft.DotNet.ILCompiler** | 9.0.8 |
| **Microsoft.EntityFrameworkCore.Relational** | 9.0.8 |
| **Microsoft.EntityFrameworkCore.Tools** | 9.0.8 |
| **Microsoft.Extensions.Caching.StackExchangeRedis** | 9.0.8 |
| **Microsoft.Extensions.Configuration** | 9.0.8 |
| **Microsoft.Extensions.Configuration.EnvironmentVariables** | 9.0.8 |
| **Microsoft.Extensions.Configuration.Json** | 9.0.8 |
| **Microsoft.Extensions.Configuration.UserSecrets** | 9.0.8 |
| **Microsoft.Extensions.DependencyInjection** | 9.0.8 |
| **Microsoft.Extensions.DependencyInjection.Abstractions** | 9.0.8 |
| **Microsoft.Extensions.Diagnostics.HealthChecks** | 9.0.8 |
| **Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore** | 9.0.8 |
| **Microsoft.Extensions.Hosting** | 9.0.8 |
| **Microsoft.Extensions.Hosting.Abstractions** | 9.0.8 |
| **Microsoft.Extensions.Http** | 9.0.8 |
| **Microsoft.Extensions.Http.Polly** | 9.0.8 |
| **Microsoft.Extensions.Options.ConfigurationExtensions** | 9.0.8 |
| **Microsoft.NET.ILLink.Tasks** | 9.0.8 |
| **Microsoft.NET.Test.Sdk** | 17.14.1 |
| **NetArchTest.Rules** | 1.3.2 |
| **Npgsql** | 9.0.3 |
| **Npgsql.EntityFrameworkCore.PostgreSQL** | 9.0.4 |
| **Npgsql.OpenTelemetry** | 9.0.3 |
| **NSec.Cryptography** | 25.4.0 |
| **NSubstitute** | 5.3.0 |
| **NSwag.MSBuild** | 14.2.0 |
| **OneOf** | 3.0.271 |
| **OneOf.SourceGenerator** | 3.0.271 |
| **OpenTelemetry** | 1.10.0 |
| **OpenTelemetry.Exporter.OpenTelemetryProtocol** | 1.10.0 |
| **OpenTelemetry.Instrumentation.Runtime** | 1.10.0 |
| **Polly.Contrib.WaitAndRetry** | 1.1.1 |
| **Refit** | 8.0.0 |
| **Refit.HttpClientFactory** | 8.0.0 |
| **Respawn** | 6.2.1 |
| **Scrutor** | 6.1.0 |
| **Serilog.AspNetCore** | 9.0.0 |
| **Serilog.Enrichers.Environment** | 3.0.1 |
| **Serilog.Extensions.Hosting** | 9.0.0 |
| **Serilog.Settings.Configuration** | 9.0.0 |
| **Serilog.Sinks.ApplicationInsights** | 4.0.0 |
| **Serilog.Sinks.Console** | 6.0.0 |
| **Serilog.Sinks.OpenTelemetry** | 4.2.0 |
| **Testcontainers.PostgreSql** | 4.6.0 |
| **UUIDNext** | 4.1.2 |
| **Verify.Xunit** | 30.7.3 |
| **xunit** | 2.9.3 |
| **xunit.runner.visualstudio** | 3.1.4 |
| **ZiggyCreatures.FusionCache** | 2.4.0 |
| **ZiggyCreatures.FusionCache.Backplane.StackExchangeRedis** | 2.4.0 |
| **ZiggyCreatures.FusionCache.OpenTelemetry** | 2.4.0 |
| **ZiggyCreatures.FusionCache.Serialization.NeueccMessagePack** | 2.4.0 |


### Automatically Updated Docker Overview

*Generated from docker-compose files*

| Image | Version | File |
|-------|---------|------|
| **grafana/grafana** | 11.6.3 | docker-compose-otel.yml |
| **grafana/loki** | 3.5.3 | docker-compose-otel.yml |
| **jaegertracing/all-in-one** | 1.72.0 | docker-compose-otel.yml |
| **nginx** | 1.29.1 | docker-compose.yml |
| **otel/opentelemetry-collector-contrib** | 0.133.0 | docker-compose-otel.yml |
| **postgres** | 16.9 | docker-compose-db-redis.yml |
| **prom/prometheus** | v3.5.0 | docker-compose-otel.yml |
| **redis** | 6.0-alpine | docker-compose-db-redis.yml |
| **redis/redisinsight** | latest | docker-compose-db-redis.yml |

