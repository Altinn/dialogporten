using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Configurations.DialogEndUserContexts;

internal sealed class DialogEndUserContextConfiguration : IEntityTypeConfiguration<DialogEndUserContext>
{
    public void Configure(EntityTypeBuilder<DialogEndUserContext> builder)
    {
        builder.HasOne(d => d.Dialog)
            .WithOne(d => d.EndUserContext)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(d => d.SystemLabelIds)
            .HasMethod("GIN");
    }
}
