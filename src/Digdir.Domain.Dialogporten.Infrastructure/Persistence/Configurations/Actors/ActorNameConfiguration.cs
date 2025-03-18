using Digdir.Domain.Dialogporten.Domain.Actors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Configurations.Actors;

internal sealed class ActorNameConfiguration : IEntityTypeConfiguration<ActorName>
{
    public void Configure(EntityTypeBuilder<ActorName> builder)
    {
        builder
            .HasIndex(x => new { x.ActorId, x.Name })
            .IsUnique()
            .AreNullsDistinct(false);
    }
}
