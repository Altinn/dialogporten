using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChangeFCEMediaTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "DialogContentType",
                keyColumn: "Id",
                keyValue: 6,
                column: "AllowedMediaTypes",
                value: new[] { "application/vnd.dialogporten.frontchannelembed-url;type=text/markdown" });

            migrationBuilder.UpdateData(
                table: "DialogTransmissionContentType",
                keyColumn: "Id",
                keyValue: 3,
                column: "AllowedMediaTypes",
                value: new[] { "application/vnd.dialogporten.frontchannelembed-url;type=text/markdown" });

            migrationBuilder.Sql("""
                                 -- Update MediaType in DialogContent
                                 UPDATE public."DialogContent"
                                 SET "MediaType" = CASE
                                     WHEN "MediaType" = 'application/vnd.dialogporten.frontchannelembed+json;type=html'
                                     THEN 'application/vnd.dialogporten.frontchannelembed-url;type=text/html'
                                     WHEN "MediaType" = 'application/vnd.dialogporten.frontchannelembed+json;type=markdown'
                                     THEN 'application/vnd.dialogporten.frontchannelembed-url;type=text/markdown'
                                     ELSE "MediaType"
                                 END
                                 WHERE "MediaType" IN ('application/vnd.dialogporten.frontchannelembed+json;type=html',
                                    'application/vnd.dialogporten.frontchannelembed+json;type=markdown');
                                 
                                 -- Update MediaType in DialogTransmissionContent
                                 UPDATE public."DialogTransmissionContent"
                                 SET "MediaType" = CASE
                                     WHEN "MediaType" = 'application/vnd.dialogporten.frontchannelembed+json;type=html'
                                     THEN 'application/vnd.dialogporten.frontchannelembed-url;type=text/html'
                                     WHEN "MediaType" = 'application/vnd.dialogporten.frontchannelembed+json;type=markdown'
                                     THEN 'application/vnd.dialogporten.frontchannelembed-url;type=text/markdown'
                                     ELSE "MediaType"
                                 END
                                 WHERE "MediaType" IN ('application/vnd.dialogporten.frontchannelembed+json;type=html',
                                 'application/vnd.dialogporten.frontchannelembed+json;type=markdown');
                                 """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "DialogContentType",
                keyColumn: "Id",
                keyValue: 6,
                column: "AllowedMediaTypes",
                value: new[] { "application/vnd.dialogporten.frontchannelembed+json;type=markdown" });

            migrationBuilder.UpdateData(
                table: "DialogTransmissionContentType",
                keyColumn: "Id",
                keyValue: 3,
                column: "AllowedMediaTypes",
                value: new[] { "application/vnd.dialogporten.frontchannelembed+json;type=markdown" });
        }
    }
}
