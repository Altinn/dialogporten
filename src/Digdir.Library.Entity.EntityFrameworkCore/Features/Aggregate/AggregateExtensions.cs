﻿using System.Diagnostics;
using System.Reflection;
using Digdir.Library.Entity.Abstractions.Features.Aggregate;
using Digdir.Library.Entity.Abstractions.Features.Updatable;
using Digdir.Library.Entity.Abstractions.Features.Versionable;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Digdir.Library.Entity.EntityFrameworkCore.Features.Aggregate;

public static class AggregateExtensions
{
    private static readonly EntityEntryComparer _entityEntryComparer = new();

    internal static async Task HandleAggregateEntities(this ChangeTracker changeTracker,
        DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        var aggregateNodeByEntry = await changeTracker
            .Entries()
            .Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .GetAggregateNodeByEntry(cancellationToken);

        foreach (var (entry, aggregateNode) in aggregateNodeByEntry)
        {
            if (entry.Entity is INotifyAggregateCreated created && entry.State is EntityState.Added)
            {
                created.OnCreate(aggregateNode, utcNow);
            }

            if (entry.Entity is INotifyAggregateUpdated updated &&
                entry.State is EntityState.Modified or EntityState.Unchanged)
            {
                updated.OnUpdate(aggregateNode, utcNow);
            }

            if (entry.Entity is INotifyAggregateDeleted deleted && entry.State is EntityState.Deleted)
            {
                deleted.OnDelete(aggregateNode, utcNow);
            }

            if (entry.Entity is IUpdateableEntity updatable)
            {
                updatable.Update(utcNow);
            }

            if (entry.Entity is IVersionableEntity versionable)
            {
                versionable.NewVersion();
            }
        }
    }

    private static async Task<IReadOnlyDictionary<EntityEntry, AggregateNode>> GetAggregateNodeByEntry(
        this IEnumerable<EntityEntry> entries,
        CancellationToken cancellationToken)
    {
        var nodeByEntry = new Dictionary<EntityEntry, AggregateNode>(comparer: _entityEntryComparer);

        foreach (var entry in entries)
        {
            await nodeByEntry.AddAggregateParentChain(entry, cancellationToken);
        }

        return nodeByEntry;
    }

    private static async Task AddAggregateParentChain(
        this Dictionary<EntityEntry, AggregateNode> nodeByEntry,
        EntityEntry entry,
        CancellationToken cancellationToken)
    {
        if (!nodeByEntry.ContainsKey(entry))
        {
            nodeByEntry[entry] = AggregateNode.Create(entry.Entity);
        }

        foreach (var parentForeignKey in entry.Metadata.FindAggregateParents())
        {
            // Supports only dependent to principal. That is - one-to-one and one-to-many
            // relationships. Many-to-many relationships is not supported.
            var parentType = parentForeignKey.PrincipalEntityType.ClrType;

            var parentPrimaryKey = parentForeignKey
                .Properties
                .Select(key => entry.OriginalValues[key.Name])
                .ToArray();

            if (parentPrimaryKey.Length == 0 || parentPrimaryKey.Any(x => x is null))
            {
                throw new UnreachableException(
                    $"Foreign key to {parentType.Name} from {entry.Metadata.ClrType.Name} " +
                    $"is empty or contains null values.");
            }

            var parentEntity = await entry.Context
                .FindAsync(parentType, parentPrimaryKey, cancellationToken: cancellationToken);

            if (parentEntity is null)
            {
                throw new InvalidOperationException(
                    $"Could not find parent {parentType.Name} on {entry.Metadata.ClrType.Name} " +
                    $"with key [{string.Join(",", parentPrimaryKey)}].");
            }

            var parentEntry = entry.Context.Entry(parentEntity);

            if (!nodeByEntry.TryGetValue(parentEntry, out var parentNode))
            {
                nodeByEntry[parentEntry] = parentNode = AggregateNode.Create(parentEntry.Entity);
                await nodeByEntry.AddAggregateParentChain(parentEntry, cancellationToken);
            }

            parentNode.AddChild(nodeByEntry[entry]);
        }
    }

    private static IEnumerable<IForeignKey> FindAggregateParents(this IEntityType entityType)
    {
        return entityType
            .GetForeignKeys()
            .Where(x => x
                .DependentToPrincipal?
                .PropertyInfo?
                .GetCustomAttribute(typeof(AggregateParentAttribute)) is not null);
    }

    private sealed class EntityEntryComparer : IEqualityComparer<EntityEntry>
    {
        public bool Equals(EntityEntry? x, EntityEntry? y) => ReferenceEquals(x?.Entity, y?.Entity);
        public int GetHashCode(EntityEntry obj) => obj.Entity.GetHashCode();
    }
}