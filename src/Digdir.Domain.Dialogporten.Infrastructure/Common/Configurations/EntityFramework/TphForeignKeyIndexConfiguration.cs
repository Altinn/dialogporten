using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Digdir.Domain.Dialogporten.Infrastructure.Common.Configurations.EntityFramework;

/// <summary>
/// Configures partial indexes for single-column foreign keys on TPH hierarchies.
/// Rationale:
/// - TPH tables are sparse by design; each derived type typically uses only a subset of FK columns.
///   Partial indexes (<c>IS NOT NULL</c>) therefore avoid indexing large NULL-heavy portions of the table,
///   which gives substantial index size savings and lower write amplification.
/// - Keep indexes as slim as possible (FK-only) to maximize cache residency and reduce I/O on large tables.
/// </summary>
internal static class TphForeignKeyIndexConfiguration
{
    public static ModelBuilder ConfigureTphForeignKeyIndexes<TBase>(
        this ModelBuilder modelBuilder)
        where TBase : class
    {
        var entityType = modelBuilder.Model.FindEntityType(typeof(TBase));
        if (entityType is null)
        {
            return modelBuilder;
        }

        if (entityType.FindDiscriminatorProperty() is null)
        {
            return modelBuilder;
        }

        var indexTargets = entityType
            .GetDerivedTypes()
            // Include the TPH base type itself because it may declare FKs/indexes
            // on shared-table columns (for this model: ActorNameEntityId and ActorTypeId on Actor).
            .Prepend(entityType)
            .SelectMany(derivedType =>
            {
                var table = StoreObjectIdentifier.Create(derivedType, StoreObjectType.Table);
                if (table is null)
                {
                    return [];
                }

                return derivedType
                    .GetForeignKeys()
                    // Only keep FKs declared directly on this CLR type and only single-column FKs.
                    // This avoids re-processing inherited FKs from the base type and skips composite
                    // FKs that don't map to the one-column index pattern used here.
                    .Where(foreignKey => foreignKey.DeclaringEntityType == derivedType && foreignKey.Properties.Count == 1)
                    .Select(foreignKey =>
                    {
                        var property = foreignKey.Properties[0];
                        return new
                        {
                            EntityType = derivedType,
                            Property = property,
                            ColumnName = property.GetColumnName(table.Value),
                            TableName = table.Value.Name,
                            foreignKey.IsUnique,
                        };
                    });
            })
            .Where(x => x.ColumnName is not null)
            .ToList();

        var resolvedColumnNames = indexTargets
            .ToDictionary(
                target => (target.EntityType.Name, target.Property.Name),
                target => target.ColumnName!);

        // In shared-table TPH, multiple CLR types can expose the same FK property name
        // while mapping to different physical columns (e.g. GuiActionId vs DialogGuiActionPrompt_GuiActionId).
        // Resolve those collisions deterministically so index names/filters target the correct column.
        var duplicateGroups = indexTargets
            .GroupBy(x => (x.TableName, x.Property.Name))
            .Where(group => group.Count() > 1)
            .ToList();

        foreach (var group in duplicateGroups)
        {
            var orderedTargets = group
                .OrderBy(x => x.EntityType.Name, StringComparer.Ordinal)
                .ToList();

            var defaultColumnTarget = orderedTargets[^1];

            foreach (var target in orderedTargets)
            {
                // Keep one mapping aligned with EF's default property->column convention and
                // prefix the others with CLR type name to match TPH-disambiguated column names.
                var resolvedColumnName = ReferenceEquals(target, defaultColumnTarget)
                    ? target.Property.Name
                    : $"{target.EntityType.ClrType.Name}_{target.Property.Name}";

                resolvedColumnNames[(target.EntityType.Name, target.Property.Name)] = resolvedColumnName;
            }
        }

        foreach (var target in indexTargets)
        {
            var resolvedColumnName = resolvedColumnNames[(target.EntityType.Name, target.Property.Name)];

            var entityBuilder = modelBuilder.Entity(target.EntityType.ClrType);
            entityBuilder
                .Property(target.Property.Name)
                .HasColumnName(resolvedColumnName);

            entityBuilder
                .HasIndex(target.Property.Name)
                // Build index metadata from the resolved physical column name to keep
                // index name/filter synchronized with the actual table shape.
                .HasDatabaseName($"IX_{target.TableName}_{resolvedColumnName}")
                .HasFilter($"\"{resolvedColumnName}\" IS NOT NULL")
                .IsUnique(target.IsUnique);
        }

        return modelBuilder;
    }
}
