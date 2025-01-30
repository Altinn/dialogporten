using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIdempotentKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdempotentKey",
                table: "Dialog",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_Org_IdempotentKey",
                table: "Dialog",
                columns: new[] { "Org", "IdempotentKey" },
                unique: true,
                filter: "\"IdempotentKey\" is not null");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Dialog_Org_IdempotentKey",
                table: "Dialog");

            migrationBuilder.DropColumn(
                name: "IdempotentKey",
                table: "Dialog");
        }
    }
}
