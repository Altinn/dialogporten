# Repository Guidelines for AI coding agents

This file describes how AI coding agents should interact with the repository.

## Build & Test Commands
- **Build**: `dotnet build Digdir.Domain.Dialogporten.sln`
- **Test all**: `dotnet test Digdir.Domain.Dialogporten.sln`
- **Test w/o integration**: `dotnet test Digdir.Domain.Dialogporten.sln --filter 'FullyQualifiedName!~Integration'`
- **Test single**: `dotnet test --filter "FullyQualifiedName=Namespace.TestClass.TestMethod"`
- **Run project**: `cd src/ProjectDir && dotnet run`
- **Add DB migration**: `dotnet ef migrations add <Name> -p .\src\Digdir.Domain.Dialogporten.Infrastructure\`
- **Run K6 functional tests**: `./tests/k6/run.sh -e localdev -a v1 -u "$TOKENGENERATOR_USERNAME" -p "$TOKENGENERATOR_PASSWORD" suites/all-single-pass.js`
- Do **not** run performance test suites. All K6 tests requires internet connectivity.

Always run `dotnet build` and `dotnet test` after making changes. Running integration tests require Docker, so run tests outside any sandbox. 

If a sandbox is absolutely needed, use `dotnet test Digdir.Domain.Dialogporten.sln --filter 'FullyQualifiedName!~Integration'` to skip them. 

All code must compile with `TreatWarningsAsErrors=true` and pass the .NET analyzers.

Changes that affect Swagger/GraphQL spec must be reflected in the `docs/schema/v1/*verified*.json` files. Use the corresponding `*received*.json` files, which are generated upon build, for synchronization. 
The SwaggerSnapshot test will fail if these files are not identical. The SwaggerSnapshot test will fail if running the in debug configuration (must use release).

## Code Style Guidelines
- Use file-scoped namespaces with `using` directives outside the namespace.
- 4‑space indentation (2 for JSON/YAML) with LF line endings.
- PascalCase for classes and methods, camelCase for variables and parameters.
- Organize by feature folders following Clean Architecture (Domain/Application/Infrastructure/API layers).
- Prefer expression bodies for single-line members and use `var` when the type is apparent.
- Apply CQRS, domain events, repository pattern and rich domain result objects.
- Avoid throwing exceptions for domain flow; return domain-specific result objects.
- Enable nullable reference types and keep entities immutable. Use OneOf for union returns when applicable.
- Tests use xUnit with the fixture pattern and Verify for snapshot tests.

### LINQ & Collection Style
- Prefer LINQ over manual loops (`foreach`, `for`) for transformations, filtering, grouping, and projections.
- Prefer `FirstOrDefault()` / `SingleOrDefault()` with explicit null handling over index-based access.
- Use `Select`, `Where`, `GroupBy`, `ToDictionary`, `ToLookup` instead of building collections imperatively.
- Avoid materializing collections early (`ToList`, `ToArray`) unless required for correctness or performance.
- Prefer immutable LINQ pipelines over mutating existing collections.

### Modern C# Syntax (Required)
- Target latest language features available in the solution (.NET 9 / C# 13).
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

## Pull Requests
- PR titles must follow the [Conventional Commits](https://www.conventionalcommits.org/) format, and must be prefixed such that the title is <type>[optional scope]: <description>. The title will be used as the squash commit message.
- Do not manually modify `CHANGELOG.md` or `version.txt`; these files are managed by automation.

