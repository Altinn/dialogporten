using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCoveringIndicesForAf : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Dialog_Id_Covering",
                table: "Dialog",
                column: "Id")
                .Annotation("Npgsql:IndexInclude", new[] { "ServiceResource", "IsApiOnly", "StatusId", "Org", "VisibleFrom", "ExpiresAt", "ContentUpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_Party_ContentUpdatedAt_Id_Covering",
                table: "Dialog",
                columns: new[] { "Party", "ContentUpdatedAt", "Id" },
                descending: new[] { false, true, true })
                .Annotation("Npgsql:IndexInclude", new[] { "ServiceResource", "IsApiOnly", "StatusId", "Org", "VisibleFrom", "ExpiresAt" });

            migrationBuilder.DropIndex(
                name: "IX_Dialog_Party_ContentUpdatedAt_Id",
                table: "Dialog");

            // No longer needed in new query patterns
            migrationBuilder.DropIndex(
                name: "IX_Dialog_ServiceResource_Party",
                table: "Dialog");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Dialog_Party_ContentUpdatedAt_Id",
                table: "Dialog",
                columns: new[] { "Party", "ContentUpdatedAt", "Id" },
                descending: new[] { false, true, true })
                .Annotation("Npgsql:CreatedConcurrently", true)
                .Annotation("Npgsql:IndexInclude", new[] { "ServiceResource" });

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_ServiceResource_Party",
                table: "Dialog",
                columns: new[] { "ServiceResource", "Party" })
                .Annotation("Npgsql:IndexInclude", new[] { "Id" });

            migrationBuilder.DropIndex(
                name: "IX_Dialog_Id_Covering",
                table: "Dialog");

            migrationBuilder.DropIndex(
                name: "IX_Dialog_Party_ContentUpdatedAt_Id_Covering",
                table: "Dialog");
        }
    }
}
