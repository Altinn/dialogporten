using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMarkAsUnopened : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DialogEndUserContext_SystemLabel_SystemLabelId",
                table: "DialogEndUserContext");

            migrationBuilder.DropIndex(
                name: "IX_DialogEndUserContext_SystemLabelId",
                table: "DialogEndUserContext");

            migrationBuilder.DropColumn(
                name: "SystemLabelId",
                table: "DialogEndUserContext");

            migrationBuilder.AddColumn<int[]>(
                name: "SystemLabelIds",
                table: "DialogEndUserContext",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);

            migrationBuilder.CreateTable(
                name: "DialogEndUserContextSystemLabel (Dictionary<string, object>)",
                columns: table => new
                {
                    DialogEndUserContextsId = table.Column<Guid>(type: "uuid", nullable: false),
                    SystemLabelsId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DialogEndUserContextSystemLabel (Dictionary<string, object>)", x => new { x.DialogEndUserContextsId, x.SystemLabelsId });
                    table.ForeignKey(
                        name: "FK_DialogEndUserContextSystemLabel (Dictionary<string, object>~",
                        column: x => x.DialogEndUserContextsId,
                        principalTable: "DialogEndUserContext",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DialogEndUserContextSystemLabel (Dictionary<string, object~1",
                        column: x => x.SystemLabelsId,
                        principalTable: "SystemLabel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "SystemLabel",
                columns: new[] { "Id", "Name" },
                values: new object[] { 4, "MarkedAsUnopened" });

            migrationBuilder.CreateIndex(
                name: "IX_DialogEndUserContextSystemLabel (Dictionary<string, object>~",
                table: "DialogEndUserContextSystemLabel (Dictionary<string, object>)",
                column: "SystemLabelsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DialogEndUserContextSystemLabel (Dictionary<string, object>)");

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
