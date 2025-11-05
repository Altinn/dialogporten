# SQL Migration Assets

This folder hosts handwritten SQL that augments Entity Framework migrations. It provides:

- Easier re-baselining: large SQL statements live in standalone `.sql` files.
- IDE/editor support: PostgreSQL syntax highlighting, linting, and copy/paste into `psql`/pgAdmin.
- Clear ownership: files are grouped by domain (e.g., `Dialog/Search`) and object type.

## Usage Guidelines

1. **Keep scripts idempotent.**  
   Use `CREATE OR REPLACE` and other defensive patterns so the migration can be re-run safely.

2. **Add EF migrations when dependent schema changes.**  
   If a script references application tables/columns (e.g., `Dialog.UpdatedAt`), ensure future schema updates either adjust the SQL here or add a new migration to maintain compatibility.

3. **Embed scripts through `MigrationSqlLoader`.**  
   New migrations should load new or updated SQL using the helper to keep C# code minimal.

### Example Migration

```csharp
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Sql;
using Microsoft.EntityFrameworkCore.Migrations;

public partial class AddDialogSearchReindexInfra : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var scripts = new[]
        {
            "Dialog/Search/View.VDialogContent.sql",
            // ...
        };

        foreach (var sql in MigrationSqlLoader.LoadAll(scripts))
        {
            migrationBuilder.Sql(sql);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        // Drop created objects in reverse dependency order
        migrationBuilder.Sql("""
            DROP VIEW IF EXISTS search."DialogSearchRebuildProgress";
            -- ...
        """);
}
```

