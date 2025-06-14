using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Digdir.Domain.Dialogporten.Domain.Common.Constants;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Configurations.Dialogs;

internal sealed class DialogEntityConfiguration : IEntityTypeConfiguration<DialogEntity>
{
    public void Configure(EntityTypeBuilder<DialogEntity> builder)
    {
        builder.ToTable("Dialog");

        builder.HasIndex(x => x.ServiceResource);
        builder.Property(x => x.ServiceResource)
            .HasMaxLength(DefaultMaxStringLength);
        builder.Property(x => x.ServiceResource)
            .UseCollation("C");

        builder.HasIndex(x => x.Party);
        builder.Property(x => x.Party)
            .UseCollation("C");

        builder.HasIndex(x => x.Org);
        builder.Property(x => x.Org)
            .UseCollation("C");
        builder.HasIndex(x => new { x.Org, x.IdempotentKey })
            .IsUnique()
            .HasFilter($"\"{nameof(DialogEntity.IdempotentKey)}\" is not null");

        builder.Property(x => x.IdempotentKey)
            .HasMaxLength(36);

        builder.Property(x => x.ContentUpdatedAt)
            .HasDefaultValueSql("current_timestamp at time zone 'utc'");

        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.UpdatedAt);
        builder.HasIndex(x => x.DueAt);
        builder.HasIndex(x => x.VisibleFrom);
        builder.HasIndex(x => x.ContentUpdatedAt);

        builder.HasIndex(x => x.ExtendedStatus);
        builder.HasIndex(x => x.ExternalReference);

        builder.HasIndex(x => x.Process);

        builder.HasIndex(x => x.IsApiOnly);
        builder.Property(x => x.IsApiOnly)
            .HasDefaultValue(false);
    }
}
