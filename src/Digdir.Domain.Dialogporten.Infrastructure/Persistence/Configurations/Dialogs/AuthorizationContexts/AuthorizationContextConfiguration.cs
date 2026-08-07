using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Configurations.Dialogs.AuthorizationContexts;

internal sealed class AuthorizationContextConfiguration : IEntityTypeConfiguration<AuthorizationContext>
{
    public void Configure(EntityTypeBuilder<AuthorizationContext> builder)
    {
        // String conventions (max length) do not apply to primitive collection elements.
        builder.PrimitiveCollection(x => x.Parties)
            .ElementType(e => e.HasMaxLength(Domain.Common.Constants.DefaultMaxStringLength));
    }
}
