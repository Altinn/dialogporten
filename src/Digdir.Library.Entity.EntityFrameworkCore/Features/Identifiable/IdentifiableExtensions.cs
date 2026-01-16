using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Digdir.Library.Entity.EntityFrameworkCore.Features.Identifiable;

internal static class IdentifiableExtensions
{
    public static ModelBuilder AddIdentifiableEntities(this ModelBuilder modelBuilder)
    {
        return modelBuilder.EntitiesOfType<IIdentifiableEntity>(builder =>
        {
            builder.HasKey(nameof(IIdentifiableEntity.Id));
            builder.Property(nameof(IIdentifiableEntity.Id)).HasDefaultValueSql("gen_random_uuid()");
        });
    }

    public static ChangeTracker HandleIdentifiableEntities(this ChangeTracker changeTracker)
    {
        changeTracker.Entries<IIdentifiableEntity>()
            .Where(x => x.State == EntityState.Added)
            .Select(x => x.Entity)
            .EnsureIds();

        return changeTracker;
    }
}
