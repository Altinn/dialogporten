using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Sql;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GuardSearchSortKeyAgainstInfinity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The sort-key helper must exist before the view that references it.
            var scripts = new[]
            {
                "Dialog/Search/Function.EpochToBigintSortKey.sql",
                "Dialog/Search/View.VDialogContent.V4.sql",
            };

            foreach (var sql in MigrationSqlLoader.LoadAll(scripts))
            {
                migrationBuilder.Sql(sql);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore the view to the version without the helper before dropping it.
            foreach (var sql in MigrationSqlLoader.LoadAll("Dialog/Search/View.VDialogContent.V3.sql"))
            {
                migrationBuilder.Sql(sql);
            }

            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS search.""EpochToBigintSortKey""(timestamptz);");
        }
    }
}
