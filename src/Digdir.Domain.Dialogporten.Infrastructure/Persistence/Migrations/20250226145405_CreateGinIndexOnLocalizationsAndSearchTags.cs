using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateGinIndexOnLocalizationsAndSearchTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DialogSearchTag_DialogId_Value",
                table: "DialogSearchTag");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateIndex(
                name: "IX_Localization_Value",
                table: "Localization",
                column: "Value")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_DialogSearchTag_DialogId",
                table: "DialogSearchTag",
                column: "DialogId");

            migrationBuilder.CreateIndex(
                name: "IX_DialogSearchTag_Value",
                table: "DialogSearchTag",
                column: "Value")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Localization_Value",
                table: "Localization");

            migrationBuilder.DropIndex(
                name: "IX_DialogSearchTag_DialogId",
                table: "DialogSearchTag");

            migrationBuilder.DropIndex(
                name: "IX_DialogSearchTag_Value",
                table: "DialogSearchTag");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateIndex(
                name: "IX_DialogSearchTag_DialogId_Value",
                table: "DialogSearchTag",
                columns: new[] { "DialogId", "Value" },
                unique: true);
        }
    }
}
