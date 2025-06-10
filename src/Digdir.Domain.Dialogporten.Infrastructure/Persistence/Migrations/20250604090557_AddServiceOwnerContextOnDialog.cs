using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceOwnerContextOnDialog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DialogServiceOwnerContext",
                columns: table => new
                {
                    DialogId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "current_timestamp at time zone 'utc'"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "current_timestamp at time zone 'utc'"),
                    Revision = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DialogServiceOwnerContext", x => x.DialogId);
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
                    Value = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DialogServiceOwnerContextId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "current_timestamp at time zone 'utc'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DialogServiceOwnerLabel", x => new { x.DialogServiceOwnerContextId, x.Value });
                    table.ForeignKey(
                        name: "FK_DialogServiceOwnerLabel_DialogServiceOwnerContext_DialogSer~",
                        column: x => x.DialogServiceOwnerContextId,
                        principalTable: "DialogServiceOwnerContext",
                        principalColumn: "DialogId",
                        onDelete: ReferentialAction.Cascade);
                });
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
