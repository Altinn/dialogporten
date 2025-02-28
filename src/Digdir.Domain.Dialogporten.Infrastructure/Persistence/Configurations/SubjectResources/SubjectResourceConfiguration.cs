using Digdir.Domain.Dialogporten.Domain.SubjectResources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Configurations.SubjectResources;

internal sealed class SubjectResourceConfiguration : IEntityTypeConfiguration<SubjectResource>
{
    public void Configure(EntityTypeBuilder<SubjectResource> builder)
    {
        builder
            .HasIndex(sr => new { sr.Resource, sr.Subject })
            .IsUnique();
    }
}
