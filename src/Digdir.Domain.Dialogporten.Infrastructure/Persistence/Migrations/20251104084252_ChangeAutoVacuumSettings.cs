using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Sql;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChangeAutoVacuumSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var scripts = new[]
            {
                "Configuration/MassTransitAutoVacuum.sql"
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
                 ALTER TABLE public."MassTransitOutboxMessage" RESET (
                   autovacuum_enabled,
                   autovacuum_vacuum_scale_factor,
                   autovacuum_vacuum_threshold,
                   autovacuum_analyze_scale_factor,
                   autovacuum_analyze_threshold,
                   autovacuum_vacuum_cost_limit,
                   autovacuum_vacuum_cost_delay,
                   vacuum_index_cleanup
                 );
                 
                 ALTER TABLE public."MassTransitOutboxState" RESET (
                   autovacuum_enabled,
                   autovacuum_vacuum_scale_factor,
                   autovacuum_vacuum_threshold,
                   autovacuum_analyze_scale_factor,
                   autovacuum_analyze_threshold,
                   autovacuum_vacuum_cost_limit,
                   autovacuum_vacuum_cost_delay,
                   vacuum_index_cleanup
                 );                

                 ALTER TABLE public."MassTransitOutboxMessage"
                   SET (vacuum_index_cleanup = auto);
                 
                 ALTER TABLE public."MassTransitOutboxState"
                   SET (vacuum_index_cleanup = auto);
                 """);
        }
    }
}
