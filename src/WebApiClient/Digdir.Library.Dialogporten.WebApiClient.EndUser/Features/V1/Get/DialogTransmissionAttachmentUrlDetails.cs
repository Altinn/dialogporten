using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Enums;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Get;

public class DialogTransmissionAttachmentUrlDetails
{
    /// <summary>
    /// The unique identifier for the attachment URL in UUIDv7 format.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The fully qualified URL of the attachment. Will be set to "urn:dialogporten:unauthorized" if the user is
    /// <br/>not authorized to access the transmission.
    /// </summary>
    [JsonPropertyName("url")]
    public Uri Url { get; set; } = default!;

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
