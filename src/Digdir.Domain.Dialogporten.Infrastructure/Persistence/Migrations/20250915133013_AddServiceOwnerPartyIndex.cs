using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceOwnerPartyIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Dialog_ServiceResource_Party",
                table: "Dialog",
                columns: new[] { "ServiceResource", "Party" })
                .Annotation("Npgsql:IndexInclude", new[] { "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Dialog_ServiceResource_Party",
                table: "Dialog");
        }
    }
}
