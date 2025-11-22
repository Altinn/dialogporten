using Digdir.Domain.Dialogporten.Domain.Localizations;
using Digdir.Library.Entity.Abstractions;
using Digdir.Library.Entity.Abstractions.Features.Aggregate;

namespace Digdir.Domain.Dialogporten.Domain.Attachments;

public abstract class Attachment : IEntity
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }

    // === Principal relationships ===
    [AggregateChild]
    public List<AttachmentUrl> Urls { get; set; } = [];

    [AggregateChild]
    public AttachmentDisplayName? DisplayName { get; set; }
}

public sealed class AttachmentDisplayName : LocalizationSet
{
    public Guid AttachmentId { get; set; }
    public Attachment Attachment { get; set; } = null!;
}
