using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSentSystemLabel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "SystemLabel",
                columns: new[] { "Id", "Name" },
                values: new object[] { 4, "Sent" });

            migrationBuilder.AddColumn<int[]>(
                name: "SystemLabelIds",
                table: "DialogEndUserContext",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);

            migrationBuilder.Sql(@"
                UPDATE ""DialogEndUserContext""
                SET ""SystemLabelIds"" = ARRAY[""SystemLabelId""]
                WHERE ""SystemLabelId"" IS NOT NULL;
            ");

            migrationBuilder.DropForeignKey(
                name: "FK_DialogEndUserContext_SystemLabel_SystemLabelId",
                table: "DialogEndUserContext");

            migrationBuilder.DropIndex(
                name: "IX_DialogEndUserContext_SystemLabelId",
                table: "DialogEndUserContext");

            migrationBuilder.DropColumn(
                name: "SystemLabelId",
                table: "DialogEndUserContext");

            migrationBuilder.CreateIndex(
                name: "IX_DialogEndUserContext_SystemLabelIds",
                table: "DialogEndUserContext",
                column: "SystemLabelIds")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DialogEndUserContext_SystemLabelIds",
                table: "DialogEndUserContext");

            migrationBuilder.DeleteData(
                table: "SystemLabel",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DropColumn(
                name: "SystemLabelIds",
                table: "DialogEndUserContext");

            migrationBuilder.AddColumn<int>(
                name: "SystemLabelId",
                table: "DialogEndUserContext",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_DialogEndUserContext_SystemLabelId",
                table: "DialogEndUserContext",
                column: "SystemLabelId");

            migrationBuilder.AddForeignKey(
                name: "FK_DialogEndUserContext_SystemLabel_SystemLabelId",
                table: "DialogEndUserContext",
                column: "SystemLabelId",
                principalTable: "SystemLabel",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
