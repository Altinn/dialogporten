using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SetSentLabel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                // language=PostgreSQL
                """
                WITH relevantDialogIds AS (
                    SELECT "DialogId"
                    FROM "DialogTransmission" dt
                    WHERE dt."TypeId" IN (7, 8)
                    GROUP BY dt."DialogId"
                )
                INSERT INTO "DialogEndUserContextSystemLabel" ("SystemLabelId", "DialogEndUserContextId")
                SELECT 5, c."Id"
                FROM "DialogEndUserContext" c
                INNER JOIN relevantDialogIds r ON c."DialogId" = r."DialogId"
                ON CONFLICT DO NOTHING;
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
