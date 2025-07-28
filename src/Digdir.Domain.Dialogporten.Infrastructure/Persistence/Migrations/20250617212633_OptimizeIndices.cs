using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeIndices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create covering indexes for Dialog table to enable index-only scans
            // for queries that filter by Party or ServiceResource and select only a few columns.
            migrationBuilder.CreateIndex(
                name: "IX_Dialog_Covering_Party",
                table: "Dialog",
                column: "Party",
                filter: "\"Deleted\" = false")
                .Annotation("Npgsql:IndexInclude", new[]
                {
                    "Id", "ServiceResource", "CreatedAt", "UpdatedAt", "DueAt"
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_Covering_ServiceResource",
                table: "Dialog",
                column: "ServiceResource",
                filter: "\"Deleted\" = false")
                .Annotation("Npgsql:IndexInclude", new[]
                {
                    "Id", "Party", "CreatedAt", "UpdatedAt", "DueAt"
                });

            // Drop the old index on Party and ServiceResource
            migrationBuilder.DropIndex(
                name: "IX_Dialog_Party",
                table: "Dialog");

            migrationBuilder.DropIndex(
                name: "IX_Dialog_ServiceResource",
                table: "Dialog");

            // Remove redundant IX_Dialog_Org index (IX_Dialog_Org_IdempotentKey is sufficient)
            migrationBuilder.DropIndex(
                name: "IX_Dialog_Org",
                table: "Dialog");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Dialog_Covering_Party",
                table: "Dialog");

            migrationBuilder.DropIndex(
                name: "IX_Dialog_Covering_ServiceResource",
                table: "Dialog");

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_Party",
                table: "Dialog",
                column: "Party");

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_ServiceResource",
                table: "Dialog",
                column: "ServiceResource");

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_Org",
                table: "Dialog",
                column: "Org");
        }
    }
}
