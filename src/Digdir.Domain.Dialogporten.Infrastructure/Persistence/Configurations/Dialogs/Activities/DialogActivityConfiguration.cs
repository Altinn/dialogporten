﻿using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Configurations.Dialogs.Activities;

internal sealed class DialogActivityConfiguration : IEntityTypeConfiguration<DialogActivity>
{
    public void Configure(EntityTypeBuilder<DialogActivity> builder)
    {
        builder.HasOne(x => x.Transmission)
            .WithMany(x => x.Activities)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
