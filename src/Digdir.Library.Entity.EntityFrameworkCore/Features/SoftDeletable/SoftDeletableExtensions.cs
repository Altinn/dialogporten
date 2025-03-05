using Digdir.Library.Entity.Abstractions.Features.SoftDeletable;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Reflection;

namespace Digdir.Library.Entity.EntityFrameworkCore.Features.SoftDeletable;

/// <summary>
/// Provides extension methods for EntityFrameworkCore.
/// </summary>
public static class SoftDeletableExtensions
{
    private static readonly MethodInfo OpenGenericInternalMethodInfo = typeof(SoftDeletableExtensions)
            .GetMethod(nameof(EnableSoftDeletableQueryFilter_Internal), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>
    /// Marks a <typeparamref name="TSoftDeletableEntity"/> as hard deleted.
    /// </summary>
    /// <remarks>
    /// This will permanently delete the entity from the database.
    /// </remarks>
    /// <param name="set">The <see cref="DbSet{TEntity}"/> where <paramref name="entity"/> resides.</param>
    /// <param name="entity">The entity to permanently delete.</param>
    /// <returns>
    /// The <see cref="EntityEntry{TEntity}" /> for the entity. The entry provides
    /// access to change tracking information and operations for the entity.
    /// </returns>
    public static EntityEntry<TSoftDeletableEntity> HardRemove<TSoftDeletableEntity>(this DbSet<TSoftDeletableEntity> set, TSoftDeletableEntity entity)
        where TSoftDeletableEntity : class, ISoftDeletableEntity => set.Remove(entity);

    /// <summary>
    /// Marks a <typeparamref name="TSoftDeletableEntity"/> as hard deleted.
    /// </summary>
    /// <remarks>
    /// This will permanently delete the entity from the database.
    /// </remarks>
    /// <param name="set">The <see cref="DbSet{TEntity}"/> where <paramref name="entities"/> resides.</param>
    /// <param name="entities">The entities to permanently delete.</param>
    public static void HardRemoveRange<TSoftDeletableEntity>(this DbSet<TSoftDeletableEntity> set, IEnumerable<TSoftDeletableEntity> entities)
        where TSoftDeletableEntity : class, ISoftDeletableEntity
    {
        foreach (var entity in entities)
        {
            set.HardRemove(entity);
        }
    }

    /// <summary>
    /// Marks a <typeparamref name="TSoftDeletableEntity"/> as soft deleted.
    /// </summary>
    /// <remarks>
    /// This will mark the entity as deleted in the database.
    /// </remarks>
    /// <param name="set">The <see cref="DbSet{TEntity}"/> where <paramref name="entity"/> resides.</param>
    /// <param name="entity">The entity to soft-delete.</param>
    /// <returns>
    /// The <see cref="EntityEntry{TEntity}" /> for the entity. The entry provides
    /// access to change tracking information and operations for the entity.
    /// </returns>
    public static EntityEntry<TSoftDeletableEntity> SoftRemove<TSoftDeletableEntity>(this DbSet<TSoftDeletableEntity> set, TSoftDeletableEntity entity)
        where TSoftDeletableEntity : class, ISoftDeletableEntity
    {
        entity.SoftDelete();
        // In case the entity implements SoftDelete, but forgets to set Deleted to true.
        entity.Deleted = true;
        return set.Entry(entity);
    }

    /// <summary>
    /// Marks a <typeparamref name="TSoftDeletableEntity"/> as soft deleted.
    /// </summary>
    /// <remarks>
    /// This will mark the entity as deleted in the database.
    /// </remarks>
    /// <param name="set">The <see cref="DbSet{TEntity}"/> where <paramref name="entities"/> resides.</param>
    /// <param name="entities">The entities to soft-delete.</param>
    public static void SoftRemoveRange<TSoftDeletableEntity>(this DbSet<TSoftDeletableEntity> set, IEnumerable<TSoftDeletableEntity> entities)
        where TSoftDeletableEntity : class, ISoftDeletableEntity
    {
        foreach (var entity in entities)
        {
            set.SoftRemove(entity);
        }
    }

    internal static ModelBuilder EnableSoftDeletableQueryFilter(this ModelBuilder modelBuilder)
    {
        return modelBuilder.EntitiesOfType<ISoftDeletableEntity>(builder =>
        {
            builder.HasIndex(nameof(ISoftDeletableEntity.Deleted));

            var method = OpenGenericInternalMethodInfo.MakeGenericMethod(builder.Metadata.ClrType);
            method.Invoke(null, [modelBuilder]);
        });
    }

    internal static bool IsMarkedForSoftDeletion(this EntityEntry entry)
    {
        return entry.Entity is ISoftDeletableEntity
            && !(bool)entry.Property(nameof(ISoftDeletableEntity.Deleted)).OriginalValue! // Not already soft-deleted in the database
            && (bool)entry.Property(nameof(ISoftDeletableEntity.Deleted)).CurrentValue!; // Deleted in memory
    }

    internal static bool IsMarkedForRestoration(this EntityEntry entry)
    {
        return entry.Entity is ISoftDeletableEntity
            && (bool)entry.Property(nameof(ISoftDeletableEntity.Deleted)).OriginalValue! // Already soft-deleted in the database
            && !(bool)entry.Property(nameof(ISoftDeletableEntity.Deleted)).CurrentValue!; // Restored in memory
    }

    internal static bool IsSoftDeleted(this EntityEntry entry)
    {
        return entry.Entity is ISoftDeletableEntity
            && (bool)entry.Property(nameof(ISoftDeletableEntity.Deleted)).OriginalValue!; // Already soft-deleted in the database
    }

    internal static ChangeTracker HandleSoftDeletableEntities(this ChangeTracker changeTracker, DateTimeOffset utcNow)
    {
        var softDeletableEntities = changeTracker
            .Entries<ISoftDeletableEntity>()
            .ToList()
            .AssertNoModifiedSoftDeletedEntity();

        var softDeletedEntities = softDeletableEntities
            .Where(x => x.State is EntityState.Modified or EntityState.Added && x.Entity.Deleted);

        foreach (var entity in softDeletedEntities)
        {
            entity.Entity.SoftDelete(utcNow);
        }

        return changeTracker;
    }

    private static List<EntityEntry<ISoftDeletableEntity>> AssertNoModifiedSoftDeletedEntity(this List<EntityEntry<ISoftDeletableEntity>> softDeletableEntities)
    {
        var invalidSoftDeleteModifications = softDeletableEntities
            .Where(x => x.State is EntityState.Modified
                && x.Property(x => x.Deleted).OriginalValue // Already soft-deleted in the database
                && !x.Property(x => x.Deleted).CurrentValue); // Restored in memory

        return invalidSoftDeleteModifications.Any()
            ? throw new InvalidOperationException("Cannot modify a soft deleted entity without restoring it first.")
            : softDeletableEntities;
    }

    private static void EnableSoftDeletableQueryFilter_Internal<T>(ModelBuilder modelBuilder)
        where T : class, ISoftDeletableEntity => modelBuilder.Entity<T>().HasQueryFilter(x => !x.Deleted);
}
