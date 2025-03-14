using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IntroduceActorEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ActorNameEntityId",
                table: "Actor",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ActorName",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ActorId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "current_timestamp at time zone 'utc'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActorName", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Actor_ActorNameEntityId",
                table: "Actor",
                column: "ActorNameEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_ActorName_ActorId_Name",
                table: "ActorName",
                columns: new[] { "ActorId", "Name" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.AddForeignKey(
                name: "FK_Actor_ActorName_ActorNameEntityId",
                table: "Actor",
                column: "ActorNameEntityId",
                principalTable: "ActorName",
                principalColumn: "Id");

            migrationBuilder.Sql("""
                                    INSERT INTO "ActorName" ("Id", "CreatedAt", "ActorId", "Name")
                                    SELECT a."Id", -- Just borrow the Id from Actor to get uuid7
                                           a."CreatedAt",
                                           a."ActorId",
                                           a."ActorName"
                                    FROM "Actor" a
                                    WHERE a."ActorTypeId" != 2
                                    ON CONFLICT DO NOTHING;
                                    
                                    UPDATE "Actor" a
                                    SET "ActorNameEntityId" = (
                                        SELECT an."Id"
                                            FROM "ActorName" an
                                            WHERE a."ActorId" = an."ActorId"
                                                AND "ActorName" = an."Name")
                                      WHERE "ActorNameEntityId" IS NULL AND a."ActorTypeId" != 2;
                                 """);

            migrationBuilder.DropColumn(
                name: "ActorId",
                table: "Actor");

            migrationBuilder.DropColumn(
                name: "ActorName",
                table: "Actor");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Actor_ActorName_ActorNameEntityId",
                table: "Actor");

            migrationBuilder.DropTable(
                name: "ActorName");

            migrationBuilder.DropIndex(
                name: "IX_Actor_ActorNameEntityId",
                table: "Actor");

            migrationBuilder.DropColumn(
                name: "ActorNameEntityId",
                table: "Actor");

            migrationBuilder.AddColumn<string>(
                name: "ActorId",
                table: "Actor",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActorName",
                table: "Actor",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }
    }
}
