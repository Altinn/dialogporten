using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDialogContentUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ContentUpdatedAt",
                table: "Dialog",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "current_timestamp at time zone 'utc'");

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_ContentUpdatedAt",
                table: "Dialog",
                column: "ContentUpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Dialog_ContentUpdatedAt",
                table: "Dialog");

            migrationBuilder.DropColumn(
                name: "ContentUpdatedAt",
                table: "Dialog");
        }
    }
}
