using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDialogRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasRelations",
                table: "Dialog",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "DialogRelationType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DialogRelationType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DialogRelation",
                columns: table => new
                {
                    DialogId = table.Column<Guid>(type: "uuid", nullable: false),
                    RelatedDialogId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "current_timestamp at time zone 'utc'"),
                    TypeId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DialogRelation", x => new { x.DialogId, x.RelatedDialogId });
                    table.CheckConstraint("CK_DialogRelation_DialogId_LessThan_RelatedDialogId", "\"DialogId\" < \"RelatedDialogId\"");
                    table.ForeignKey(
                        name: "FK_DialogRelation_DialogRelationType_TypeId",
                        column: x => x.TypeId,
                        principalTable: "DialogRelationType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DialogRelation_Dialog_DialogId",
                        column: x => x.DialogId,
                        principalTable: "Dialog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DialogRelation_Dialog_RelatedDialogId",
                        column: x => x.RelatedDialogId,
                        principalTable: "Dialog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "DialogRelationType",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Parent" },
                    { 2, "Child" },
                    { 3, "Related" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DialogRelation_RelatedDialogId_DialogId",
                table: "DialogRelation",
                columns: new[] { "RelatedDialogId", "DialogId" });

            migrationBuilder.CreateIndex(
                name: "IX_DialogRelation_TypeId",
                table: "DialogRelation",
                column: "TypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DialogRelation");

            migrationBuilder.DropTable(
                name: "DialogRelationType");

            migrationBuilder.DropColumn(
                name: "HasRelations",
                table: "Dialog");
        }
    }
}
