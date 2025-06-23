using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReAssignDialogStatusValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "DialogStatus",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 7, "NotApplicable" },
                    { 8, "Awaiting" }
                });

            migrationBuilder.Sql(
                """
                UPDATE "Dialog" SET "StatusId" = 7 WHERE "StatusId" = 1;
                UPDATE "Dialog" SET "StatusId" = 8 WHERE "StatusId" = 4;
                """);

            migrationBuilder.DeleteData(
                table: "DialogStatus",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "DialogStatus",
                keyColumn: "Id",
                keyValue: 4);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "DialogStatus",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "NotApplicable" },
                    { 4, "Awaiting" }
                });

            migrationBuilder.Sql(
                """
                UPDATE "Dialog" SET "StatusId" = 1 WHERE "StatusId" = 7;
                UPDATE "Dialog" SET "StatusId" = 4 WHERE "StatusId" = 8;
                """);

            migrationBuilder.DeleteData(
                table: "DialogStatus",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "DialogStatus",
                keyColumn: "Id",
                keyValue: 8);
        }
    }
}
