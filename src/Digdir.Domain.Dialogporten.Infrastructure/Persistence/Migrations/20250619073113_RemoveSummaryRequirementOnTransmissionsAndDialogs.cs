using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSummaryRequirementOnTransmissionsAndDialogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "DialogContentType",
                keyColumn: "Id",
                keyValue: 3,
                column: "Required",
                value: false);

            migrationBuilder.UpdateData(
                table: "DialogTransmissionContentType",
                keyColumn: "Id",
                keyValue: 2,
                column: "Required",
                value: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "DialogContentType",
                keyColumn: "Id",
                keyValue: 3,
                column: "Required",
                value: true);

            migrationBuilder.UpdateData(
                table: "DialogTransmissionContentType",
                keyColumn: "Id",
                keyValue: 2,
                column: "Required",
                value: true);
        }
    }
}
