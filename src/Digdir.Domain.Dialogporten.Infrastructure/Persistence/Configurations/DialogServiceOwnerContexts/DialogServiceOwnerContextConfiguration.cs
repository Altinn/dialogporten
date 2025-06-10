using Digdir.Domain.Dialogporten.Domain.DialogServiceOwnerContexts.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Configurations.DialogServiceOwnerContexts;

internal sealed class DialogServiceOwnerContextConfiguration : IEntityTypeConfiguration<DialogServiceOwnerContext>
{
    public void Configure(EntityTypeBuilder<DialogServiceOwnerContext> builder)
        => builder.HasKey(x => x.DialogId);
}
