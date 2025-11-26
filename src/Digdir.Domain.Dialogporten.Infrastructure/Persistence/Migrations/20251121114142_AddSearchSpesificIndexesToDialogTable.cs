using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchSpesificIndexesToDialogTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Dialog_Party_ContentUpdatedAt_Id",
                table: "Dialog",
                columns: new[] { "Party", "ContentUpdatedAt", "Id" },
                descending: new[] { false, true, true })
                .Annotation("Npgsql:CreatedConcurrently", true)
                .Annotation("Npgsql:IndexInclude", new[] { "ServiceResource" });

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_Party_CreatedAt_Id",
                table: "Dialog",
                columns: new[] { "Party", "CreatedAt", "Id" },
                descending: new[] { false, true, true })
                .Annotation("Npgsql:CreatedConcurrently", true)
                .Annotation("Npgsql:IndexInclude", new[] { "ServiceResource" });

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_Party_DueAt_Id",
                table: "Dialog",
                columns: new[] { "Party", "DueAt", "Id" },
                descending: new[] { false, true, true })
                .Annotation("Npgsql:CreatedConcurrently", true)
                .Annotation("Npgsql:IndexInclude", new[] { "ServiceResource" });

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_Party_UpdatedAt_Id",
                table: "Dialog",
                columns: new[] { "Party", "UpdatedAt", "Id" },
                descending: new[] { false, true, true })
                .Annotation("Npgsql:CreatedConcurrently", true)
                .Annotation("Npgsql:IndexInclude", new[] { "ServiceResource" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Dialog_Party_ContentUpdatedAt_Id",
                table: "Dialog");

            migrationBuilder.DropIndex(
                name: "IX_Dialog_Party_CreatedAt_Id",
                table: "Dialog");

            migrationBuilder.DropIndex(
                name: "IX_Dialog_Party_DueAt_Id",
                table: "Dialog");

            migrationBuilder.DropIndex(
                name: "IX_Dialog_Party_UpdatedAt_Id",
                table: "Dialog");
        }
    }
}
