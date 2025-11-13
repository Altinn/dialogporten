using System.Diagnostics;
using System.Reflection;
using Digdir.Library.Entity.Abstractions.Features.Aggregate;
using Digdir.Library.Entity.Abstractions.Features.Updatable;
using Digdir.Library.Entity.Abstractions.Features.Versionable;
using Digdir.Library.Entity.EntityFrameworkCore.Features.SoftDeletable;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Digdir.Library.Entity.EntityFrameworkCore.Features.Aggregate;

internal static class AggregateExtensions
{
    private static readonly EntityEntryComparer _entityEntryComparer = new();

    internal static async Task<ChangeTracker> HandleAggregateEntities(this ChangeTracker changeTracker,
        DateTimeOffset utcNow, IEntityOptions options, CancellationToken cancellationToken)
    {
        var aggregateNodeByEntry = await changeTracker
            .Entries()
            .Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .GetAggregateNodeByEntry(cancellationToken);

        foreach (var (_, aggregateNode) in aggregateNodeByEntry)
        {
            if (options.EnableAggregateFilter && aggregateNode.Entity is IAggregateCreatedHandler created && aggregateNode.IsAdded())
            {
                created.OnCreate(aggregateNode, utcNow);
            }

            if (options.EnableAggregateFilter && aggregateNode.Entity is IAggregateUpdatedHandler updated && aggregateNode.IsModified())
            {
                updated.OnUpdate(aggregateNode, utcNow, options.EnableUpdatableFilter);
            }

            if (options.EnableAggregateFilter && aggregateNode.Entity is IAggregateDeletedHandler deleted && aggregateNode.IsDeleted())
            {
                deleted.OnDelete(aggregateNode, utcNow);
            }

            if (options.EnableAggregateFilter && aggregateNode.Entity is IAggregateRestoredHandler restored && aggregateNode.IsRestored())
            {
                restored.OnRestore(aggregateNode, utcNow);
            }

            if (options.EnableUpdatableFilter && aggregateNode.Entity is IUpdateableEntity updatable)
            {
                if (aggregateNode.IsModified() || aggregateNode.IsAddedWithDefaultUpdatedAt(updatable))
                {
                    updatable.Update(utcNow);
                }
            }

            if (options.EnableVersionableFilter && aggregateNode.Entity is IVersionableEntity versionable)
            {
                versionable.NewVersion();
            }
        }

        return changeTracker;
    }

    internal static ModelBuilder AddAggregateEntities(this ModelBuilder modelBuilder)
    {
        // This will eager load entire aggregate trees for ALL queries unless explicitly
        // opted out for through ".IgnoreAutoIncludes()". Optimally we want to lazy load
        // the entire tree ONLY when the service owner is altering them, not on every query.
        // In addition, we don't want the ISoftDeletableEntity query filter to be
        // applied when loading aggregates, but it will be through FindAsync and
        // NavProp.LoadAsync.

        //var entities = modelBuilder.Model
        //    .GetEntityTypes()
        //    .Where(x => x.BaseType is null)
        //    .SelectMany(entityType =>
        //    {
        //        var children = entityType
        //            .FindAggregateChildren()
        //            .Select(foreignKey => entityType
        //                .FindNavigation(foreignKey.PrincipalToDependent!.Name)!);
        //        var parents = entityType
        //            .FindAggregateParents()
        //            .Select(foreignKey => entityType
        //                .FindNavigation(foreignKey.DependentToPrincipal!.Name)!);
        //        return children.Concat(parents);
        //    });

        //foreach (var entityType in entities)
        //{
        //    entityType.SetIsEagerLoaded(true);
        //}

        return modelBuilder;
    }

    private static async Task<IReadOnlyDictionary<EntityEntry, AggregateNode>> GetAggregateNodeByEntry(
        this IEnumerable<EntityEntry> entries,
        CancellationToken cancellationToken)
    {
        var nodeByEntry = new Dictionary<EntityEntry, AggregateNode>(comparer: _entityEntryComparer);

        foreach (var entry in entries)
        {
            await nodeByEntry.AddAggregateChain(entry, cancellationToken);
        }

        foreach (var rootNode in nodeByEntry
            .Select(x => x.Value)
            .Where(x => x.IsRootNode))
        {
            rootNode.CollapseAggregateState();
        }

        return nodeByEntry;
    }

