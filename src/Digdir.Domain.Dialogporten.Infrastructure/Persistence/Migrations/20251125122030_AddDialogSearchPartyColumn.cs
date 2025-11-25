using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDialogSearchPartyColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:btree_gin", ",,")
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.AddColumn<string>(
                name: "Party",
                schema: "search",
                table: "DialogSearch",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            // WARNING! This backfill cannot be done in a production environment
            // where these tables have any significant amount of data without
            // batching the updates to avoid long locks.
            migrationBuilder.Sql("""
                                     UPDATE search."DialogSearch" ds
                                     SET "Party" = d."Party"
                                     FROM "Dialog" d
                                     WHERE ds."DialogId" = d."Id"
                                 """);

            // WARNING! Creating this index on a large table in production
            // should be done with the CONCURRENTLY option to avoid long locks.
            migrationBuilder.CreateIndex(
                name: "IX_DialogSearch_Party_SearchVector",
                schema: "search",
                table: "DialogSearch",
                columns: new[] { "Party", "SearchVector" })
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DialogSearch_Party_SearchVector",
                schema: "search",
                table: "DialogSearch");

            migrationBuilder.DropColumn(
                name: "Party",
                schema: "search",
                table: "DialogSearch");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:btree_gin", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");
        }
    }
}
