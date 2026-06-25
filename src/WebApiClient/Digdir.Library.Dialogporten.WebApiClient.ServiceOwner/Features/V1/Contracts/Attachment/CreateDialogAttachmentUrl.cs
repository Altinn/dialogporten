using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Contracts.Attachment;

public class CreateDialogAttachmentUrl
{
    /// <summary>
    /// A self-defined UUIDv7 may be provided to support idempotent creation of attachment URLs. If not provided, a new UUIDv7 will be generated.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid? Id { get; set; }

    /// <summary>
    /// The fully qualified URL of the attachment.
    /// </summary>
    [JsonPropertyName("url")]
    public Uri Url { get; set; } = null!;

    /// <summary>
    /// The media type of the attachment.
    /// </summary>
    [JsonPropertyName("mediaType")]
    public string? MediaType { get; set; }

    /// <summary>
    /// The type of consumer the URL is intended for.
    /// </summary>
    [JsonPropertyName("consumerType")]
    [JsonConverter(typeof(JsonStringEnumConverter<AttachmentUrlConsumerType>))]
    public AttachmentUrlConsumerType ConsumerType { get; set; }
}