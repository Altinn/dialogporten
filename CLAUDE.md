# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Essential Commands

**Build & Test:**
- **Build**: `dotnet build Digdir.Domain.Dialogporten.sln`
- **Test all**: `dotnet test Digdir.Domain.Dialogporten.sln` 
- **Test without integration**: `dotnet test Digdir.Domain.Dialogporten.sln --filter 'FullyQualifiedName!~Integration'`
- **Test single**: `dotnet test --filter "FullyQualifiedName=Namespace.TestClass.TestMethod"`

**Database Migrations:**
- **Add migration**: `dotnet ef migrations add <Name> -p .\src\Digdir.Domain.Dialogporten.Infrastructure\`

**Local Development:**
- **Start all services**: `podman compose up`
- **Start without WebAPI/GraphQL (for IDE debugging)**: `podman compose -f docker-compose-no-webapi.yml up`
- **WebAPI**: https://localhost:7214/swagger
- **GraphQL**: https://localhost:7215/graphql

Always run build and test commands after making changes. Integration tests require Docker.

## Architecture Overview

**Clean Architecture** with CQRS pattern:
- **Domain**: Core entities (Dialogs, Actors, Attachments, Parties) - no external dependencies
- **Application**: Use cases, CQRS handlers, domain events, validation behaviors
- **Infrastructure**: EF Core + PostgreSQL, external integrations (Altinn, Maskinporten)
- **WebAPI**: REST endpoints using FastEndpoints
- **GraphQL**: HotChocolate-based API

**Key Domain Concepts:**
- **Dialogs**: Main business entity with activities, transmissions, content
- **Actors**: Parties involved (Norwegian orgs/persons, system users)
- **Attachments**: File management with consumer-specific URLs
- **Parties**: Norwegian identifiers (NorwegianOrganizationIdentifier, NorwegianPersonIdentifier)

**Technology Stack:**
- .NET 9.0 (see global.json)
- Entity Framework Core with PostgreSQL
- xUnit + FluentAssertions + NSubstitute for testing
- Testcontainers for integration tests
- OpenTelemetry for observability

## Code Conventions

- File-scoped namespaces, 4-space indentation, LF line endings
- `TreatWarningsAsErrors=true` - all code must compile without warnings
- Nullable reference types enabled
- CQRS with rich domain result objects (avoid throwing exceptions for domain flow)
- OneOf for union return types
- Feature folder organization
- Use Verify for snapshot tests

## Schema Validation

Changes affecting Swagger/GraphQL specs must update `docs/schema/v1/*verified*.json` files using the corresponding `*received*.json` files generated on build. SwaggerSnapshot test must pass (requires Release configuration, not Debug).

## Local Development Configuration

Use `appsettings.local.json` (git-ignored) for personal dev settings. Toggle external dependencies in `appsettings.Development.json`:
- `DisableAuth: true` - Skip authentication  
- `UseLocalDevelopmentUser: true` - Mock user context
- `UseInMemoryServiceBusTransport: true` - In-memory messaging

## Database Setup for IDE Debugging

Set user secret for DB connection:
```bash
dotnet user-secrets set -p "./src/Digdir.Domain.Dialogporten.WebApi" "Infrastructure:DialogDbConnectionString" "Server=localhost;Port=5432;Database=dialogporten;User ID=postgres;Password=supersecret;Include Error Detail=True;"
```