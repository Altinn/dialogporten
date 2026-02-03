/*
using Digdir.Domain.Dialogporten.Domain.Localizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Configurations.Localizations;

internal sealed class LocalizationSetConfiguration : IEntityTypeConfiguration<LocalizationSet>
{
    public void Configure(EntityTypeBuilder<LocalizationSet> builder)
    {
        string[] foreignIds =
        [
            "ActivityId",
            "AttachmentId",
            "DialogContentId",
            "GuiActionId",
            "NavigationalActionId",
            "TransmissionContentId",
        ];

        foreach (var foreignId in foreignIds)
        {
            // OPTIMIZATION: Partial + Covering Index
            // 1. Index on ID (Maintains strict uniqueness on the ID)
            builder.HasIndex(foreignId)
                .HasDatabaseName($"IX_LocalizationSet_{foreignId}")
                // 2. Partial: Only index rows where this specific FK is populated (Saves ~80% space)
                .HasFilter($"\"{foreignId}\" IS NOT NULL")
                // 3. Covering: Piggyback the Discriminator for Index-Only Scans
                .IncludeProperties("Discriminator")
                .IsUnique();
        }
    }
}
*/
