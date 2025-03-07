using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingSearchIndexesExtStatusExtRefDeleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Dialog_Deleted",
                table: "Dialog",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_ExtendedStatus",
                table: "Dialog",
                column: "ExtendedStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_ExternalReference",
                table: "Dialog",
                column: "ExternalReference");

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_VisibleFrom",
                table: "Dialog",
                column: "VisibleFrom");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Dialog_Deleted",
                table: "Dialog");

            migrationBuilder.DropIndex(
                name: "IX_Dialog_ExtendedStatus",
                table: "Dialog");

            migrationBuilder.DropIndex(
                name: "IX_Dialog_ExternalReference",
                table: "Dialog");

            migrationBuilder.DropIndex(
                name: "IX_Dialog_VisibleFrom",
                table: "Dialog");
        }
    }
}
