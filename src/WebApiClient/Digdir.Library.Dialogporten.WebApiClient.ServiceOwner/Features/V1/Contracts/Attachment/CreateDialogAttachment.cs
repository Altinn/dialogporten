using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Contracts.Attachment;

public class CreateDialogAttachment
{
    /// <summary>
    /// A self-defined UUIDv7 may be provided to support idempotent creation of attachments. If not provided, a new UUIDv7 will be generated.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid? Id { get; set; }

    /// <summary>
    /// The display name of the attachment that should be used in GUIs.
    /// </summary>
    [JsonPropertyName("displayName")]
    public ICollection<Localization>? DisplayName { get; set; }

    /// <summary>
    /// The logical name of the attachment.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// The URLs associated with the attachment, each referring to a different representation of the attachment.
    /// </summary>
    [JsonPropertyName("urls")]
    public ICollection<CreateDialogAttachmentUrl>? Urls { get; set; }

    /// <summary>
    /// The UTC timestamp when the attachment expires and is no longer available.
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; set; }
}