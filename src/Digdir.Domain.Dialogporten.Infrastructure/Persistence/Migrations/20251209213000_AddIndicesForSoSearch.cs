using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIndicesForSoSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Dialog_Org_ContentUpdatedAt_Id",
                table: "Dialog",
                columns: new[] { "Org", "ContentUpdatedAt", "Id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_Org_CreatedAt_Id",
                table: "Dialog",
                columns: new[] { "Org", "CreatedAt", "Id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_Org_Party_ContentUpdatedAt_Id",
                table: "Dialog",
                columns: new[] { "Org", "Party", "ContentUpdatedAt", "Id" },
                descending: new[] { false, false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_Org_ServiceResource_ContentUpdatedAt_Id",
                table: "Dialog",
                columns: new[] { "Org", "ServiceResource", "ContentUpdatedAt", "Id" },
                descending: new[] { false, false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_Org_UpdatedAt_Id",
                table: "Dialog",
                columns: new[] { "Org", "UpdatedAt", "Id" },
                descending: new[] { false, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Dialog_Org_ContentUpdatedAt_Id",
                table: "Dialog");

            migrationBuilder.DropIndex(
                name: "IX_Dialog_Org_CreatedAt_Id",
                table: "Dialog");

            migrationBuilder.DropIndex(
                name: "IX_Dialog_Org_Party_ContentUpdatedAt_Id",
                table: "Dialog");

            migrationBuilder.DropIndex(
                name: "IX_Dialog_Org_ServiceResource_ContentUpdatedAt_Id",
                table: "Dialog");

            migrationBuilder.DropIndex(
                name: "IX_Dialog_Org_UpdatedAt_Id",
                table: "Dialog");
        }
    }
}
