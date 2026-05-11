# Repository Guidelines for AI coding agents

This file describes how AI coding agents should interact with the repository.

## Build & Test Commands
- **Build**: `dotnet build Digdir.Domain.Dialogporten.slnx --configuration Release`
- **Test default (non-E2E)**: `dotnet test Digdir.Domain.Dialogporten.slnx --configuration Release --no-build --filter 'FullyQualifiedName!~E2E'`
- **Test single**: `dotnet test --filter "FullyQualifiedName=Namespace.TestClass.TestMethod"`
- **Run project**: `cd src/ProjectDir && dotnet run`
- **Add DB migration**: `dotnet ef migrations add <Name> -p .\src\Digdir.Domain.Dialogporten.Infrastructure\`
- **Run C# E2E tests**: `scripts/e2e/run-webapi-e2e.zsh [webapi|graphql|both]`
- **Run K6 functional tests**: `./tests/k6/run.sh -e localdev -a v1 -u "$TOKENGENERATOR_USERNAME" -p "$TOKENGENERATOR_PASSWORD" suites/all-single-pass.js`
- Do **not** run performance test suites. All K6 tests requires internet connectivity.

Always run `dotnet build Digdir.Domain.Dialogporten.slnx --configuration Release` and `dotnet test Digdir.Domain.Dialogporten.slnx --configuration Release --no-build --filter 'FullyQualifiedName!~E2E'` after making changes to *.cs files in `./src/**` or `./tests/**`.

Do not run E2E tests by default. Run them only when adding or changing E2E tests, when the task explicitly requires E2E validation, or when the user asks for E2E verification. The default non-E2E test command still builds the E2E projects through the preceding solution build.

When E2E tests are required, use `scripts/e2e/.env` as the source of local ports: WebApi `https://localhost:7215`, GraphQL `http://localhost:5180/graphql`, and Service `http://localhost:56843`.
Check the required ports before starting the E2E launcher. If the required ports are free, run `scripts/e2e/run-webapi-e2e.zsh webapi`, `scripts/e2e/run-webapi-e2e.zsh graphql`, or `scripts/e2e/run-webapi-e2e.zsh both`. The launcher starts DB/Redis if needed, starts the required app processes, runs the selected E2E tests with explicit tests enabled, and cleans up the app processes it started. DB/Redis are intentionally left running.

If all required services are already responding on the configured ports, reuse them by exporting the same environment variables derived by the launcher and run the relevant E2E test project directly with `-- xUnit.Explicit=on`. If only some ports are occupied, or a port is occupied by something that does not respond as expected, stop and report the conflict instead of killing unknown processes. Do not start WebApi, GraphQL, or Service manually in separate long-lived sessions; rely on the E2E launcher for lifecycle management.

If integration tests fail because test containers are blocked by sandboxing, use `dotnet test Digdir.Domain.Dialogporten.slnx --configuration Release --no-build --filter 'FullyQualifiedName!~E2E&FullyQualifiedName!~Integration'` to skip them.

All code must compile with `TreatWarningsAsErrors=true` and pass the .NET analyzers.

Changes that affect Swagger/GraphQL spec must be reflected in the `docs/schema/v1/*verified*.json` files. Use the corresponding `*received*.json` files, which are generated upon build, for synchronization.
Do not edit `schema.verified.graphql` or `swagger.verified.json` directly; they are automatically generated and validated via snapshot tests.
The SwaggerSnapshot test will fail if these files are not identical. It will also fail when run in Debug configuration (must use Release).

## Code Style Guidelines
- Use file-scoped namespaces with `using` directives outside the namespace.
- 4‑space indentation (2 for JSON/YAML) with LF line endings.
- PascalCase for classes and methods, camelCase for variables and parameters. Exception: test method names should have Snake_Case naming (e.g. `Should_Get_SeenLog_By_Id_Verify_Snapshot`).
- Organize by feature folders following Clean Architecture (Domain/Application/Infrastructure/API layers).
- Prefer expression bodies for single-line members and use `var` when the type is apparent.
- Apply CQRS, domain events, repository pattern and rich domain result objects.
- Avoid throwing exceptions for domain flow; return domain-specific result objects.
- Enable nullable reference types and keep entities immutable. Use OneOf for union returns when applicable.
- Tests use xUnit with the fixture pattern and Verify for snapshot tests.
- New WebAPI E2E tests should use `response.ShouldHaveStatusCode(statusCode);` instead of `.StatusCode.Should().Be(statusCode);`.
- In test assertions, prefer explicit AwesomeAssertions null checks over null-coalescing throws for response content. Use `response.Content.Should().NotBeNull();` followed by assertions on `response.Content` instead of `var content = response.Content ?? throw new InvalidOperationException(...)`.
- "Try"-methods should return a boolean indicating success and a nullable `out` parameter annotated with [NotNullWhen(true)] for the result, e.g. `bool TryGetUser(int id, [NotNullWhen(true)] out User? user)`.

### LINQ & Collection Style
- Prefer LINQ over manual loops (`foreach`, `for`) for transformations, filtering, grouping, and projections.
- Prefer `FirstOrDefault()` / `SingleOrDefault()` with explicit null handling over index-based access.
- Use `Select`, `Where`, `GroupBy`, `ToDictionary`, `ToLookup` instead of building collections imperatively.
- Avoid materializing collections early (`ToList`, `ToArray`) unless required for correctness or performance.
- Prefer immutable LINQ pipelines over mutating existing collections.

### Modern C# Syntax (Required)
- Target latest language features available in the solution (.NET 10 / C# 14).
- Use extension block syntax
- Use the field keyword for auto properties
- Prefer pattern matching (`switch` expressions, property patterns) over `if/else` chains.
- Prefer switch expressions over statement-based `switch`.
- Prefer `if/else` chains over statement-based `switch`.
- Prefer `if` with early returns over `if/else` chains 
- Prefer records or record structs for immutable data models.
- Use collection expressions (`[]`) instead of `new List<T>()` or `new[] { }` where applicable.
- Prefer target-typed `new()` when the type is obvious.
- Prefer `with` expressions for non-destructive mutation.

### Nullability & Guards
- Do not suppress nullable warnings (`!`) unless strictly justified.
- Prefer pattern matching (`is not null`) over `!= null`.
- Prefer early returns over nested null checks.
- Use `ArgumentNullException.ThrowIfNull()` for public API guards.

### Database migrations and entity configuration
- Database migration files must be created using Entity Framework Core tools and not edited manually unless absolutely necessary.
- No use of .HasAnnotation("Npgsql:CreatedConcurrently", true) allowed, as this breaks automation of migrations.
- Database changes must be backwards compatible to avoid downtime during deployment.
- Database changes must be applied to production manually, this should be addressed in the PR description. Code reviewers should add a comment to remind the author of this.
- Only simple field additions with default values can be applied automatically in production.

## Pull Requests
- PR titles must follow the [Conventional Commits](https://www.conventionalcommits.org/) format, and must be prefixed such that the title is <type>[optional scope]: <description>. The title will be used as the squash commit message.
- Do not manually modify `CHANGELOG.md` or `version.txt`; these files are managed by automation.
