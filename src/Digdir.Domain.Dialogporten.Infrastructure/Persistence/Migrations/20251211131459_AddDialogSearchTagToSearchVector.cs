using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Sql;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDialogSearchTagToSearchVector : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var scripts = new[]
            {
                "Dialog/Search/View.VDialogContent.V2.sql",
            };

            foreach (var sql in MigrationSqlLoader.LoadAll(scripts))
            {
                migrationBuilder.Sql(sql);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var scripts = new[]
            {
                "Dialog/Search/View.VDialogContent.sql",
            };

            foreach (var sql in MigrationSqlLoader.LoadAll(scripts))
            {
                migrationBuilder.Sql(sql);
            }
        }
    }
}
