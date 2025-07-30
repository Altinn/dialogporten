using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Configurations.DialogEndUserContexts;

internal sealed class DialogEndUserContextSystemLabelConfiguration : IEntityTypeConfiguration<DialogEndUserContextSystemLabel>
{
    public void Configure(EntityTypeBuilder<DialogEndUserContextSystemLabel> builder)
    {
        builder.HasKey(x => new { x.DialogEndUserContextId, x.SystemLabelId });
    }
}
