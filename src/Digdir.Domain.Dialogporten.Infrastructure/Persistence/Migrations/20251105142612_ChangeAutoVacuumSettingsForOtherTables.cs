using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Sql;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChangeAutoVacuumSettingsForOtherTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var scripts = new[]
            {
                "Configuration/WriteHeavyTablesAutoVacuum.sql"
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
                ALTER TABLE public."Actor"                       RESET (
                  autovacuum_enabled,
                  autovacuum_vacuum_scale_factor,
                  autovacuum_vacuum_threshold,
                  autovacuum_analyze_scale_factor,
                  autovacuum_analyze_threshold,
                  autovacuum_vacuum_cost_limit,
                  autovacuum_vacuum_cost_delay
                );
                
                ALTER TABLE public."Attachment"                  RESET (
                  autovacuum_enabled,
                  autovacuum_vacuum_scale_factor,
                  autovacuum_vacuum_threshold,
                  autovacuum_analyze_scale_factor,
                  autovacuum_analyze_threshold,
                  autovacuum_vacuum_cost_limit,
                  autovacuum_vacuum_cost_delay
                );
                
                ALTER TABLE public."AttachmentUrl"               RESET (
                  autovacuum_enabled,
                  autovacuum_vacuum_scale_factor,
                  autovacuum_vacuum_threshold,
                  autovacuum_analyze_scale_factor,
                  autovacuum_analyze_threshold,
                  autovacuum_vacuum_cost_limit,
                  autovacuum_vacuum_cost_delay
                );
                
                ALTER TABLE public."Dialog"                      RESET (
                  autovacuum_enabled,
                  autovacuum_vacuum_scale_factor,
                  autovacuum_vacuum_threshold,
                  autovacuum_analyze_scale_factor,
                  autovacuum_analyze_threshold,
                  autovacuum_vacuum_cost_limit,
                  autovacuum_vacuum_cost_delay
                );
                
                ALTER TABLE public."DialogActivity"              RESET (
                  autovacuum_enabled,
                  autovacuum_vacuum_scale_factor,
                  autovacuum_vacuum_threshold,
                  autovacuum_analyze_scale_factor,
                  autovacuum_analyze_threshold,
                  autovacuum_vacuum_cost_limit,
                  autovacuum_vacuum_cost_delay
                );
                
                ALTER TABLE public."DialogApiAction"             RESET (
                  autovacuum_enabled,
                  autovacuum_vacuum_scale_factor,
                  autovacuum_vacuum_threshold,
                  autovacuum_analyze_scale_factor,
                  autovacuum_analyze_threshold,
                  autovacuum_vacuum_cost_limit,
                  autovacuum_vacuum_cost_delay
                );
                
                ALTER TABLE public."DialogApiActionEndpoint"     RESET (
                  autovacuum_enabled,
                  autovacuum_vacuum_scale_factor,
                  autovacuum_vacuum_threshold,
                  autovacuum_analyze_scale_factor,
                  autovacuum_analyze_threshold,
                  autovacuum_vacuum_cost_limit,
                  autovacuum_vacuum_cost_delay
                );
                
                ALTER TABLE public."DialogContent"               RESET (
                  autovacuum_enabled,
                  autovacuum_vacuum_scale_factor,
                  autovacuum_vacuum_threshold,
                  autovacuum_analyze_scale_factor,
                  autovacuum_analyze_threshold,
                  autovacuum_vacuum_cost_limit,
                  autovacuum_vacuum_cost_delay
                );
                
                ALTER TABLE public."DialogEndUserContext"        RESET (
                  autovacuum_enabled,
                  autovacuum_vacuum_scale_factor,
                  autovacuum_vacuum_threshold,
                  autovacuum_analyze_scale_factor,
                  autovacuum_analyze_threshold,
                  autovacuum_vacuum_cost_limit,
                  autovacuum_vacuum_cost_delay
                );
                
                ALTER TABLE public."DialogEndUserContextSystemLabel" RESET (
                  autovacuum_enabled,
                  autovacuum_vacuum_scale_factor,
                  autovacuum_vacuum_threshold,
                  autovacuum_analyze_scale_factor,
                  autovacuum_analyze_threshold,
                  autovacuum_vacuum_cost_limit,
                  autovacuum_vacuum_cost_delay
                );
                
                ALTER TABLE public."DialogGuiAction"             RESET (
                  autovacuum_enabled,
                  autovacuum_vacuum_scale_factor,
                  autovacuum_vacuum_threshold,
                  autovacuum_analyze_scale_factor,
                  autovacuum_analyze_threshold,
                  autovacuum_vacuum_cost_limit,
                  autovacuum_vacuum_cost_delay
                );
                
                ALTER TABLE public."DialogSearchTag"             RESET (
                  autovacuum_enabled,
                  autovacuum_vacuum_scale_factor,
                  autovacuum_vacuum_threshold,
                  autovacuum_analyze_scale_factor,
                  autovacuum_analyze_threshold,
                  autovacuum_vacuum_cost_limit,
                  autovacuum_vacuum_cost_delay
                );
                
                ALTER TABLE public."DialogSeenLog"               RESET (
                  autovacuum_enabled,
                  autovacuum_vacuum_scale_factor,
                  autovacuum_vacuum_threshold,
                  autovacuum_analyze_scale_factor,
                  autovacuum_analyze_threshold,
                  autovacuum_vacuum_cost_limit,
                  autovacuum_vacuum_cost_delay
                );
                
                ALTER TABLE public."DialogServiceOwnerContext"   RESET (
                  autovacuum_enabled,
                  autovacuum_vacuum_scale_factor,
                  autovacuum_vacuum_threshold,
                  autovacuum_analyze_scale_factor,
                  autovacuum_analyze_threshold,
                  autovacuum_vacuum_cost_limit,
                  autovacuum_vacuum_cost_delay
                );
                
                ALTER TABLE public."DialogServiceOwnerLabel"     RESET (
                  autovacuum_enabled,
                  autovacuum_vacuum_scale_factor,
                  autovacuum_vacuum_threshold,
                  autovacuum_analyze_scale_factor,
                  autovacuum_analyze_threshold,
                  autovacuum_vacuum_cost_limit,
                  autovacuum_vacuum_cost_delay
                );
                
                ALTER TABLE public."DialogTransmission"          RESET (
                  autovacuum_enabled,
                  autovacuum_vacuum_scale_factor,
                  autovacuum_vacuum_threshold,
                  autovacuum_analyze_scale_factor,
                  autovacuum_analyze_threshold,
                  autovacuum_vacuum_cost_limit,
                  autovacuum_vacuum_cost_delay
                );
                
                ALTER TABLE public."DialogTransmissionContent"   RESET (
                  autovacuum_enabled,
                  autovacuum_vacuum_scale_factor,
                  autovacuum_vacuum_threshold,
                  autovacuum_analyze_scale_factor,
                  autovacuum_analyze_threshold,
                  autovacuum_vacuum_cost_limit,
                  autovacuum_vacuum_cost_delay
                );
                
                ALTER TABLE public."LabelAssignmentLog"          RESET (
                  autovacuum_enabled,
                  autovacuum_vacuum_scale_factor,
                  autovacuum_vacuum_threshold,
                  autovacuum_analyze_scale_factor,
                  autovacuum_analyze_threshold,
                  autovacuum_vacuum_cost_limit,
                  autovacuum_vacuum_cost_delay
                );
                
                ALTER TABLE public."Localization"                RESET (
                  autovacuum_enabled,
                  autovacuum_vacuum_scale_factor,
                  autovacuum_vacuum_threshold,
                  autovacuum_analyze_scale_factor,
                  autovacuum_analyze_threshold,
                  autovacuum_vacuum_cost_limit,
                  autovacuum_vacuum_cost_delay
                );
                
                ALTER TABLE public."LocalizationSet"             RESET (
                  autovacuum_enabled,
                  autovacuum_vacuum_scale_factor,
                  autovacuum_vacuum_threshold,
                  autovacuum_analyze_scale_factor,
                  autovacuum_analyze_threshold,
                  autovacuum_vacuum_cost_limit,
                  autovacuum_vacuum_cost_delay
                );
                
                ALTER TABLE search."DialogSearch"                RESET (
                  autovacuum_enabled,
                  autovacuum_vacuum_scale_factor,
                  autovacuum_vacuum_threshold,
                  autovacuum_analyze_scale_factor,
                  autovacuum_analyze_threshold,
                  autovacuum_vacuum_cost_limit,
                  autovacuum_vacuum_cost_delay
                );
                
                ALTER TABLE search."DialogSearchRebuildQueue"    RESET (
                  autovacuum_enabled,
                  autovacuum_vacuum_scale_factor,
                  autovacuum_vacuum_threshold,
                  autovacuum_analyze_scale_factor,
                  autovacuum_analyze_threshold,
                  autovacuum_vacuum_cost_limit,
                  autovacuum_vacuum_cost_delay
                );
                """);
        }
     }
 }
