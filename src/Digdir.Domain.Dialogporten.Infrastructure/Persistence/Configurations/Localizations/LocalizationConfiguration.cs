using Digdir.Domain.Dialogporten.Domain.Localizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Configurations.Localizations;

internal sealed class LocalizationConfiguration : IEntityTypeConfiguration<Localization>
{
    public void Configure(EntityTypeBuilder<Localization> builder)
    {
        builder.HasKey(x => new { x.LocalizationSetId, CultureCode = x.LanguageCode });
        builder.Property(x => x.LanguageCode).HasMaxLength(15);
        builder.Property(x => x.Value).HasMaxLength(4095);

        builder.HasIndex(x => x.Value)
            .HasMethod(Constants.Gin)
            .HasOperators(Constants.GinTrgmOps);

    }
}
