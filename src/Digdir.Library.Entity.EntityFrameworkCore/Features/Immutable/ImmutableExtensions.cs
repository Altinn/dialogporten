using Digdir.Library.Entity.Abstractions.Features.Immutable;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Digdir.Library.Entity.EntityFrameworkCore.Features.Immutable;

internal static class ImmutableExtensions
{
    public static ModelBuilder AddImmutableEntities(this ModelBuilder modelBuilder) => modelBuilder;

    public static ChangeTracker HandleImmutableEntities(this ChangeTracker changeTracker)
    {
        var immutableEntities = changeTracker
            .Entries<IImmutableEntity>()
            .Where(x => x.State == EntityState.Modified);

        if (immutableEntities.Any())
        {
            throw new InvalidOperationException(
                $"Entities marked with {nameof(IImmutableEntity)} can not be modified. " +
                $"The following entity types were attempted modified: " +
                $"{string.Join(", ", immutableEntities.Select(x => x.Metadata.ClrType.Name).Distinct())}");
        }

        return changeTracker;
    }
}
