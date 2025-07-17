using Digdir.Domain.Dialogporten.Domain.DialogServiceOwnerContexts.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Configurations.DialogServiceOwnerContexts;

internal sealed class DialogSystemOwnerLabelConfiguration : IEntityTypeConfiguration<DialogServiceOwnerLabel>
{
    public void Configure(EntityTypeBuilder<DialogServiceOwnerLabel> builder)
    {
        builder.HasKey(x => new { x.DialogServiceOwnerContextId, x.Value });

        builder.HasIndex(x => x.Value)
            .HasDatabaseName("IX_DialogServiceOwnerLabel_Value_Covering")
            .IncludeProperties(x => x.DialogServiceOwnerContextId);
    }
}
