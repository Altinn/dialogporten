using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddedIdemotentkeyForTransmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdempotentKey",
                table: "DialogTransmission",
                type: "character varying(36)",
                maxLength: 36,
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
        }
    }
}
