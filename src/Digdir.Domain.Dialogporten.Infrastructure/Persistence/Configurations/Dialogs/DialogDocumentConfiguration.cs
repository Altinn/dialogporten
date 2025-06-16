using Digdir.Domain.Dialogporten.Domain.Dialogs.Documents;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.ValueConverters;
using static Digdir.Domain.Dialogporten.Domain.Common.Constants;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Configurations.Dialogs;

internal sealed class DialogDocumentConfiguration : IEntityTypeConfiguration<DialogDocument>
{
    public void Configure(EntityTypeBuilder<DialogDocument> builder)
    {
        builder.ToTable("DialogDocument");

        builder.HasIndex(x => x.ServiceResource);
        builder.Property(x => x.ServiceResource)
            .HasMaxLength(DefaultMaxStringLength)
            .UseCollation("C");

        builder.HasIndex(x => x.Party);
        builder.Property(x => x.Party)
            .UseCollation("C");

        builder.HasIndex(x => x.Org);
        builder.Property(x => x.Org)
            .UseCollation("C");

        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.UpdatedAt);
        builder.HasIndex(x => x.DueAt);
        builder.HasIndex(x => x.VisibleFrom);

        builder.HasIndex(x => x.ExtendedStatus);
        builder.HasIndex(x => x.ExternalReference);
        builder.HasIndex(x => x.Process);
        builder.HasIndex(x => x.IsApiOnly);

        builder.Property(x => x.DialogData)
            .HasConversion<DialogEntityJsonConverter>()
            .HasColumnType("jsonb");
    }
}
