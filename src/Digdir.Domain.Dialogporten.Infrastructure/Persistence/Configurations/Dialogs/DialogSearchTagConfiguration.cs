using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Digdir.Domain.Dialogporten.Domain.Common.Constants;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Configurations.Dialogs;

internal sealed class DialogSearchTagConfiguration : IEntityTypeConfiguration<DialogSearchTag>
{
    public void Configure(EntityTypeBuilder<DialogSearchTag> builder)
    {
        builder.Property(x => x.Value)
            .HasMaxLength(MaxSearchTagLength);

        builder.HasIndex(x => x.Value)
            .HasMethod(Constants.Gin)
            .HasOperators(Constants.GinTrgmOps);
    }
}
