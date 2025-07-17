using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCoveringIndexToLabelValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_DialogServiceOwnerLabel_Value_Covering",
                table: "DialogServiceOwnerLabel",
                column: "Value")
                .Annotation("Npgsql:IndexInclude", new[] { "DialogServiceOwnerContextId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DialogServiceOwnerLabel_Value_Covering",
                table: "DialogServiceOwnerLabel");
        }
    }
}
