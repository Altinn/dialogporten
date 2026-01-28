using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTransmissionNavigationalActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "NavigationalActionId",
                table: "LocalizationSet",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DialogTransmissionNavigationalAction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "current_timestamp at time zone 'utc'"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "current_timestamp at time zone 'utc'"),
                    Url = table.Column<string>(type: "character varying(1023)", maxLength: 1023, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TransmissionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DialogTransmissionNavigationalAction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DialogTransmissionNavigationalAction_DialogTransmission_Tra~",
                        column: x => x.TransmissionId,
                        principalTable: "DialogTransmission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LocalizationSet_NavigationalActionId",
                table: "LocalizationSet",
                column: "NavigationalActionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DialogTransmissionNavigationalAction_TransmissionId",
                table: "DialogTransmissionNavigationalAction",
                column: "TransmissionId");

            migrationBuilder.AddForeignKey(
                name: "FK_LocalizationSet_DialogTransmissionNavigationalAction_Naviga~",
                table: "LocalizationSet",
                column: "NavigationalActionId",
                principalTable: "DialogTransmissionNavigationalAction",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LocalizationSet_DialogTransmissionNavigationalAction_Naviga~",
                table: "LocalizationSet");

            migrationBuilder.DropTable(
                name: "DialogTransmissionNavigationalAction");

            migrationBuilder.DropIndex(
                name: "IX_LocalizationSet_NavigationalActionId",
                table: "LocalizationSet");

            migrationBuilder.DropColumn(
                name: "NavigationalActionId",
                table: "LocalizationSet");
        }
    }
}
