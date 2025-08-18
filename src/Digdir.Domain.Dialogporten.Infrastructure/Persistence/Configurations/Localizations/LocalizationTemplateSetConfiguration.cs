using Digdir.Domain.Dialogporten.Domain.Localizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Configurations.Localizations;

internal sealed class LocalizationTemplateSetConfiguration : IEntityTypeConfiguration<LocalizationTemplateSet>
{
    public void Configure(EntityTypeBuilder<LocalizationTemplateSet> builder)
    {
        builder.HasKey(x => new { x.Org, x.Id });
        builder.Property(x => x.Org).HasMaxLength(100);
        builder.Property(x => x.Id).HasMaxLength(100);
        builder.Property(x => x.ImmutableCopyId).HasMaxLength(100);

        builder.HasOne(x => x.ImmutableCopy)
            .WithOne(x => x.Source)
            .HasPrincipalKey<LocalizationTemplateSet>(x => new { x.Org, x.Id })
            .HasForeignKey<LocalizationTemplateSet>(x => new { x.Org, x.ImmutableCopyId });
        builder.Navigation(x => x.ImmutableCopy).AutoInclude();
        builder.OwnsMany(x => x.Templates, x =>
        {
            x.OwnedEntityType.RemoveAnnotation(RelationalAnnotationNames.TableName);
            x.ToJson();
        });
    }
}
