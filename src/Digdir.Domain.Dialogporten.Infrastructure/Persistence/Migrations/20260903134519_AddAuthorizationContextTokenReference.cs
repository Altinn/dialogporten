using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorizationContextTokenReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TokenReference",
                table: "AuthorizationContext",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TokenReference",
                table: "AuthorizationContext");
        }
    }
}
