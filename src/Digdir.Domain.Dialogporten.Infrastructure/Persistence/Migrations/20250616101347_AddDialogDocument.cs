using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDialogDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DialogDocument",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "current_timestamp at time zone 'utc'"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "current_timestamp at time zone 'utc'"),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false),
                    Org = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, collation: "C"),
                    ServiceResource = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, collation: "C"),
                    Party = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, collation: "C"),
                    ExtendedStatus = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ExternalReference = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    VisibleFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DueAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Process = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    PrecedingProcess = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    IsApiOnly = table.Column<bool>(type: "boolean", nullable: false),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    DialogData = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DialogDocument", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DialogDocument_CreatedAt",
                table: "DialogDocument",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DialogDocument_DueAt",
                table: "DialogDocument",
                column: "DueAt");

            migrationBuilder.CreateIndex(
                name: "IX_DialogDocument_ExtendedStatus",
                table: "DialogDocument",
                column: "ExtendedStatus");

            migrationBuilder.CreateIndex(
                name: "IX_DialogDocument_ExternalReference",
                table: "DialogDocument",
                column: "ExternalReference");

            migrationBuilder.CreateIndex(
                name: "IX_DialogDocument_IsApiOnly",
                table: "DialogDocument",
                column: "IsApiOnly");

            migrationBuilder.CreateIndex(
                name: "IX_DialogDocument_Org",
                table: "DialogDocument",
                column: "Org");

            migrationBuilder.CreateIndex(
                name: "IX_DialogDocument_Party",
                table: "DialogDocument",
                column: "Party");

            migrationBuilder.CreateIndex(
                name: "IX_DialogDocument_Process",
                table: "DialogDocument",
                column: "Process");

            migrationBuilder.CreateIndex(
                name: "IX_DialogDocument_ServiceResource",
                table: "DialogDocument",
                column: "ServiceResource");

            migrationBuilder.CreateIndex(
                name: "IX_DialogDocument_UpdatedAt",
                table: "DialogDocument",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DialogDocument_VisibleFrom",
                table: "DialogDocument",
                column: "VisibleFrom");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DialogDocument");
        }
    }
}
