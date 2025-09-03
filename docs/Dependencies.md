# Dialogporten - Avhengighetsoversikt

Denne dokumentasjonen gir en komplett oversikt over alle avhengigheter Dialogporten har, både interne og eksterne, for å sikre effektiv risikostyring og endringshåndtering.

*Sist oppdatert: 2025-09-03 09:21*

## 📋 Innholdsfortegnelse

- [.NET Runtime og SDK](#net-runtime-og-sdk)
- [NuGet-pakker](#nuget-pakker)
- [Eksterne tjenester](#eksterne-tjenester)
- [Azure-infrastruktur](#azure-infrastruktur)
- [Utviklingsverktøy](#utviklingsverktøy)
- [Container-avhengigheter](#container-avhengigheter)
- [Automatisert sporing](#automatisert-sporing)

## 🔧 .NET Runtime og SDK

| Komponent | Versjon | Kritisk | Kommentar |
|-----------|---------|---------|-----------|
| .NET SDK | 9.0.304 | ✅ Ja | Definert i global.json |
| Target Framework | net9.0 | ✅ Ja | Alle prosjekter |
| Roll Forward | disable | ⚠️ Nei | Kan påvirke CI/CD |

## 📦 NuGet-pakker

### Kjernekomponenter

| Pakke | Versjon | Brukes i | Kritisk | SLA/Support |
|-------|---------|----------|---------|-------------|
| **Microsoft.AspNetCore.***| 9.0.8 | WebApi, GraphQL | ✅ Ja | Microsoft LTS |
| **Microsoft.EntityFrameworkCore.***| 9.0.8 | Infrastructure | ✅ Ja | Microsoft LTS |
| **Npgsql.EntityFrameworkCore.PostgreSQL** | 9.0.4 | Infrastructure | ✅ Ja | Community |
| **Npgsql** | 9.0.3 | Infrastructure | ✅ Ja | Community |

### Sikkerhet og autentisering

| Pakke | Versjon | Brukes i | Kritisk | Kommentar |
|-------|---------|----------|---------|-----------|
| **Microsoft.AspNetCore.Authentication.JwtBearer** | 9.0.8 | WebApi, GraphQL | ✅ Ja | OAuth2/OIDC |
| **Azure.Identity** | 1.15.0 | WebApi, Janitor | ✅ Ja | Managed Identity |
| **Altinn.Authorization.ABAC** | 0.0.8 | Infrastructure | ✅ Ja | Altinn-spesifikk |
| **Altinn.ApiClients.Maskinporten** | 9.2.1 | Infrastructure | ✅ Ja | Maskinporten-integrasjon |

### Logging og monitorering

| Pakke | Versjon | Brukes i | Kritisk | Kommentar |
|-------|---------|----------|---------|-----------|
| **Serilog.AspNetCore** | 9.0.0 | Alle | ✅ Ja | Strukturert logging |
| **Serilog.Sinks.OpenTelemetry** | 4.2.0 | Alle | ✅ Ja | Telemetri |
| **Serilog.Enrichers.Environment** | 3.0.1 | Alle | ⚠️ Nei | Miljøinfo |

### Caching og messaging

| Pakke | Versjon | Brukes i | Kritisk | Kommentar |
|-------|---------|----------|---------|-----------|
| **ZiggyCreatures.FusionCache** | 2.4.0 | Infrastructure | ✅ Ja | Multi-layer cache |
| **ZiggyCreatures.FusionCache.Backplane.StackExchangeRedis** | 2.4.0 | Infrastructure | ✅ Ja | Redis backplane |
| **Microsoft.Extensions.Caching.StackExchangeRedis** | 9.0.8 | Infrastructure | ✅ Ja | Redis cache |
| **MassTransit.Azure.ServiceBus.Core** | 8.5.2 | Infrastructure | ✅ Ja | Message bus |
| **MassTransit.EntityFrameworkCore** | 8.5.2 | Infrastructure | ✅ Ja | Saga persistence |

### GraphQL

| Pakke | Versjon | Brukes i | Kritisk | Kommentar |
|-------|---------|----------|---------|-----------|
| **HotChocolate.Subscriptions.Redis** | 15.1.8 | GraphQL | ✅ Ja | GraphQL subscriptions |

### API og HTTP

| Pakke | Versjon | Brukes i | Kritisk | Kommentar |
|-------|---------|----------|---------|-----------|
| **FastEndpoints.Swagger** | 5.35.0 | WebApi | ✅ Ja | API documentation |
| **NSwag.MSBuild** | 14.2.0 | WebApi | ⚠️ Nei | Build-time only |
| **Microsoft.Extensions.Http.Polly** | 9.0.8 | Infrastructure | ✅ Ja | HTTP resilience |
| **Polly.Contrib.WaitAndRetry** | 1.1.1 | Infrastructure | ⚠️ Nei | Retry policies |

### Validering og serialisering

| Pakke | Versjon | Brukes i | Kritisk | Kommentar |
|-------|---------|----------|---------|-----------|
| **Microsoft.AspNetCore.Mvc.NewtonsoftJson** | 9.0.8 | WebApi | ✅ Ja | JSON serialization |
| **ZiggyCreatures.FusionCache.Serialization.NeueccMessagePack** | 2.4.0 | Infrastructure | ⚠️ Nei | Cache serialization |

### Feil- og eksepsjonshåndtering

| Pakke | Versjon | Brukes i | Kritisk | Kommentar |
|-------|---------|----------|---------|-----------|
| **EntityFrameworkCore.Exceptions.PostgreSQL** | 8.1.3 | Infrastructure | ⚠️ Nei | DB exception mapping |

### Helsesjekker

| Pakke | Versjon | Brukes i | Kritisk | Kommentar |
|-------|---------|----------|---------|-----------|
| **Microsoft.Extensions.Diagnostics.HealthChecks** | 9.0.8 | Infrastructure | ✅ Ja | Health monitoring |
| **Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore** | 9.0.8 | Infrastructure | ✅ Ja | EF health checks |

### Konfigurasjonsadministrasjon

| Pakke | Versjon | Brukes i | Kritisk | Kommentar |
|-------|---------|----------|---------|-----------|
| **Microsoft.Azure.AppConfiguration.AspNetCore** | 8.3.0 | WebApi | ⚠️ Nei | Azure App Config |
| **Microsoft.Extensions.Configuration.UserSecrets** | 9.0.8 | Infrastructure | ⚠️ Nei | Development secrets |

### Verktøy og utviklingsstøtte

| Pakke | Versjon | Brukes i | Kritisk | Kommentar |
|-------|---------|----------|---------|-----------|
| **Microsoft.EntityFrameworkCore.Tools** | 9.0.8 | Infrastructure | ⚠️ Nei | EF migrations |
| **Microsoft.Extensions.Hosting.Abstractions** | 9.0.8 | Infrastructure | ✅ Ja | Hosting model |

## 🌐 Eksterne tjenester

### Altinn-plattformen

| Tjeneste | Miljø | Baseurl | Kritisk | SLA | Kontakt |
|----------|-------|---------|---------|-----|---------|
| **Altinn Platform** | Prod | https://platform.altinn.no/ | ✅ Ja | 99.5% | Altinn |
| **Altinn Platform** | Test (TT02) | https://platform.tt02.altinn.no/ | ⚠️ Nei | Ikke garantert | Altinn |
| **Altinn Platform** | AT23 | https://platform.at23.altinn.cloud/ | ⚠️ Nei | Ikke garantert | Altinn |
| **Altinn Events** | Prod | https://platform.altinn.no/ | ✅ Ja | 99.5% | Altinn |
| **Altinn CDN** | Alle | https://altinncdn.no/ | ⚠️ Nei | Ikke spesifisert | Altinn |

### Maskinporten

| Tjeneste | Miljø | Baseurl | Kritisk | SLA | Kontakt |
|----------|-------|---------|---------|-----|---------|
| **Maskinporten** | Prod | https://maskinporten.no/ | ✅ Ja | 99.5% | Digdir |
| **Maskinporten** | Test | https://test.maskinporten.no/ | ⚠️ Nei | Ikke garantert | Digdir |

### ID-porten

| Tjeneste | Miljø | Baseurl | Kritisk | SLA | Kontakt |
|----------|-------|---------|---------|-----|---------|
| **ID-porten** | Prod | https://idporten.no/ | ✅ Ja | 99.5% | Digdir |
| **ID-porten** | Test | https://test.idporten.no/ | ⚠️ Nei | Ikke garantert | Digdir |

## ☁️ Azure-infrastruktur

### Kjernetjenester

| Tjeneste | SKU/Tier | Miljøer | Kritisk | SLA | Kommentar |
|----------|----------|---------|---------|-----|-----------|
| **Azure Container Apps** | Consumption/Dedicated-D8 | Alle | ✅ Ja | 99.95% | Hosting platform |
| **Azure PostgreSQL** | Standard_D8ads_v5 | Alle | ✅ Ja | 99.99% | Primær database |
| **Azure Redis Cache** | Basic C1 | Alle | ✅ Ja | 99.9% | Cache og sessions |
| **Azure Service Bus** | Premium | Alle | ✅ Ja | 99.9% | Message queue |
| **Azure Key Vault** | Standard | Alle | ✅ Ja | 99.9% | Secret management |
| **Azure App Configuration** | Standard | Alle | ⚠️ Nei | 99.9% | Configuration |
| **Azure Application Insights** | PerGB2018 | Alle | ✅ Ja | 99.9% | Telemetri |

### Nettverk og sikkerhet

| Tjeneste | Konfigurasjon | Miljøer | Kritisk | Kommentar |
|----------|---------------|---------|---------|-----------|
| **Virtual Network** | 10.0.0.0/16 | Alle | ✅ Ja | Network isolation |
| **Private Endpoints** | Alle PaaS-tjenester | Alle | ✅ Ja | Security |
| **Private DNS Zones** | Service-specific | Alle | ✅ Ja | Name resolution |
| **SSH Jumper** | Standard_B1s | Alle | ⚠️ Nei | Debug access |

### Backup og sikkerhet

| Tjeneste | Konfigurasjon | Miljøer | Kritisk | Kommentar |
|----------|---------------|---------|---------|-----------|
| **PostgreSQL Backup** | 7-32 dager | Alle | ✅ Ja | Data protection |
| **Long-term Backup** | 12 måneder | Prod | ✅ Ja | Compliance |
| **Managed Identity** | System-assigned | Alle | ✅ Ja | Authentication |

## 🛠️ Utviklingsverktøy

### CI/CD Pipeline

| Verktøy | Versjon/Config | Kritisk | Kommentar |
|---------|----------------|---------|-----------|
| **GitHub Actions** | Latest | ✅ Ja | Deployment pipeline |
| **Docker** | Multi-stage builds | ✅ Ja | Containerization |
| **Bicep** | Latest | ✅ Ja | Infrastructure as Code |

### Lokal utvikling

| Verktøy | Versjon | Kritisk | Kommentar |
|---------|---------|---------|-----------|
| **Docker Compose** | v2+ | ✅ Ja | Local development |
| **PostgreSQL** | 16.8 | ✅ Ja | Local database |
| **Redis** | 6.0-alpine | ✅ Ja | Local cache |
| **Nginx** | 1.29.1 | ⚠️ Nei | Local ingress |

## 🐳 Container-avhengigheter

### Observability Stack (Lokal utvikling)

| Container | Image | Versjon | Port | Kritisk |
|-----------|-------|---------|------|---------|
| **OpenTelemetry Collector** | otel/opentelemetry-collector-contrib | 0.131.1 | 4317/4318 | ⚠️ Nei |
| **Jaeger** | jaegertracing/all-in-one | 1.72.0 | 16686 | ⚠️ Nei |
| **Prometheus** | prom/prometheus | v3.5.0 | 9090 | ⚠️ Nei |
| **Loki** | grafana/loki | 3.5.3 | 3100 | ⚠️ Nei |
| **Grafana** | grafana/grafana | 11.6.3 | 3000 | ⚠️ Nei |
| **RedisInsight** | redis/redisinsight | latest | 7216 | ⚠️ Nei |

## 🔄 Automatisert sporing

### Implementerte mekanismer

1. **NuGet Package Tracking**
   - Alle pakker definert i .csproj filer
   - Versjonshåndtering via Directory.Build.props
   - Renovate Bot for automatiske oppdateringer

2. **Container Image Tracking**
   - Docker images definert i docker-compose filer
   - Versjonslåsing for alle production images
   - Renovate Bot for oppdateringer

3. **Infrastructure Tracking**
   - Azure-ressurser definert i Bicep templates
   - Versjonshåndtering av SKUs og konfigurasjoner
   - Environment-spesifikke parameterfiler

### Anbefalte forbedringer

1. **SBOM (Software Bill of Materials)**
   - Implementer automatisk SBOM-generering
   - Integrer med sikkerhetsskanning
   - Lagre SBOM som build artifacts

2. **Dependency Vulnerability Scanning**
   - GitHub Dependabot allerede aktivt
   - Vurder Snyk eller WhiteSource integration
   - Automatisk sikkerhetsskanning i pipeline

3. **License Compliance**
   - Implementer license scanning
   - Dokumenter godkjente licenser
   - Automatisk varsling ved nye licenser

## 📞 Kontaktinformasjon

### Interne team
- **Dialogporten Team**: @elsand, @arealmaas
- **Infrastructure**: Altinn Platform Team
- **Security**: Digdir Security Team

### Eksterne leverandører
- **Microsoft Azure**: Support via Azure Portal
- **Altinn**: platform@altinn.no
- **Digdir**: Maskinporten/ID-porten support

## 🔄 Vedlikeholdsrutiner

### Månedlig gjennomgang
- [ ] Sjekk for kritiske sikkerhetsoppdateringer
- [ ] Gjennomgå Renovate Bot PRs
- [ ] Oppdater denne dokumentasjonen

### Kvartalsvis gjennomgang
- [ ] Evaluér nye major versions
- [ ] Gjennomgå SLA-er og support-avtaler
- [ ] Vurder avhengighetsreduksjon

### Årlig gjennomgang
- [ ] Fullstendig arkitekturevaluering
- [ ] Leverandørevaluering
- [ ] Oppdater disaster recovery planer

---

## 🤖 Automatisk genererte avhengigheter

*Denne seksjonen oppdateres automatisk av Update-Dependencies.ps1*

### Automatisk oppdatert NuGet-oversikt

*Generert automatisk fra alle .csproj filer*

| Pakke | Versjon | Antall filer |
|-------|---------|--------------|
| **Altinn.ApiClients.Maskinporten** | 9.2.1 | 1 |
| **Altinn.Authorization.ABAC** | 0.0.8 | 1 |
| **AppAny.HotChocolate.FluentValidation** | 0.12.0 | 1 |
| **AspNetCore.HealthChecks.UI.Client** | 9.0.0 | 1 |
| **AutoMapper** | 14.0.0 | 1 |
| **Azure.Identity** | 1.15.0 | 1 |
| **Azure.Monitor.OpenTelemetry.AspNetCore** | 1.2.0 | 1 |
| **Bogus** | 35.6.3 | 1 |
| **Cocona** | 2.2.0 | 1 |
| **CommandLineParser** | 2.9.1 | 1 |
| **coverlet.collector** | 6.0.4 | 1 |
| **EntityFrameworkCore.Exceptions.PostgreSQL** | 8.1.3 | 1 |
| **FastEndpoints.Swagger** | 5.35.0 | 1 |
| **FluentAssertions** | [7.2.0, 8.0.0) | 1 |
| **FluentValidation.DependencyInjectionExtensions** | 12.0.0 | 1 |
| **HotChocolate.AspNetCore** | 15.1.8 | 1 |
| **HotChocolate.AspNetCore.Authorization** | 15.1.8 | 1 |
| **HotChocolate.Data.EntityFramework** | 15.1.8 | 1 |
| **HotChocolate.Diagnostics** | 15.1.8 | 1 |
| **HotChocolate.Subscriptions.Redis** | 15.1.8 | 1 |
| **HtmlAgilityPack** | 1.12.2 | 1 |
| **MassTransit.Azure.ServiceBus.Core** | 8.5.2 | 1 |
| **MassTransit.EntityFrameworkCore** | 8.5.2 | 1 |
| **MediatR** | 12.5.0 | 1 |
| **MediatR.Contracts** | 2.0.1 | 1 |
| **Medo.Uuid7** | 3.1.0 | 1 |
| **Microsoft.ApplicationInsights.AspNetCore** | 2.23.0 | 1 |
| **Microsoft.AspNetCore.Authentication.JwtBearer** | 9.0.8 | 1 |
| **Microsoft.AspNetCore.Mvc.NewtonsoftJson** | 9.0.8 | 1 |
| **Microsoft.AspNetCore.Mvc.Testing** | 9.0.8 | 1 |
| **Microsoft.AspNetCore.OpenApi** | 9.0.8 | 1 |
| **Microsoft.Azure.AppConfiguration.AspNetCore** | 8.3.0 | 1 |
| **Microsoft.Build** | 17.14.8 | 1 |
| **Microsoft.EntityFrameworkCore.Relational** | 9.0.8 | 1 |
| **Microsoft.EntityFrameworkCore.Tools** | 9.0.8 | 1 |
| **Microsoft.Extensions.Caching.StackExchangeRedis** | 9.0.8 | 1 |
| **Microsoft.Extensions.Configuration** | 9.0.8 | 1 |
| **Microsoft.Extensions.Configuration.EnvironmentVariables** | 9.0.8 | 1 |
| **Microsoft.Extensions.Configuration.Json** | 9.0.8 | 1 |
| **Microsoft.Extensions.Configuration.UserSecrets** | 9.0.8 | 1 |
| **Microsoft.Extensions.DependencyInjection** | 9.0.8 | 1 |
| **Microsoft.Extensions.DependencyInjection.Abstractions** | 9.0.8 | 1 |
| **Microsoft.Extensions.Diagnostics.HealthChecks** | 9.0.8 | 1 |
| **Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore** | 9.0.8 | 1 |
| **Microsoft.Extensions.Hosting** | 9.0.8 | 1 |
| **Microsoft.Extensions.Hosting.Abstractions** | 9.0.8 | 1 |
| **Microsoft.Extensions.Http** | 9.0.8 | 1 |
| **Microsoft.Extensions.Http.Polly** | 9.0.8 | 1 |
| **Microsoft.Extensions.Options.ConfigurationExtensions** | 9.0.8 | 1 |
| **Microsoft.NET.Test.Sdk** | 17.14.1 | 1 |
| **NetArchTest.Rules** | 1.3.2 | 1 |
| **Npgsql** | 9.0.3 | 1 |
| **Npgsql.EntityFrameworkCore.PostgreSQL** | 9.0.4 | 1 |
| **Npgsql.OpenTelemetry** | 9.0.3 | 1 |
| **NSec.Cryptography** | 25.4.0 | 1 |
| **NSubstitute** | 5.3.0 | 1 |
| **NSwag.MSBuild** | 14.2.0 | 1 |
| **OneOf** | 3.0.271 | 1 |
| **OneOf.SourceGenerator** | 3.0.271 | 1 |
| **OpenTelemetry** | 1.10.0 | 1 |
| **OpenTelemetry.Exporter.OpenTelemetryProtocol** | 1.10.0 | 1 |
| **OpenTelemetry.Instrumentation.Runtime** | 1.10.0 | 1 |
| **Polly.Contrib.WaitAndRetry** | 1.1.1 | 1 |
| **Refit** | 8.0.0 | 1 |
| **Refit.HttpClientFactory** | 8.0.0 | 1 |
| **Refitter.SourceGenerator** | 1.6.2 | 1 |
| **Respawn** | 6.2.1 | 1 |
| **Scrutor** | 6.1.0 | 1 |
| **Serilog.AspNetCore** | 9.0.0 | 1 |
| **Serilog.Enrichers.Environment** | 3.0.1 | 1 |
| **Serilog.Extensions.Hosting** | 9.0.0 | 1 |
| **Serilog.Settings.Configuration** | 9.0.0 | 1 |
| **Serilog.Sinks.ApplicationInsights** | 4.0.0 | 1 |
| **Serilog.Sinks.Console** | 6.0.0 | 1 |
| **Serilog.Sinks.OpenTelemetry** | 4.2.0 | 1 |
| **Testcontainers.PostgreSql** | 4.6.0 | 1 |
| **UUIDNext** | 4.1.2 | 1 |
| **Verify.Xunit** | 30.7.3 | 1 |
| **xunit** | 2.9.3 | 1 |
| **xunit.runner.visualstudio** | 3.1.4 | 1 |
| **ZiggyCreatures.FusionCache** | 2.4.0 | 1 |
| **ZiggyCreatures.FusionCache.Backplane.StackExchangeRedis** | 2.4.0 | 1 |
| **ZiggyCreatures.FusionCache.OpenTelemetry** | 2.4.0 | 1 |
| **ZiggyCreatures.FusionCache.Serialization.NeueccMessagePack** | 2.4.0 | 1 |


### Automatisk oppdatert Docker-oversikt

*Generert fra docker-compose filer*

| Image | Versjon | Fil |
|-------|---------|-----|
| **grafana/grafana** | 11.6.3 | docker-compose-otel.yml |
| **grafana/loki** | 3.5.3 | docker-compose-otel.yml |
| **jaegertracing/all-in-one** | 1.72.0 | docker-compose-otel.yml |
| **nginx** | 1.29.1 | docker-compose.yml |
| **otel/opentelemetry-collector-contrib** | 0.131.1 | docker-compose-otel.yml |
| **postgres** | 16.8 | docker-compose-db-redis.yml |
| **prom/prometheus** | v3.5.0 | docker-compose-otel.yml |
| **redis** | 6.0-alpine | docker-compose-db-redis.yml |
| **redis/redisinsight** | latest | docker-compose-db-redis.yml |

*Dette dokumentet er automatisk generert og vedlikeholdes av Dialogporten-teamet.*

