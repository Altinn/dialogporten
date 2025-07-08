using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTransmissionCountOnDialog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "FromPartyTransmissionsCount",
                table: "Dialog",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<short>(
                name: "FromServiceOwnerTransmissionsCount",
                table: "Dialog",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.Sql("""
                                 UPDATE "Dialog" d
                                 SET 
                                     "FromPartyTransmissionsCount" = (
                                         SELECT COUNT(1) FROM "DialogTransmission" t
                                         WHERE t."DialogId" = d."Id"
                                           AND t."TypeId" IN (7, 8) 
                                     ),
                                     "FromServiceOwnerTransmissionsCount" = (
                                         SELECT COUNT(1) FROM "DialogTransmission" t
                                         WHERE t."DialogId" = d."Id"
                                           AND t."TypeId" NOT IN (7, 8)
                                     )
                                 """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FromPartyTransmissionsCount",
                table: "Dialog");

            migrationBuilder.DropColumn(
                name: "FromServiceOwnerTransmissionsCount",
                table: "Dialog");
        }
    }
}
