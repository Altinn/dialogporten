using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Sql;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Codifies indexes, storage parameters and planner statistics that were tuned by hand in prod, and pins
    /// n_distinct on foreign key columns whose sampled estimate is off by orders of magnitude (see
    /// Sql/Configuration/ColumnDistinctOverrides.sql).
    ///
    /// Model-only, deliberately left out of Up/Down: the removed CreatedConcurrently annotation on
    /// IX_Dialog_Party_{CreatedAt,UpdatedAt,DueAt}_Id. It does not change the database, and EF would otherwise drop
    /// and rebuild those indexes.
    ///
    /// Prod already has everything this migration creates; it is applied there by hand (see the drift runbook).
    /// </summary>
    public partial class CodifyProdIndicesAndStorageSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MassTransitOutboxState_Created",
                table: "MassTransitOutboxState");

            migrationBuilder.DropIndex(
                name: "IX_MassTransitOutboxMessage_EnqueueTime",
                table: "MassTransitOutboxMessage");

            migrationBuilder.DropIndex(
                name: "IX_MassTransitOutboxMessage_ExpirationTime",
                table: "MassTransitOutboxMessage");

            migrationBuilder.DropIndex(
                name: "IX_MassTransitOutboxMessage_InboxMessageId_InboxConsumerId_Seq~",
                table: "MassTransitOutboxMessage");

            migrationBuilder.DropIndex(
                name: "IX_MassTransitOutboxMessage_OutboxId_SequenceNumber",
                table: "MassTransitOutboxMessage");

            migrationBuilder.DropIndex(
                name: "IX_DialogSeenLog_DialogId",
                table: "DialogSeenLog");

            migrationBuilder.DropIndex(
                name: "IX_Dialog_Id_Covering",
                table: "Dialog");

            migrationBuilder.DropIndex(
                name: "IX_Dialog_Party_ContentUpdatedAt_Id_Covering",
                table: "Dialog");

            migrationBuilder.DropIndex(
                name: "IX_Dialog_ServiceResource_Party_ContentUpdatedAt_Id_NotDeleted",
                table: "Dialog");

            migrationBuilder.AlterTable(
                name: "MassTransitOutboxState")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", 0)
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .Annotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 0)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 1000)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", 0)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 2000)
                .Annotation("Npgsql:StorageParameter:vacuum_index_cleanup", "on");

            migrationBuilder.AlterTable(
                name: "MassTransitOutboxMessage")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", 0)
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .Annotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 0)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 1000)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", 0)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 2000)
                .Annotation("Npgsql:StorageParameter:vacuum_index_cleanup", "on");

            migrationBuilder.AlterTable(
                name: "LocalizationSet")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .Annotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.AlterTable(
                name: "Localization")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .Annotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.AlterTable(
                name: "LabelAssignmentLog")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .Annotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.AlterTable(
                name: "DialogTransmissionContent")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .Annotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.AlterTable(
                name: "DialogTransmission")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .Annotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.AlterTable(
                name: "DialogServiceOwnerLabel")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .Annotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.AlterTable(
                name: "DialogServiceOwnerContext")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .Annotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.AlterTable(
                name: "DialogSeenLog")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .Annotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.AlterTable(
                name: "DialogSearchTag")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .Annotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.AlterTable(
                name: "DialogSearch",
                schema: "search")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.01")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .Annotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 20)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 300)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.05")
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 50000);

            migrationBuilder.AlterTable(
                name: "DialogGuiAction")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .Annotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.AlterTable(
                name: "DialogEndUserContextSystemLabel")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .Annotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.AlterTable(
                name: "DialogEndUserContext")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .Annotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.AlterTable(
                name: "DialogContent")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .Annotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.AlterTable(
                name: "DialogApiActionEndpoint")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .Annotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.AlterTable(
                name: "DialogApiAction")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .Annotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.AlterTable(
                name: "DialogActivity")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .Annotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.AlterTable(
                name: "Dialog")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.01")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .Annotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 5)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 2000)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.01")
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000)
                .Annotation("Npgsql:StorageParameter:parallel_workers", 4);

            migrationBuilder.AlterTable(
                name: "AttachmentUrl")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .Annotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.AlterTable(
                name: "Attachment")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .Annotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.AlterTable(
                name: "Actor")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .Annotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .Annotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.CreateIndex(
                name: "IX_MassTransitOutboxState_Created",
                table: "MassTransitOutboxState",
                column: "Created")
                .Annotation("Npgsql:StorageParameter:fillfactor", 70);

            migrationBuilder.CreateIndex(
                name: "IX_MassTransitOutboxMessage_EnqueueTime",
                table: "MassTransitOutboxMessage",
                column: "EnqueueTime")
                .Annotation("Npgsql:StorageParameter:fillfactor", 70);

            migrationBuilder.CreateIndex(
                name: "IX_MassTransitOutboxMessage_ExpirationTime",
                table: "MassTransitOutboxMessage",
                column: "ExpirationTime")
                .Annotation("Npgsql:StorageParameter:fillfactor", 70);

            migrationBuilder.CreateIndex(
                name: "IX_MassTransitOutboxMessage_InboxMessageId_InboxConsumerId_Seq~",
                table: "MassTransitOutboxMessage",
                columns: new[] { "InboxMessageId", "InboxConsumerId", "SequenceNumber" },
                unique: true)
                .Annotation("Npgsql:StorageParameter:fillfactor", 70);

            migrationBuilder.CreateIndex(
                name: "IX_MassTransitOutboxMessage_OutboxId_SequenceNumber",
                table: "MassTransitOutboxMessage",
                columns: new[] { "OutboxId", "SequenceNumber" },
                unique: true)
                .Annotation("Npgsql:StorageParameter:fillfactor", 70);

            migrationBuilder.CreateIndex(
                name: "IX_DialogServiceOwnerLabel_ContextId",
                table: "DialogServiceOwnerLabel",
                column: "DialogServiceOwnerContextId");

            migrationBuilder.CreateIndex(
                name: "IX_DialogSeenLog_DialogId_CreatedAt",
                table: "DialogSeenLog",
                columns: new[] { "DialogId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_Party_ServiceResource_ContentUpdatedAt_Id_NotDeleted",
                table: "Dialog",
                columns: new[] { "Party", "ServiceResource", "ContentUpdatedAt", "Id" },
                descending: new[] { false, false, true, true },
                filter: "\"Deleted\" = false")
                .Annotation("Npgsql:IndexInclude", new[] { "StatusId", "VisibleFrom", "ExpiresAt", "IsApiOnly", "SystemLabelsMask", "IsSeenSinceLastContentUpdate" });

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_ServiceResource_ContentUpdatedAt_Id_NotDeleted",
                table: "Dialog",
                columns: new[] { "ServiceResource", "ContentUpdatedAt", "Id" },
                descending: new[] { false, true, true },
                filter: "\"Deleted\" = false")
                .Annotation("Npgsql:IndexInclude", new[] { "Party", "StatusId", "VisibleFrom", "ExpiresAt", "IsApiOnly", "SystemLabelsMask", "IsSeenSinceLastContentUpdate" });

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_Id_Covering",
                table: "Dialog",
                column: "Id")
                .Annotation("Npgsql:IndexInclude", new[] { "ServiceResource", "Deleted", "IsApiOnly", "StatusId", "Org", "VisibleFrom", "ExpiresAt", "ContentUpdatedAt", "SystemLabelsMask" });

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_Party_ContentUpdatedAt_Id_Covering",
                table: "Dialog",
                columns: new[] { "Party", "ContentUpdatedAt", "Id" },
                descending: new[] { false, true, true },
                filter: "\"Deleted\" = false")
                .Annotation("Npgsql:IndexInclude", new[] { "ServiceResource", "StatusId", "VisibleFrom", "ExpiresAt", "IsApiOnly", "SystemLabelsMask", "IsSeenSinceLastContentUpdate" });

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_ServiceResource_Party_ContentUpdatedAt_Id_NotDeleted",
                table: "Dialog",
                columns: new[] { "ServiceResource", "Party", "ContentUpdatedAt", "Id" },
                descending: new[] { false, false, true, true },
                filter: "\"Deleted\" = false")
                .Annotation("Npgsql:IndexInclude", new[] { "StatusId", "VisibleFrom", "ExpiresAt", "IsApiOnly", "SystemLabelsMask", "IsSeenSinceLastContentUpdate" });

            var scripts = new[]
            {
                "Configuration/TableAutoVacuum.sql",
                "Configuration/PlannerStatistics.sql",
                "Configuration/ColumnDistinctOverrides.sql"
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
                ALTER TABLE public."Localization" ALTER COLUMN "LocalizationSetId" RESET (n_distinct);
                ALTER TABLE public."DialogTransmissionContent" ALTER COLUMN "TransmissionId" RESET (n_distinct);
                ALTER TABLE public."DialogContent" ALTER COLUMN "DialogId" RESET (n_distinct);
                ALTER TABLE public."DialogActivity" ALTER COLUMN "DialogId" RESET (n_distinct);
                ALTER TABLE public."DialogApiAction" ALTER COLUMN "DialogId" RESET (n_distinct);
                ALTER TABLE public."DialogGuiAction" ALTER COLUMN "DialogId" RESET (n_distinct);
                ALTER TABLE public."DialogSearchTag" ALTER COLUMN "DialogId" RESET (n_distinct);
                ALTER TABLE public."LabelAssignmentLog" ALTER COLUMN "ContextId" RESET (n_distinct);
                ALTER TABLE public."Attachment" ALTER COLUMN "DialogId" RESET (n_distinct);
                ALTER TABLE public."Attachment" ALTER COLUMN "TransmissionId" RESET (n_distinct);
                DROP STATISTICS IF EXISTS public."STATS_Dialog_Party_ServiceResource_MCV";
                ALTER TABLE public."Dialog" ALTER COLUMN "Party" SET STATISTICS -1;
                ALTER TABLE public."Dialog" ALTER COLUMN "ServiceResource" SET STATISTICS -1;
                ALTER TABLE public."DialogEndUserContextSystemLabel" ALTER COLUMN "DialogEndUserContextId" SET STATISTICS -1;
                ALTER TABLE public."DialogGuiAction" ALTER COLUMN "DialogId" SET STATISTICS -1;
                ALTER TABLE public."LocalizationSet" ALTER COLUMN "DialogGuiActionPrompt_GuiActionId" SET STATISTICS -1;
                ALTER TABLE search."DialogSearchRebuildQueue" RESET (autovacuum_enabled, autovacuum_vacuum_scale_factor,
                    autovacuum_vacuum_threshold, autovacuum_analyze_scale_factor, autovacuum_analyze_threshold,
                    autovacuum_vacuum_cost_limit, autovacuum_vacuum_cost_delay);
                ALTER TABLE partyresource."PartyResource" RESET (autovacuum_vacuum_scale_factor,
                    autovacuum_vacuum_insert_scale_factor);
                """);

            migrationBuilder.DropIndex(
                name: "IX_Dialog_Id_Covering",
                table: "Dialog");

            migrationBuilder.DropIndex(
                name: "IX_Dialog_Party_ContentUpdatedAt_Id_Covering",
                table: "Dialog");

            migrationBuilder.DropIndex(
                name: "IX_Dialog_ServiceResource_Party_ContentUpdatedAt_Id_NotDeleted",
                table: "Dialog");

            migrationBuilder.DropIndex(
                name: "IX_MassTransitOutboxState_Created",
                table: "MassTransitOutboxState");

            migrationBuilder.DropIndex(
                name: "IX_MassTransitOutboxMessage_EnqueueTime",
                table: "MassTransitOutboxMessage");

            migrationBuilder.DropIndex(
                name: "IX_MassTransitOutboxMessage_ExpirationTime",
                table: "MassTransitOutboxMessage");

            migrationBuilder.DropIndex(
                name: "IX_MassTransitOutboxMessage_InboxMessageId_InboxConsumerId_Seq~",
                table: "MassTransitOutboxMessage");

            migrationBuilder.DropIndex(
                name: "IX_MassTransitOutboxMessage_OutboxId_SequenceNumber",
                table: "MassTransitOutboxMessage");

            migrationBuilder.DropIndex(
                name: "IX_DialogServiceOwnerLabel_ContextId",
                table: "DialogServiceOwnerLabel");

            migrationBuilder.DropIndex(
                name: "IX_DialogSeenLog_DialogId_CreatedAt",
                table: "DialogSeenLog");

            migrationBuilder.DropIndex(
                name: "IX_Dialog_Party_ServiceResource_ContentUpdatedAt_Id_NotDeleted",
                table: "Dialog");

            migrationBuilder.DropIndex(
                name: "IX_Dialog_ServiceResource_ContentUpdatedAt_Id_NotDeleted",
                table: "Dialog");

            migrationBuilder.AlterTable(
                name: "MassTransitOutboxState")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", 0)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 0)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 1000)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", 0)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 2000)
                .OldAnnotation("Npgsql:StorageParameter:vacuum_index_cleanup", "on");

            migrationBuilder.AlterTable(
                name: "MassTransitOutboxMessage")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", 0)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 0)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 1000)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", 0)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 2000)
                .OldAnnotation("Npgsql:StorageParameter:vacuum_index_cleanup", "on");

            migrationBuilder.AlterTable(
                name: "LocalizationSet")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.AlterTable(
                name: "Localization")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.AlterTable(
                name: "LabelAssignmentLog")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.AlterTable(
                name: "DialogTransmissionContent")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.AlterTable(
                name: "DialogTransmission")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.AlterTable(
                name: "DialogServiceOwnerLabel")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.AlterTable(
                name: "DialogServiceOwnerContext")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.AlterTable(
                name: "DialogSeenLog")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.AlterTable(
                name: "DialogSearchTag")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.AlterTable(
                name: "DialogSearch",
                schema: "search")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.01")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 20)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 300)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.05")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 50000);

            migrationBuilder.AlterTable(
                name: "DialogGuiAction")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.AlterTable(
                name: "DialogEndUserContextSystemLabel")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.AlterTable(
                name: "DialogEndUserContext")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.AlterTable(
                name: "DialogContent")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.AlterTable(
                name: "DialogApiActionEndpoint")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.AlterTable(
                name: "DialogApiAction")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.AlterTable(
                name: "DialogActivity")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.AlterTable(
                name: "Dialog")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.01")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 5)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 2000)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.01")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000)
                .OldAnnotation("Npgsql:StorageParameter:parallel_workers", 4);

            migrationBuilder.AlterTable(
                name: "AttachmentUrl")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.AlterTable(
                name: "Attachment")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.AlterTable(
                name: "Actor")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_analyze_threshold", 500)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_enabled", true)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_delay", 2)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_cost_limit", 200)
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_scale_factor", "0.005")
                .OldAnnotation("Npgsql:StorageParameter:autovacuum_vacuum_threshold", 1000);

            migrationBuilder.CreateIndex(
                name: "IX_MassTransitOutboxState_Created",
                table: "MassTransitOutboxState",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_MassTransitOutboxMessage_EnqueueTime",
                table: "MassTransitOutboxMessage",
                column: "EnqueueTime");

            migrationBuilder.CreateIndex(
                name: "IX_MassTransitOutboxMessage_ExpirationTime",
                table: "MassTransitOutboxMessage",
                column: "ExpirationTime");

            migrationBuilder.CreateIndex(
                name: "IX_MassTransitOutboxMessage_InboxMessageId_InboxConsumerId_Seq~",
                table: "MassTransitOutboxMessage",
                columns: new[] { "InboxMessageId", "InboxConsumerId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MassTransitOutboxMessage_OutboxId_SequenceNumber",
                table: "MassTransitOutboxMessage",
                columns: new[] { "OutboxId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DialogSeenLog_DialogId",
                table: "DialogSeenLog",
                column: "DialogId");

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_Id_Covering",
                table: "Dialog",
                column: "Id")
                .Annotation("Npgsql:IndexInclude", new[] { "ServiceResource", "Deleted", "IsApiOnly", "StatusId", "Org", "VisibleFrom", "ExpiresAt", "ContentUpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_Party_ContentUpdatedAt_Id_Covering",
                table: "Dialog",
                columns: new[] { "Party", "ContentUpdatedAt", "Id" },
                descending: new[] { false, true, true },
                filter: "\"Deleted\" = false")
                .Annotation("Npgsql:IndexInclude", new[] { "ServiceResource", "IsApiOnly", "StatusId", "Org", "VisibleFrom", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_ServiceResource_Party_ContentUpdatedAt_Id_NotDeleted",
                table: "Dialog",
                columns: new[] { "ServiceResource", "Party", "ContentUpdatedAt", "Id" },
                descending: new[] { false, false, true, true },
                filter: "\"Deleted\" = false")
                .Annotation("Npgsql:IndexInclude", new[] { "StatusId", "VisibleFrom", "ExpiresAt", "IsApiOnly", "SystemLabelsMask" });
        }
    }
}
