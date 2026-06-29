using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Contracts.Transmission;

public class CreateTransmissionRequest
{
    /// <summary>
    /// A UUIDv7 may be provided to support idempotent additions to the list of transmissions.
    /// <br/>If not supplied, a new UUIDv7 will be generated.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid? Id { get; set; }

    /// <summary>
    /// An optional key to ensure idempotency in transmission creation. If provided, it must be unique within the dialog; reusing the same key for the same dialog results in Conflict and no new transmission is created.
    /// </summary>
    [JsonPropertyName("idempotentKey")]
    public string? IdempotentKey { get; set; }

    /// <summary>
    /// If supplied, overrides the creating date and time for the transmission.
    /// <br/>If not supplied, the current date /time will be used.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Contains an authorization resource attributeId, that can used in custom authorization rules in the XACML service
    /// <br/>policy, which by default is the policy belonging to the service referred to by "serviceResource" in the dialog.
    /// <br/>
    /// <br/>Can also be used to refer to other service policies.
    /// </summary>
    [JsonPropertyName("authorizationAttribute")]
    public string? AuthorizationAttribute { get; set; }

    /// <summary>
    /// Arbitrary URI/URN describing a service-specific transmission type.
    /// <br/>
    /// <br/>Refer to the service-specific documentation provided by the service owner for details (if in use).
    /// </summary>
    [JsonPropertyName("extendedType")]
    public Uri? ExtendedType { get; set; }

    /// <summary>
    /// Arbitrary string with a service-specific reference to an external system or service.
    /// </summary>
    [JsonPropertyName("externalReference")]
    public string? ExternalReference { get; set; }

    /// <summary>
    /// Reference to any other transmission that this transmission is related to.
    /// </summary>
    [JsonPropertyName("relatedTransmissionId")]
    public Guid? RelatedTransmissionId { get; set; }

    /// <summary>
    /// The type of transmission.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter<DialogTransmissionType>))]
    public DialogTransmissionType Type { get; set; }

    /// <summary>
    /// The actor that sent the transmission.
    /// </summary>
    [JsonPropertyName("sender")]
    public Actor Sender { get; set; } = null!;

    /// <summary>
    /// The transmission unstructured text content.
    /// </summary>
    [JsonPropertyName("content")]
    public TransmissionContent? Content { get; set; }

    /// <summary>
    /// The transmission-level attachments.
    /// </summary>
    [JsonPropertyName("attachments")]
    public ICollection<DialogAttachment>? Attachments { get; set; }

    /// <summary>
    /// The transmission-level navigational actions.
    /// </summary>
    [JsonPropertyName("navigationalActions")]
    public ICollection<DialogTransmissionNavigationalAction>? NavigationalActions { get; set; }
}
