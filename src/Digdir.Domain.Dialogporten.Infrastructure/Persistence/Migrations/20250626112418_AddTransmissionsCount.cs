using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTransmissionsCount : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FromPartyTransmissionsCount",
                table: "Dialog",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FromServiceOwnerTransmissionsCount",
                table: "Dialog",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FromPartyTransmissionsCount",
                table: "Dialog");

            migrationBuilder.DropColumn(
                name: "FromServiceOwnerTransmissionsCount",
                table: "Dialog");
        }
    }
}
