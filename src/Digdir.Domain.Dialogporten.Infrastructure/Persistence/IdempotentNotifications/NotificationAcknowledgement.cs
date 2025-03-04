using Digdir.Domain.Dialogporten.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Digdir.Domain.Dialogporten.Domain.Common.Constants;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.IdempotentNotifications;

internal sealed class NotificationAcknowledgement
{
    public required Guid EventId { get; init; }
    public required string NotificationHandler { get; init; }
    public DateTimeOffset AcknowledgedAt { get; init; }
}

internal sealed class NotificationAcknowledgementConfiguration : IEntityTypeConfiguration<NotificationAcknowledgement>
{
    public void Configure(EntityTypeBuilder<NotificationAcknowledgement> builder)
    {
        builder.HasKey(x => new { x.EventId, x.NotificationHandler });
        builder.HasIndex(x => x.EventId);
        builder.Property(x => x.NotificationHandler).HasMaxLength(DefaultMaxStringLength);
        builder.Property(x => x.AcknowledgedAt).HasDefaultValueSql("current_timestamp at time zone 'utc'");
    }
}
