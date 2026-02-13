using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Configurations.DialogEndUserContexts;

internal sealed class DialogEndUserContextConfiguration : IEntityTypeConfiguration<DialogEndUserContext>
{
    public void Configure(EntityTypeBuilder<DialogEndUserContext> builder)
    {
        // Cover EXISTS lookup pattern: seek by DialogId, then use Id to join
        // DialogEndUserContextSystemLabel
        builder.HasIndex(x => x.DialogId)
            .IncludeProperties(x => x.Id)
            .HasDatabaseName("IX_DialogEndUserContext_DialogId_IncludeId");

        builder.HasOne(d => d.Dialog)
            .WithOne(d => d.EndUserContext)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
