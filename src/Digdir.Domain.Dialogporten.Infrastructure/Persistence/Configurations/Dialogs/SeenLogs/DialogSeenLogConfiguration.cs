using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Configurations.Dialogs.SeenLogs;

public class DialogSeenLogConfiguration : IEntityTypeConfiguration<DialogSeenLog>
{

    public void Configure(EntityTypeBuilder<DialogSeenLog> builder)
    {
        builder.HasIndex(x =>
                new
                {
                    x.Id,
                    ActorId = x.SeenBy.Id,
                    x.LastSeenLogId
                })
            .IsUnique()
            .HasFilter($"\"{nameof(DialogSeenLog.LastSeenLogId)}\" is not null");

    }
}
