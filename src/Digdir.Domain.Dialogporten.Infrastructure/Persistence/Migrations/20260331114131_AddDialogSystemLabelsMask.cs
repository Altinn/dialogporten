using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Sql;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDialogSystemLabelsMask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "SystemLabelsMask",
                table: "Dialog",
                type: "smallint",
                nullable: false,
                defaultValue: (short)1);

            var scripts = new[]
            {
                "Dialog/SystemLabels/Function.RecomputeDialogSystemLabelsMask.sql",
                "Dialog/SystemLabels/Function.SyncDialogSystemLabelsMaskFromLabelChanges.sql",
                "Dialog/SystemLabels/Function.BackfillDialogSystemLabelsMaskBatch.sql",
                "Dialog/SystemLabels/Procedure.RunBackfillDialogSystemLabelsMask.sql",
                "Dialog/SystemLabels/Trigger.SyncDialogSystemLabelsMaskFromLabelChanges.sql",
            };

            foreach (var sql in MigrationSqlLoader.LoadAll(scripts))
            {
                migrationBuilder.Sql(sql);
            }

            migrationBuilder.DropIndex(
                name: "IX_Dialog_ServiceResource_Party_ContentUpdatedAt_Id_NotDeleted",
                table: "Dialog");

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_ServiceResource_Party_ContentUpdatedAt_Id_NotDeleted",
                table: "Dialog",
                columns: new[] { "ServiceResource", "Party", "ContentUpdatedAt", "Id" },
                descending: new[] { false, false, true, true },
                filter: "\"Deleted\" = false")
                .Annotation("Npgsql:IndexInclude", new[] { "StatusId", "VisibleFrom", "ExpiresAt", "IsApiOnly", "SystemLabelsMask" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS "TR_DialogSystemLabelsMask_AfterInsert_Statement" ON public."DialogEndUserContextSystemLabel";
                DROP TRIGGER IF EXISTS "TR_DialogSystemLabelsMask_AfterDelete_Statement" ON public."DialogEndUserContextSystemLabel";
                DROP TRIGGER IF EXISTS "TR_DialogSystemLabelsMask_AfterUpdate_Statement" ON public."DialogEndUserContextSystemLabel";

                DROP PROCEDURE IF EXISTS public.run_backfill_dialog_system_labels_mask(integer, uuid);
                DROP PROCEDURE IF EXISTS public.run_backfill_dialog_system_labels_mask(integer);
                DROP FUNCTION IF EXISTS public.backfill_dialog_system_labels_mask_batch(uuid, integer);
                DROP FUNCTION IF EXISTS public.sync_dialog_system_labels_mask_from_inserted_label_rows();
                DROP FUNCTION IF EXISTS public.sync_dialog_system_labels_mask_from_deleted_label_rows();
                DROP FUNCTION IF EXISTS public.sync_dialog_system_labels_mask_from_updated_label_rows();
                DROP FUNCTION IF EXISTS public.sync_dialog_system_labels_mask_from_label_changes(uuid[]);
                DROP FUNCTION IF EXISTS public.recompute_dialog_system_labels_mask(uuid[]);
                """);

            migrationBuilder.DropIndex(
                name: "IX_Dialog_ServiceResource_Party_ContentUpdatedAt_Id_NotDeleted",
                table: "Dialog");

            migrationBuilder.DropColumn(
                name: "SystemLabelsMask",
                table: "Dialog");

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_ServiceResource_Party_ContentUpdatedAt_Id_NotDeleted",
                table: "Dialog",
                columns: new[] { "ServiceResource", "Party", "ContentUpdatedAt", "Id" },
                descending: new[] { false, false, true, true },
                filter: "\"Deleted\" = false")
                .Annotation("Npgsql:IndexInclude", new[] { "StatusId", "VisibleFrom", "ExpiresAt", "IsApiOnly" });
        }
    }
}
