using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Configurations.Dialogs;

internal sealed class DialogSeenLogConfiguration : IEntityTypeConfiguration<DialogSeenLog>
{
    public void Configure(EntityTypeBuilder<DialogSeenLog> builder)
    {
        // Also covers the DialogId foreign key, so EF does not create IX_DialogSeenLog_DialogId.
        builder.HasIndex(x => new { x.DialogId, x.CreatedAt })
            .HasDatabaseName("IX_DialogSeenLog_DialogId_CreatedAt");
    }
}
