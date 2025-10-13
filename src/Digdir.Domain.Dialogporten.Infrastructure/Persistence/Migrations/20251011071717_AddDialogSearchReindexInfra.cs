using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Sql;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDialogSearchReindexInfra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /*
             * DialogSearch reindex infrastructure
             *
             * Purpose:
             *   Establishes a subsystem for rebuilding and maintaining the full-text search index for dialogs.
             *
            * Structure (the "search" schema is provisioned elsewhere):
            *   1) Canonical views (VDialogContent, VDialogDocument) providing content and weighted documents.
            *   2) Rebuild queue table (DialogSearchRebuildQueue) with supporting indexes.
            *   3) Observability views capturing progress and failures.
            *   4) Seeder functions covering full, incremental, and stale-only enqueueing.
            *   5) Upsert helper for individual dialogs.
            *   6) Claiming functions and the generic worker orchestrator.
             */

            var scripts = new[]
            {
                "Dialog/Search/View.VDialogContent.sql",
                "Dialog/Search/View.VDialogDocument.sql",
                "Dialog/Search/Table.DialogSearchRebuildQueue.sql",
                "Dialog/Search/View.DialogSearchRebuildProgress.sql",
                "Dialog/Search/View.DialogSearchRebuildFailures.sql",
                "Dialog/Search/Function.SeedDialogSearchQueueFull.sql",
                "Dialog/Search/Function.SeedDialogSearchQueueSince.sql",
                "Dialog/Search/Function.SeedDialogSearchQueueStale.sql",
                "Dialog/Search/Function.UpsertDialogSearchOne.sql",
                "Dialog/Search/Function.ClaimDialogsStandard.sql",
                "Dialog/Search/Function.ClaimDialogsStaleFirst.sql",
                "Dialog/Search/Function.RebuildDialogSearchOnce.sql"
            };

            foreach (var sql in MigrationSqlLoader.LoadAll(scripts))
            {
                migrationBuilder.Sql(sql);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP FUNCTION IF EXISTS search."RebuildDialogSearchOnce"(text, int, bigint);
                DROP FUNCTION IF EXISTS search."ClaimDialogsStaleFirst"(int);
                DROP FUNCTION IF EXISTS search."ClaimDialogsStandard"(int);
                DROP FUNCTION IF EXISTS search."UpsertDialogSearchOne"(uuid);
                DROP VIEW IF EXISTS search."DialogSearchRebuildFailures";
                DROP VIEW IF EXISTS search."DialogSearchRebuildProgress";
                DROP VIEW IF EXISTS search."VDialogDocument";
                DROP VIEW IF EXISTS search."VDialogContent";
                DROP FUNCTION IF EXISTS search."SeedDialogSearchQueueStale"(boolean);
                DROP FUNCTION IF EXISTS search."SeedDialogSearchQueueSince"(timestamptz, boolean);
                DROP FUNCTION IF EXISTS search."SeedDialogSearchQueueFull"(boolean);
                DROP TABLE IF EXISTS search."DialogSearchRebuildQueue";
                """);
        }
    }
}
