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
                "PartyResource/Schema.PartyResource.sql",
                "PartyResource/Function.PartyParseUrn.sql",
                "PartyResource/Function.ResourceFromUrn.sql",
                "PartyResource/Table.Party.sql",
                "PartyResource/Table.Resource.sql",
                "PartyResource/Table.PartyResource.sql",
                "PartyResource/Table.BackfillState.sql",
                "PartyResource/Function.PartyResourceAfterInsertRow.sql",
                "PartyResource/Function.PartyResourceAfterDeleteRow.sql",
                "PartyResource/Function.BackfillDialogPartyResourceBatch.sql",
                "PartyResource/Procedure.RunBackfillDialogPartyResource.sql",
                "PartyResource/Trigger.UpdatePartyResource_AfterInsert_Row.sql",
                "PartyResource/Trigger.UpdatePartyResource_AfterDelete_Row.sql",
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
                DROP TRIGGER IF EXISTS "UpdatePartyResource_AfterDelete_Row" ON public."Dialog";
                DROP TRIGGER IF EXISTS "UpdatePartyResource_AfterInsert_Row" ON public."Dialog";

                DROP PROCEDURE IF EXISTS partyresource.run_backfill_dialog_partyresource(integer, integer, double precision);
                DROP FUNCTION IF EXISTS partyresource.backfill_dialog_partyresource_batch(integer);
                DROP FUNCTION IF EXISTS partyresource.party_resource_after_delete_row();
                DROP FUNCTION IF EXISTS partyresource.party_resource_after_insert_row();

                DROP TABLE IF EXISTS partyresource."BackfillPairStage";
                DROP TABLE IF EXISTS partyresource."BackfillState";
                DROP TABLE IF EXISTS partyresource."BackfillShardState";
                DROP TABLE IF EXISTS partyresource."PartyResource";
                DROP TABLE IF EXISTS partyresource."Resource";
                DROP TABLE IF EXISTS partyresource."Party";

                DROP FUNCTION IF EXISTS partyresource.resource_from_urn(text);
                DROP FUNCTION IF EXISTS partyresource.party_parse_urn(text);
                DROP SCHEMA IF EXISTS partyresource;
                """);
        }
    }
}
