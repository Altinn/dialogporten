using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class addCorrespondenceMaxLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CorrespondenceMaxLength",
                table: "DialogTransmissionContentType",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CorrespondenceMaxLength",
                table: "DialogContentType",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "DialogContentType",
                keyColumn: "Id",
                keyValue: 1,
                column: "CorrespondenceMaxLength",
                value: 512);

            migrationBuilder.UpdateData(
                table: "DialogContentType",
                keyColumn: "Id",
                keyValue: 2,
                column: "CorrespondenceMaxLength",
                value: 512);

            migrationBuilder.UpdateData(
                table: "DialogContentType",
                keyColumn: "Id",
                keyValue: 3,
                column: "CorrespondenceMaxLength",
                value: 512);

            migrationBuilder.UpdateData(
                table: "DialogContentType",
                keyColumn: "Id",
                keyValue: 4,
                column: "CorrespondenceMaxLength",
                value: 1023);

            migrationBuilder.UpdateData(
                table: "DialogContentType",
                keyColumn: "Id",
                keyValue: 5,
                column: "CorrespondenceMaxLength",
                value: 20);

            migrationBuilder.UpdateData(
                table: "DialogContentType",
                keyColumn: "Id",
                keyValue: 6,
                column: "CorrespondenceMaxLength",
                value: 1023);

            migrationBuilder.UpdateData(
                table: "DialogContentType",
                keyColumn: "Id",
                keyValue: 7,
                column: "CorrespondenceMaxLength",
                value: 512);

            migrationBuilder.UpdateData(
                table: "DialogContentType",
                keyColumn: "Id",
                keyValue: 8,
                column: "CorrespondenceMaxLength",
                value: 512);

            migrationBuilder.UpdateData(
                table: "DialogTransmissionContentType",
                keyColumn: "Id",
                keyValue: 1,
                column: "CorrespondenceMaxLength",
                value: 512);

            migrationBuilder.UpdateData(
                table: "DialogTransmissionContentType",
                keyColumn: "Id",
                keyValue: 2,
                column: "CorrespondenceMaxLength",
                value: 512);

            migrationBuilder.UpdateData(
                table: "DialogTransmissionContentType",
                keyColumn: "Id",
                keyValue: 3,
                column: "CorrespondenceMaxLength",
                value: 1023);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CorrespondenceMaxLength",
                table: "DialogTransmissionContentType");

            migrationBuilder.DropColumn(
                name: "CorrespondenceMaxLength",
                table: "DialogContentType");
        }
    }
}
