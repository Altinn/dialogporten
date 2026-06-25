using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Contracts.Transmission;

public class DialogTransmissionDetails
{
    /// <summary>
    /// The unique identifier for the transmission in UUIDv7 format.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

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
    /// Flag indicating if the authenticated user is authorized for this transmission. If not, embedded content and
    /// <br/>the attachments will not be available.
    /// </summary>
    [JsonPropertyName("isAuthorized")]
    public bool IsAuthorized { get; set; }

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
    /// The date and time when the transmission was deleted, if applicable.
    /// </summary>
    [JsonPropertyName("deletedAt")]
    public DateTimeOffset? DeletedAt { get; set; }

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
    public Actor Sender { get; set; } = null!;

    /// <summary>
    /// The content of the transmission.
    /// </summary>
    [JsonPropertyName("content")]
    public DialogTransmissionContent Content { get; set; } = null!;

    /// <summary>
    /// The attachments associated with the transmission.
    /// </summary>
    [JsonPropertyName("attachments")]
    public ICollection<DialogAttachment> Attachments { get; set; } = [];

    /// <summary>
    /// The navigational actions associated with the transmission.
    /// </summary>
    [JsonPropertyName("navigationalActions")]
    public ICollection<DialogTransmissionNavigationalAction> NavigationalActions { get; set; } = [];
}
