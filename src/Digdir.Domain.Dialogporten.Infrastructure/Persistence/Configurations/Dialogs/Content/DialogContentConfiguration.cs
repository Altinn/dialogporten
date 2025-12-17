using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Configurations.Dialogs.Content;

internal sealed class DialogContentConfiguration
    : IEntityTypeConfiguration<DialogContent>
{
    public void Configure(EntityTypeBuilder<DialogContent> builder)
        => builder.HasIndex(x => new { x.DialogId, x.TypeId }).IsUnique();
}

internal sealed class TransmissionContentConfiguration
    : IEntityTypeConfiguration<DialogTransmissionContent>
{
    public void Configure(EntityTypeBuilder<DialogTransmissionContent> builder)
        => builder.HasIndex(x => new { x.TransmissionId, x.TypeId }).IsUnique();
}
