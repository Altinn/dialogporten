using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNewNonSensitiveContentTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "DialogContentType",
                columns: new[] { "Id", "AllowedMediaTypes", "MaxLength", "Name", "OutputInList", "Required" },
                values: new object[,]
                {
                    { 7, new[] { "text/plain" }, 255, "NonSensitiveTitle", true, false },
                    { 8, new[] { "text/plain" }, 255, "NonSensitiveSummary", true, false }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "DialogContentType",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "DialogContentType",
                keyColumn: "Id",
                keyValue: 8);
        }
    }
}
