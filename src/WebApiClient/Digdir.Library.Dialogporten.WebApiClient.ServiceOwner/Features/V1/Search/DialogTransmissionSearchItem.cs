using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Common;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Search;

public class DialogTransmissionSearchItem
{
    /// <summary>
    /// The unique identifier for the transmission in UUIDv7 format.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// An optional key to ensure idempotency in transmission creation. If provided, it must be unique within the dialog; reusing the same key for the same dialog results in Conflict and no new transmission is created.
    /// </summary>
    [JsonPropertyName("idempotentKey")]
    public string? IdempotentKey { get; set; }

    /// <summary>
    /// The date and time when the transmission was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// The authorization attribute associated with the transmission.
    /// </summary>
    [JsonPropertyName("authorizationAttribute")]
    public string? AuthorizationAttribute { get; set; }

    /// <summary>
    /// The extended type URI for the transmission.
    /// </summary>
    [JsonPropertyName("extendedType")]
    public Uri? ExtendedType { get; set; }

    /// <summary>
    /// Arbitrary string with a service-specific reference to an external system or service.
    /// </summary>
    [JsonPropertyName("externalReference")]
    public string? ExternalReference { get; set; }

    /// <summary>
    /// The unique identifier for the related transmission, if any.
    /// </summary>
    [JsonPropertyName("relatedTransmissionId")]
    public Guid? RelatedTransmissionId { get; set; }

    /// <summary>
    /// The type of the transmission.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter<DialogTransmissionType>))]
    public DialogTransmissionType Type { get; set; }

    /// <summary>
    /// The sender actor information for the transmission.
    /// </summary>
    [JsonPropertyName("sender")]
    public Actor Sender { get; set; } = default!;

    /// <summary>
    /// The content of the transmission.
    /// </summary>
    [JsonPropertyName("content")]
    public DialogTransmissionSearchContent Content { get; set; } = default!;

    /// <summary>
    /// The attachments associated with the transmission.
    /// </summary>
    [JsonPropertyName("attachments")]
    public ICollection<DialogTransmissionSearchAttachment>? Attachments { get; set; } = [];

    /// <summary>
    /// The navigational actions associated with the transmission.
    /// </summary>
    [JsonPropertyName("navigationalActions")]
    public ICollection<DialogTransmissionSearchNavigationalAction>? NavigationalActions { get; set; } = [];
}
