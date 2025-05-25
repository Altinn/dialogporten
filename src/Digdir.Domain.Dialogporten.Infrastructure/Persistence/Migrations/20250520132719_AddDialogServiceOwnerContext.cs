using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDialogServiceOwnerContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DialogServiceOwnerContext",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "current_timestamp at time zone 'utc'"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "current_timestamp at time zone 'utc'"),
                    Revision = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    DialogId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DialogServiceOwnerContext", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DialogServiceOwnerContext_Dialog_DialogId",
                        column: x => x.DialogId,
                        principalTable: "Dialog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DialogServiceOwnerLabel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DialogServiceOwnerContextId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DialogServiceOwnerLabel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DialogServiceOwnerLabel_DialogServiceOwnerContext_DialogSer~",
                        column: x => x.DialogServiceOwnerContextId,
                        principalTable: "DialogServiceOwnerContext",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DialogServiceOwnerContext_DialogId",
                table: "DialogServiceOwnerContext",
                column: "DialogId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DialogServiceOwnerLabel_DialogServiceOwnerContextId",
                table: "DialogServiceOwnerLabel",
                column: "DialogServiceOwnerContextId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DialogServiceOwnerLabel");

            migrationBuilder.DropTable(
                name: "DialogServiceOwnerContext");
        }
    }
}
