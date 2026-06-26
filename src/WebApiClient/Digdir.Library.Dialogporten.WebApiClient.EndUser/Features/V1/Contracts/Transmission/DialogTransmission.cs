using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Contracts.Transmission;

public class DialogTransmission
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
    /// Contains an authorization resource attributeId, that can used in custom authorization rules in the XACML service
    /// <br/>policy, which by default is the policy belonging to the service referred to by "serviceResource" in the dialog.
    /// <br/>
    /// <br/>Can also be used to refer to other service policies.
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
    /// Indicates whether the dialog transmission has been opened.
    /// </summary>
    [JsonPropertyName("isOpened")]
    public bool IsOpened { get; set; }

    /// <summary>
    /// The transmission unstructured text content.
    /// </summary>
    [JsonPropertyName("content")]
    public DialogTransmissionContent Content { get; set; } = null!;

    /// <summary>
    /// The transmission-level attachments.
    /// </summary>
    [JsonPropertyName("attachments")]
    public ICollection<DialogAttachment> Attachments { get; set; } = [];

    /// <summary>
    /// The transmission-level navigational actions.
    /// </summary>
    [JsonPropertyName("navigationalActions")]
    public ICollection<DialogTransmissionNavigationalAction> NavigationalActions { get; set; } = [];
}

public enum DialogTransmissionType
{
    [System.Runtime.Serialization.EnumMember(Value = @"Information")]
    Information = 0,

    [System.Runtime.Serialization.EnumMember(Value = @"Acceptance")]
    Acceptance = 1,

    [System.Runtime.Serialization.EnumMember(Value = @"Rejection")]
    Rejection = 2,

    [System.Runtime.Serialization.EnumMember(Value = @"Request")]
    Request = 3,

    [System.Runtime.Serialization.EnumMember(Value = @"Alert")]
    Alert = 4,

    [System.Runtime.Serialization.EnumMember(Value = @"Decision")]
    Decision = 5,

    [System.Runtime.Serialization.EnumMember(Value = @"Submission")]
    Submission = 6,

    [System.Runtime.Serialization.EnumMember(Value = @"Correction")]
    Correction = 7,
}
