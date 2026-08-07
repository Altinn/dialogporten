using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorizationContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "DialogGuiAction",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "DialogApiAction",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.CreateTable(
                name: "AuthorizationContextUnauthorizedPresentation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorizationContextUnauthorizedPresentation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuthorizationContext",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "current_timestamp at time zone 'utc'"),
                    ServiceResource = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    AdditionalResourceAttribute = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Parties = table.Column<List<string>>(type: "character varying(255)[]", nullable: false),
                    IncludeDialogParty = table.Column<bool>(type: "boolean", nullable: false),
                    Action = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    UnauthorizedPresentationId = table.Column<int>(type: "integer", nullable: false),
                    Discriminator = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    AttachmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApiActionId = table.Column<Guid>(type: "uuid", nullable: true),
                    GuiActionId = table.Column<Guid>(type: "uuid", nullable: true),
                    TransmissionId = table.Column<Guid>(type: "uuid", nullable: true),
                    NavigationalActionId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorizationContext", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthorizationContext_Attachment_AttachmentId",
                        column: x => x.AttachmentId,
                        principalTable: "Attachment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuthorizationContext_AuthorizationContextUnauthorizedPresen~",
                        column: x => x.UnauthorizedPresentationId,
                        principalTable: "AuthorizationContextUnauthorizedPresentation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuthorizationContext_DialogApiAction_ApiActionId",
                        column: x => x.ApiActionId,
                        principalTable: "DialogApiAction",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuthorizationContext_DialogGuiAction_GuiActionId",
                        column: x => x.GuiActionId,
                        principalTable: "DialogGuiAction",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuthorizationContext_DialogTransmissionNavigationalAction_N~",
                        column: x => x.NavigationalActionId,
                        principalTable: "DialogTransmissionNavigationalAction",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuthorizationContext_DialogTransmission_TransmissionId",
                        column: x => x.TransmissionId,
                        principalTable: "DialogTransmission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AuthorizationContextUnauthorizedPresentation",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Disabled" },
                    { 2, "Redacted" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationContext_ApiActionId",
                table: "AuthorizationContext",
                column: "ApiActionId",
                unique: true,
                filter: "\"ApiActionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationContext_AttachmentId",
                table: "AuthorizationContext",
                column: "AttachmentId",
                unique: true,
                filter: "\"AttachmentId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationContext_GuiActionId",
                table: "AuthorizationContext",
                column: "GuiActionId",
                unique: true,
                filter: "\"GuiActionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationContext_NavigationalActionId",
                table: "AuthorizationContext",
                column: "NavigationalActionId",
                unique: true,
                filter: "\"NavigationalActionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationContext_TransmissionId",
                table: "AuthorizationContext",
                column: "TransmissionId",
                unique: true,
                filter: "\"TransmissionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationContext_UnauthorizedPresentationId",
                table: "AuthorizationContext",
                column: "UnauthorizedPresentationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthorizationContext");

            migrationBuilder.DropTable(
                name: "AuthorizationContextUnauthorizedPresentation");

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "DialogGuiAction",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "DialogApiAction",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);
        }
    }
}
