using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Configurations.Dialogs.AuthorizationContexts;

internal sealed class AuthorizationContextConfiguration : IEntityTypeConfiguration<AuthorizationContext>
{
    public void Configure(EntityTypeBuilder<AuthorizationContext> builder)
    {
        // Insert-heavy table: native UUIDv7 (PG18+) keeps b-tree insert locality, unlike the
        // library-wide gen_random_uuid() (v4) default this overrides.
        builder.Property(x => x.Id).HasDefaultValueSql("uuidv7()");

        // Smallint mapped to the enum in code only — deliberately no lookup table/FK/index
        // (see AuthorizationContextUnauthorizedPresentation).
        builder.Property(x => x.UnauthorizedPresentation).HasConversion<short>();

        // String conventions (max length) do not apply to primitive collection elements.
        builder.PrimitiveCollection(x => x.Parties)
            .ElementType(e => e.HasMaxLength(Domain.Common.Constants.DefaultMaxStringLength));
    }
}
