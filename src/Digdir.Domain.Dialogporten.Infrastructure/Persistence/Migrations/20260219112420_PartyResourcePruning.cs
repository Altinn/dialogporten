using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Sql;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PartyResourcePruning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var scripts = new[]
            {
                "PartyResource/Function.PartyParseUrn.sql",
                "PartyResource/Function.ResourceFromUrn.sql",
                "PartyResource/Table.Party.sql",
                "PartyResource/Table.Resource.sql",
                "PartyResource/Table.PartyResource.sql",
                "PartyResource/Function.PartyResourceAfterInsertRow.sql",
                "PartyResource/Function.PartyResourceAfterDeleteRow.sql",
                "PartyResource/Trigger.TR_PR_AfterInsert_Row.sql",
                "PartyResource/Trigger.TR_PR_AfterDelete.sql",
            };

            foreach (var sql in MigrationSqlLoader.LoadAll(scripts))
            {
                migrationBuilder.Sql(sql);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS "TR_PR_AfterDelete" ON public."Dialog";
                DROP TRIGGER IF EXISTS "TR_PR_AfterInsert_Row" ON public."Dialog";

                DROP FUNCTION IF EXISTS public.party_resource_after_delete_row();
                DROP FUNCTION IF EXISTS public.party_resource_after_insert_row();

                DROP TABLE IF EXISTS public."PartyResource";
                DROP TABLE IF EXISTS public."Resource";
                DROP TABLE IF EXISTS public."Party";

                DROP FUNCTION IF EXISTS public.resource_from_urn(text);
                DROP FUNCTION IF EXISTS public.party_parse_urn(text);
                """);
        }
    }
}
