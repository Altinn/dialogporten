using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDialogSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "search");

            migrationBuilder.CreateTable(
                name: "DialogSearch",
                schema: "search",
                columns: table => new
                {
                    DialogId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "current_timestamp at time zone 'utc'"),
                    SearchVector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DialogSearch", x => x.DialogId);
                    table.ForeignKey(
                        name: "FK_DialogSearch_Dialog_DialogId",
                        column: x => x.DialogId,
                        principalTable: "Dialog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Iso639TsVectorMap",
                schema: "search",
                columns: table => new
                {
                    IsoCode = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TsConfigName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Iso639TsVectorMap", x => x.IsoCode);
                });

            migrationBuilder.InsertData(
                schema: "search",
                table: "Iso639TsVectorMap",
                columns: new[] { "IsoCode", "TsConfigName" },
                values: new object[,]
                {
                    { "ar", "arabic" },
                    { "ca", "catalan" },
                    { "da", "danish" },
                    { "de", "german" },
                    { "el", "greek" },
                    { "en", "english" },
                    { "es", "spanish" },
                    { "eu", "basque" },
                    { "fi", "finnish" },
                    { "fr", "french" },
                    { "ga", "irish" },
                    { "hi", "hindi" },
                    { "hu", "hungarian" },
                    { "hy", "armenian" },
                    { "id", "indonesian" },
                    { "it", "italian" },
                    { "lt", "lithuanian" },
                    { "nb", "norwegian" },
                    { "ne", "nepali" },
                    { "nl", "dutch" },
                    { "nn", "norwegian" },
                    { "no", "norwegian" },
                    { "pt", "portuguese" },
                    { "ro", "romanian" },
                    { "ru", "russian" },
                    { "sr", "serbian" },
                    { "sv", "swedish" },
                    { "ta", "tamil" },
                    { "tr", "turkish" },
                    { "yi", "yiddish" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_ServiceResource_Party",
                table: "Dialog",
                columns: new[] { "ServiceResource", "Party" })
                .Annotation("Npgsql:IndexInclude", new[] { "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_DialogSearch_SearchVector",
                schema: "search",
                table: "DialogSearch",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DialogSearch",
                schema: "search");

            migrationBuilder.DropTable(
                name: "Iso639TsVectorMap",
                schema: "search");

            migrationBuilder.DropIndex(
                name: "IX_Dialog_ServiceResource_Party",
                table: "Dialog");
        }
    }
}
