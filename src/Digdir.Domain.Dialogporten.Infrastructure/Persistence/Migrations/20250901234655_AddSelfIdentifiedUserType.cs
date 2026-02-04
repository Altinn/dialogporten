using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSelfIdentifiedUserType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "DialogUserType",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 5, "IdportenEmailIdentifiedUser" },
                    { 6, "AltinnSelfIdentifiedUser" },
                    { 7, "FeideUser" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "DialogUserType",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "DialogUserType",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "DialogUserType",
                keyColumn: "Id",
                keyValue: 7);
        }
    }
}
