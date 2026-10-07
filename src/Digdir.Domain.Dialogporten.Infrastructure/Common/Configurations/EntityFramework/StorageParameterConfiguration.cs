using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Digdir.Domain.Dialogporten.Infrastructure.Common.Configurations.EntityFramework;

/// <summary>
/// Per-table autovacuum and per-index fillfactor storage parameters, as tuned in prod.
/// Rationale:
/// - The child tables vacuum and analyze after a small fraction of churn (0.5%), but are throttled
///   (cost limit 200) well below the server-wide autovacuum_vacuum_cost_limit set in IaC, so that
///   the largest tables cannot starve the autovacuum workers.
/// - The MassTransit outbox tables are small and churn constantly: vacuum on a fixed row threshold,
///   unthrottled, with room left on index pages for the insert/delete cycle.
/// Tables that EF does not map (partyresource."PartyResource", search."DialogSearchRebuildQueue")
/// are configured in Sql/Configuration/TableAutoVacuum.sql.
/// </summary>
internal static class StorageParameterConfiguration
{
    // Fractional values are strings on purpose: Npgsql formats double storage parameters with the
    // current culture, so a machine with a comma decimal separator (e.g. nb-NO) would generate
    // "autovacuum_vacuum_scale_factor=0,005", which is a syntax error. Strings are emitted verbatim.

    private static readonly Dictionary<string, object> ChildTable = new()
    {
        ["autovacuum_enabled"] = true,
        ["autovacuum_vacuum_scale_factor"] = "0.005",
        ["autovacuum_vacuum_threshold"] = 1000,
        ["autovacuum_analyze_scale_factor"] = "0.005",
        ["autovacuum_analyze_threshold"] = 500,
        ["autovacuum_vacuum_cost_limit"] = 200,
        ["autovacuum_vacuum_cost_delay"] = 2
    };

    private static readonly Dictionary<string, object> MassTransitOutboxTable = new()
    {
        ["autovacuum_enabled"] = true,
        ["autovacuum_vacuum_scale_factor"] = 0,
        ["autovacuum_vacuum_threshold"] = 2000,
        ["autovacuum_analyze_scale_factor"] = 0,
        ["autovacuum_analyze_threshold"] = 500,
        ["autovacuum_vacuum_cost_limit"] = 1000,
        ["autovacuum_vacuum_cost_delay"] = 0,
        ["vacuum_index_cleanup"] = "on"
    };

    private static readonly Dictionary<string, Dictionary<string, object>> TableParameters = new()
    {
        ["Actor"] = ChildTable,
        ["Attachment"] = ChildTable,
        ["AttachmentUrl"] = ChildTable,
        ["DialogActivity"] = ChildTable,
        ["DialogApiAction"] = ChildTable,
        ["DialogApiActionEndpoint"] = ChildTable,
        ["DialogContent"] = ChildTable,
        ["DialogEndUserContext"] = ChildTable,
        ["DialogEndUserContextSystemLabel"] = ChildTable,
        ["DialogGuiAction"] = ChildTable,
        ["DialogSearchTag"] = ChildTable,
        ["DialogSeenLog"] = ChildTable,
        ["DialogServiceOwnerContext"] = ChildTable,
        ["DialogServiceOwnerLabel"] = ChildTable,
        ["DialogTransmission"] = ChildTable,
        ["DialogTransmissionContent"] = ChildTable,
        ["LabelAssignmentLog"] = ChildTable,
        ["Localization"] = ChildTable,
        ["LocalizationSet"] = ChildTable,
        ["Dialog"] = new()
        {
            ["autovacuum_enabled"] = true,
            ["autovacuum_vacuum_scale_factor"] = "0.01",
            ["autovacuum_vacuum_threshold"] = 1000,
            ["autovacuum_analyze_scale_factor"] = "0.01",
            ["autovacuum_analyze_threshold"] = 500,
            ["autovacuum_vacuum_cost_limit"] = 2000,
            ["autovacuum_vacuum_cost_delay"] = 5,
            ["parallel_workers"] = 4
        },
        ["DialogSearch"] = new()
        {
            ["autovacuum_enabled"] = true,
            ["autovacuum_vacuum_scale_factor"] = "0.05",
            ["autovacuum_vacuum_threshold"] = 50000,
            ["autovacuum_analyze_scale_factor"] = "0.01",
            ["autovacuum_analyze_threshold"] = 500,
            ["autovacuum_vacuum_cost_limit"] = 300,
            ["autovacuum_vacuum_cost_delay"] = 20
        },
        ["MassTransitOutboxMessage"] = MassTransitOutboxTable,
        ["MassTransitOutboxState"] = MassTransitOutboxTable
    };

    private static readonly string[] FillFactor70Indexes =
    [
        "IX_MassTransitOutboxMessage_EnqueueTime",
        "IX_MassTransitOutboxMessage_ExpirationTime",
        "IX_MassTransitOutboxMessage_InboxMessageId_InboxConsumerId_Seq~",
        "IX_MassTransitOutboxMessage_OutboxId_SequenceNumber",
        "IX_MassTransitOutboxState_Created"
    ];

    /// <summary>
    /// Must run after every entity is registered, including the MassTransit outbox entities.
    /// </summary>
    public static ModelBuilder ConfigureStorageParameters(this ModelBuilder modelBuilder)
    {
        // Storage parameters belong to the table, so set them on the root of each (TPH) hierarchy.
        var rootEntityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(x => x.BaseType is null && x.GetTableName() is not null)
            .ToList();

        foreach (var (tableName, parameters) in TableParameters)
        {
            var entityType = rootEntityTypes.SingleOrDefault(x => x.GetTableName() == tableName)
                ?? throw new InvalidOperationException($"No entity type is mapped to table '{tableName}'.");

            foreach (var (name, value) in parameters)
            {
                entityType.SetStorageParameter(name, value);
            }
        }

        var indexes = rootEntityTypes
            .SelectMany(x => x.GetIndexes())
            .ToDictionary(x => x.GetDatabaseName() ?? string.Empty);

        foreach (var indexName in FillFactor70Indexes)
        {
            if (!indexes.TryGetValue(indexName, out var index))
            {
                throw new InvalidOperationException($"No index named '{indexName}' is in the model.");
            }

            index.SetStorageParameter("fillfactor", 70);
        }

        return modelBuilder;
    }
}
