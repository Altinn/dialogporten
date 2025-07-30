using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SupportForMultipleEndUserSystemLabels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DialogEndUserContextSystemLabel",
                columns: table => new
                {
                    SystemLabelId = table.Column<int>(type: "integer", nullable: false),
                    DialogEndUserContextId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "current_timestamp at time zone 'utc'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DialogEndUserContextSystemLabel", x => new { x.DialogEndUserContextId, x.SystemLabelId });
                    table.ForeignKey(
                        name: "FK_DialogEndUserContextSystemLabel_DialogEndUserContext_Dialog~",
                        column: x => x.DialogEndUserContextId,
                        principalTable: "DialogEndUserContext",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DialogEndUserContextSystemLabel_SystemLabel_SystemLabelId",
                        column: x => x.SystemLabelId,
                        principalTable: "SystemLabel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DialogEndUserContextSystemLabel_SystemLabelId",
                table: "DialogEndUserContextSystemLabel",
                column: "SystemLabelId");

            // Migrate existing SystemLabelId to the new table
            migrationBuilder.Sql(
                """
                INSERT INTO "DialogEndUserContextSystemLabel" ("DialogEndUserContextId", "SystemLabelId", "CreatedAt")
                SELECT "Id", "SystemLabelId", "CreatedAt"
                FROM "DialogEndUserContext"
                WHERE "SystemLabelId" IS NOT NULL;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_DialogEndUserContext_SystemLabel_SystemLabelId",
                table: "DialogEndUserContext");

            migrationBuilder.DropIndex(
                name: "IX_DialogEndUserContext_SystemLabelId",
                table: "DialogEndUserContext");

            migrationBuilder.DropColumn(
                name: "SystemLabelId",
                table: "DialogEndUserContext");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DialogEndUserContextSystemLabel");

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