    /// <summary>
    /// Will aggregate modified state from children to parent, and deleted/restored state from parent to children. Deleted/restored state from parent to children will take precedence.
    /// </summary>
    /// <param name="node">The AggregateNode whose state is to be collapsed.</param>
    /// <param name="parentState"></param>
    /// <returns>
    ///     <c>true</c> if the node is changed - that is, the node's state is not <see cref="AggregateNodeState.Unchanged"/> after collapsing;
    ///     otherwise, <c>false</c>.
    /// </returns>
    private static bool CollapseAggregateState(this AggregateNode node, AggregateNodeState? parentState = null)
    {
        if (parentState is AggregateNodeState.Deleted)
        {
            node.DeletedByParent = true;
            node.State = AggregateNodeState.Deleted;
        }

        if (parentState is AggregateNodeState.Restored)
        {
            node.State = AggregateNodeState.Restored;
        }

        var childrenIsModified = node.Children
            .Aggregate(false, (previousChildIsModified, childNode) =>
                childNode.CollapseAggregateState(node.State) || previousChildIsModified);

        if (node.State is AggregateNodeState.Unchanged && childrenIsModified)
        {
            node.ModifiedByChild = true;
            node.State = AggregateNodeState.Modified;
        }

        return node.State is not AggregateNodeState.Unchanged;
    }

    private static async Task<AggregateNode> AddAggregateChain(
        this Dictionary<EntityEntry, AggregateNode> nodeByEntry,
        EntityEntry entry,
        CancellationToken cancellationToken)
    {
        if (nodeByEntry.TryGetValue(entry, out var node))
        {
            // We have already added this entry, so just return the node without expanding its parents or children.
            return node;
        }

        nodeByEntry[entry] = node = entry.ToAggregateNode();
        await nodeByEntry.AddAggregateParentChain(entry, cancellationToken);

        // This is not needed for now, as we don't need to send Deleted events for
        // any aggregate children (as we did for the old DialogElement concept
        // await nodeByEntry.AddAggregateChildChain(entry, cancellationToken);
        return node;
    }

    private static async Task AddAggregateParentChain(
        this Dictionary<EntityEntry, AggregateNode> nodeByEntry,
        EntityEntry childEntry,
        CancellationToken cancellationToken)
    {
        if (!nodeByEntry.TryGetValue(childEntry, out var childNode))
        {
            throw new InvalidOperationException("Node must be added before calling this method.");
        }

        foreach (var parentForeignKey in childEntry.Metadata.FindAggregateParents())
        {
            // Supports only dependent to principal. That is - one-to-one and one-to-many
            // relationships. Many-to-many relationships are not supported.
            var parentType = parentForeignKey.PrincipalEntityType.ClrType;

            var parentPrimaryKey = parentForeignKey
                .Properties
                .Select(key => childEntry.OriginalValues[key.Name])
                .ToArray();

            if (parentPrimaryKey.Length == 0 || parentPrimaryKey.Any(x => x is null))
            {
                throw new UnreachableException(
                    $"Foreign key to {parentType.Name} from {childEntry.Metadata.ClrType.Name} " +
                    $"is empty or contains null values.");
            }

            // Adding a new entity with a custom primary key and hitting this error? Here's the deal:
            // EF checks the key to determine if it's new or existing. If the key is set, EF thinks
            // it's existing and marks it as Modified. To add it properly, manually set
            // childEntry.State = EntityState.Added. This can be done through DbContext.Add(entity).
            var parentEntity = await childEntry.Context
                .FindAsync(parentType, parentPrimaryKey, cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Could not find parent {parentType.Name} on {childEntry.Metadata.ClrType.Name} " +
                    $"with key [{string.Join(",", parentPrimaryKey)}].");

            var parentEntry = childEntry.Context.Entry(parentEntity);
            var parentNode = await nodeByEntry.AddAggregateChain(parentEntry, cancellationToken);
            parentNode.AddChild(childNode);
            childNode.AddParent(parentNode);
        }
    }

