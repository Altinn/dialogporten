using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Sql;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDialogIndexerFunctions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var scripts = new[]
            {
                "Dialog/Search/View.VDialogDocument.V2.sql",
                "Dialog/Search/Function.UpsertDialogSearchOne.V2.sql",
                "Dialog/Search/Function.RebuildDialogSearchOnce.V2.sql"
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
                "Dialog/Search/View.VDialogDocument.sql",
                "Dialog/Search/Function.UpsertDialogSearchOne.sql",
                "Dialog/Search/Function.RebuildDialogSearchOnce.sql"
            };

            foreach (var sql in MigrationSqlLoader.LoadAll(scripts))
            {
                migrationBuilder.Sql(sql);
            }
        }
    }
}
