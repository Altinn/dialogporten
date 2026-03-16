using Digdir.Library.Entity.Abstractions;

namespace Digdir.Domain.Dialogporten.Domain.Attachments;

public sealed class AttachmentUrl : IEntity
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public string? MediaType { get; set; }
    public Uri Url { get; set; } = null!;

    // === Dependent relationships ===
    public AttachmentUrlConsumerType.Values ConsumerTypeId { get; set; }
    public AttachmentUrlConsumerType ConsumerType { get; set; } = null!;

    public Guid AttachmentId { get; set; }
    public Attachment Attachment { get; set; } = null!;
}
