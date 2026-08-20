using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Create;

public class CreateDialogTransmissionAttachmentUrl
{
    /// <summary>
    /// The fully qualified URL of the attachment.
    /// </summary>
    [JsonPropertyName("url")]
    public required Uri Url { get; set; }

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
