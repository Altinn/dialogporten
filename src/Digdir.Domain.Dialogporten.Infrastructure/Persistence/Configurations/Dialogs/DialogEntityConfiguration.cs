using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Configurations.Dialogs;

internal sealed class DialogEntityConfiguration : IEntityTypeConfiguration<DialogEntity>
{
    public void Configure(EntityTypeBuilder<DialogEntity> builder)
    {
        builder.ToTable("Dialog");

        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.UpdatedAt);
        builder.HasIndex(x => x.DueAt);
        builder.HasIndex(x => x.VisibleFrom);
        builder.HasIndex(x => x.ContentUpdatedAt);
        builder.HasIndex(x => x.ExtendedStatus);
        builder.HasIndex(x => x.ExternalReference);
        builder.HasIndex(x => x.Process);
        builder.HasIndex(x => x.IsApiOnly);
        builder.HasIndex(x => x.ServiceResource);
        builder.HasIndex(x => x.Party);
        builder.HasIndex(x => x.Org);
        builder.HasIndex(x => new { x.Org, x.IdempotentKey }).IsUnique()
            .HasFilter($"\"{nameof(DialogEntity.IdempotentKey)}\" is not null");
        builder.HasIndex(x => new { x.ServiceResource, x.Party }).IncludeProperties(x => x.Id);
        builder.HasIndex(x => new { x.Party, x.CreatedAt, x.Id })
            .IsDescending(false, true, true)
            .IncludeProperties(x => x.ServiceResource)
            .IsCreatedConcurrently();
        builder.HasIndex(x => new { x.Party, x.UpdatedAt, x.Id })
            .IsDescending(false, true, true)
            .IncludeProperties(x => x.ServiceResource)
            .IsCreatedConcurrently();
        builder.HasIndex(x => new { x.Party, x.ContentUpdatedAt, x.Id })
            .IsDescending(false, true, true)
            .IncludeProperties(x => x.ServiceResource)
            .IsCreatedConcurrently();
        builder.HasIndex(x => new { x.Party, x.DueAt, x.Id })
            .IsDescending(false, true, true)
            .IncludeProperties(x => x.ServiceResource)
            .IsCreatedConcurrently();

        builder.Property(x => x.Org).UseCollation("C");
        builder.Property(x => x.Party).UseCollation("C");
        builder.Property(x => x.ServiceResource).HasMaxLength(Domain.Common.Constants.DefaultMaxStringLength);
        builder.Property(x => x.ServiceResource).UseCollation("C");
        builder.Property(x => x.IdempotentKey).HasMaxLength(36);
        builder.Property(x => x.ContentUpdatedAt).HasDefaultValueSql("current_timestamp at time zone 'utc'");
        builder.Property(x => x.IsApiOnly).HasDefaultValue(false);
    }
}
