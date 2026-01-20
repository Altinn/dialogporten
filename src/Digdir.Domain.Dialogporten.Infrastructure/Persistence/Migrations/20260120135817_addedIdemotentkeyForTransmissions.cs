using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class addedIdemotentkeyForTransmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DialogTransmission_DialogId",
                table: "DialogTransmission");

            migrationBuilder.AddColumn<string>(
                name: "IdempotentKey",
                table: "DialogTransmission",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DialogTransmission_DialogId_IdempotentKey",
                table: "DialogTransmission",
                columns: new[] { "DialogId", "IdempotentKey" },
                unique: true,
                filter: "\"IdempotentKey\" is not null");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DialogTransmission_DialogId_IdempotentKey",
                table: "DialogTransmission");

            migrationBuilder.DropColumn(
                name: "IdempotentKey",
                table: "DialogTransmission");

            migrationBuilder.CreateIndex(
                name: "IX_DialogTransmission_DialogId",
                table: "DialogTransmission",
                column: "DialogId");
        }
    }
}
