﻿using Digdir.Domain.Dialogporten.Domain.Localizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Configurations.Localizations;

internal class LocalizationConfiguration : IEntityTypeConfiguration<Localization>
{
    public void Configure(EntityTypeBuilder<Localization> builder)
    {
        builder.HasKey(x => new { x.LocalizationSetId, x.CultureCode });
        builder.Property(x => x.CultureCode).HasMaxLength(15);
    }
}
