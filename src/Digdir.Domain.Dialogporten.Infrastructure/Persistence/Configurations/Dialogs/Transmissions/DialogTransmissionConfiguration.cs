using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Configurations.Dialogs.Transmissions;

internal sealed class DialogTransmissionConfiguration : IEntityTypeConfiguration<DialogTransmission>
{
    public void Configure(EntityTypeBuilder<DialogTransmission> builder)
    {
        builder.HasOne(x => x.RelatedTransmission)
            .WithMany(x => x.RelatedTransmissions)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.ExternalReference);
    }
}
