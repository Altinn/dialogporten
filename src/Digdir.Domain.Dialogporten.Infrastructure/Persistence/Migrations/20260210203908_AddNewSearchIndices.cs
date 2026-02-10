using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNewSearchIndices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DialogEndUserContext_DialogId",
                table: "DialogEndUserContext");

            migrationBuilder.CreateIndex(
                name: "IX_DialogEndUserContext_DialogId_IncludeId",
                table: "DialogEndUserContext",
                column: "DialogId",
                unique: true)
                .Annotation("Npgsql:IndexInclude", new[] { "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_ServiceResource_Party_ContentUpdatedAt_Id_NotDeleted",
                table: "Dialog",
                columns: new[] { "ServiceResource", "Party", "ContentUpdatedAt", "Id" },
                descending: new[] { false, false, true, true },
                filter: "\"Deleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DialogEndUserContext_DialogId_IncludeId",
                table: "DialogEndUserContext");

            migrationBuilder.DropIndex(
                name: "IX_Dialog_ServiceResource_Party_ContentUpdatedAt_Id_NotDeleted",
                table: "Dialog");

            migrationBuilder.CreateIndex(
                name: "IX_DialogEndUserContext_DialogId",
                table: "DialogEndUserContext",
                column: "DialogId",
                unique: true);
        }
    }
}