    private static async Task AddAggregateChildChain(
        this Dictionary<EntityEntry, AggregateNode> nodeByEntry,
        EntityEntry parentEntry,
        CancellationToken cancellationToken)
    {
        if (!nodeByEntry.TryGetValue(parentEntry, out var parentNode))
        {
            throw new InvalidOperationException("Node must be added before calling this method.");
        }

        foreach (var childForeignKey in parentEntry.Metadata.FindAggregateChildren())
        {
            var childNav = parentEntry.Navigation(childForeignKey.PrincipalToDependent!.Name);
            if (!childNav.IsLoaded)
            {
                throw new InvalidOperationException(
                    $"Aggregate child navigation property {childNav.Metadata.Name} on {parentEntry.Metadata.Name} is not loaded. " +
                    $"The whole aggregate tree must be loaded before saving.");
            }

            var currentValues = childNav.Metadata.IsCollection
                ? childNav.CurrentValue as IEnumerable<object> ?? []
                : Enumerable.Empty<object>()
                    .Append(childNav.CurrentValue)
                    .Where(x => x is not null)
                    .Cast<object>();

            foreach (var childEntry in currentValues.Select(parentEntry.Context.Entry))
            {
                var childNode = await nodeByEntry.AddAggregateChain(childEntry, cancellationToken);
                parentNode.AddChild(childNode);
                childNode.AddParent(parentNode);
            }
        }
    }

    private static AggregateNode ToAggregateNode(this EntityEntry entry)
    {
        var aggregateState = entry.State switch
        {
            EntityState.Detached => AggregateNodeState.Unchanged,
            EntityState.Unchanged => AggregateNodeState.Unchanged,

            EntityState.Modified when entry.IsMarkedForSoftDeletion() => AggregateNodeState.Deleted,
            EntityState.Modified when entry.IsMarkedForRestoration() => AggregateNodeState.Restored,
            EntityState.Modified when entry.IsSoftDeleted() => AggregateNodeState.Unchanged,
            EntityState.Deleted when entry.IsSoftDeleted() => AggregateNodeState.Unchanged,

            EntityState.Deleted => AggregateNodeState.Deleted,
            EntityState.Modified => AggregateNodeState.Modified,
            EntityState.Added => AggregateNodeState.Added,
            _ => throw new UnreachableException()
        };

        var modifiedProperties = entry.Properties
            .Where(x => x.IsModified && !x.IsTemporary)
            .Select(x => AggregateNodeProperty.Create(
                x.Metadata.ClrType,
                x.Metadata.Name,
                x.OriginalValue,
                x.CurrentValue));

        return AggregateNode.Create(
            entry.Entity.GetType(),
            entry.Entity,
            aggregateState,
            modifiedProperties);
    }

    private static IEnumerable<IReadOnlyForeignKey> FindAggregateParents(this IReadOnlyEntityType entityType)
    {
        return entityType
            .GetForeignKeys()
            .Where(x => x
                .PrincipalToDependent?
                .PropertyInfo?
                .GetCustomAttribute<AggregateChildAttribute>()
                is not null);
    }

    private static IEnumerable<IReadOnlyForeignKey> FindAggregateChildren(this IReadOnlyEntityType entry)
    {
        return entry
            .GetReferencingForeignKeys()
            .Where(x => x
                .PrincipalToDependent?
                .PropertyInfo?
                .GetCustomAttribute<AggregateChildAttribute>()
                is not null);
    }

    private sealed class EntityEntryComparer : IEqualityComparer<EntityEntry>
    {
        public bool Equals(EntityEntry? x, EntityEntry? y) => ReferenceEquals(x?.Entity, y?.Entity);
        public int GetHashCode(EntityEntry obj) => obj.Entity.GetHashCode();
    }
}
