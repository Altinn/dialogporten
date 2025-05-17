using Digdir.Domain.Dialogporten.Domain.ServiceOwnerContexts.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Configurations.ServiceOwnerContexts;

internal sealed class ServiceOwnerContextConfiguration : IEntityTypeConfiguration<ServiceOwnerContext>
{
    public void Configure(EntityTypeBuilder<ServiceOwnerContext> builder)
    {
        builder.HasOne(d => d.Dialog)
            .WithOne(d => d.ServiceOwnerContext)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
