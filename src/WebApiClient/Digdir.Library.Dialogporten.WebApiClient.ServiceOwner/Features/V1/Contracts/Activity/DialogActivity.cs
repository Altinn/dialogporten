using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Contracts.Activity;

public enum DialogActivityType
{
    [System.Runtime.Serialization.EnumMember(Value = @"DialogCreated")]
    DialogCreated = 0,

    [System.Runtime.Serialization.EnumMember(Value = @"DialogClosed")]
    DialogClosed = 1,

    [System.Runtime.Serialization.EnumMember(Value = @"Information")]
    Information = 2,

    [System.Runtime.Serialization.EnumMember(Value = @"TransmissionOpened")]
    TransmissionOpened = 3,

    [System.Runtime.Serialization.EnumMember(Value = @"PaymentMade")]
    PaymentMade = 4,

    [System.Runtime.Serialization.EnumMember(Value = @"SignatureProvided")]
    SignatureProvided = 5,

    [System.Runtime.Serialization.EnumMember(Value = @"DialogOpened")]
    DialogOpened = 6,

    [System.Runtime.Serialization.EnumMember(Value = @"DialogDeleted")]
    DialogDeleted = 7,

    [System.Runtime.Serialization.EnumMember(Value = @"DialogRestored")]
    DialogRestored = 8,

    [System.Runtime.Serialization.EnumMember(Value = @"SentToSigning")]
    SentToSigning = 9,

    [System.Runtime.Serialization.EnumMember(Value = @"SentToFormFill")]
    SentToFormFill = 10,

    [System.Runtime.Serialization.EnumMember(Value = @"SentToSendIn")]
    SentToSendIn = 11,

    [System.Runtime.Serialization.EnumMember(Value = @"SentToPayment")]
    SentToPayment = 12,

    [System.Runtime.Serialization.EnumMember(Value = @"FormSubmitted")]
    FormSubmitted = 13,

    [System.Runtime.Serialization.EnumMember(Value = @"FormSaved")]
    FormSaved = 14,

    [System.Runtime.Serialization.EnumMember(Value = @"CorrespondenceOpened")]
    CorrespondenceOpened = 15,

    [System.Runtime.Serialization.EnumMember(Value = @"CorrespondenceConfirmed")]
    CorrespondenceConfirmed = 16,
}

public class DialogActivity
{
    /// <summary>
    /// The unique identifier for the activity in UUIDv7 format.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The date and time when the activity was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>
    /// An arbitrary string with a service-specific activity type.
    /// <br/>
    /// <br/>Consult the service-specific documentation provided by the service owner for details (if in use).
    /// </summary>
    [JsonPropertyName("extendedType")]
    public Uri? ExtendedType { get; set; }

    /// <summary>
    /// The type of activity.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter<DialogActivityType>))]
    public DialogActivityType Type { get; set; }

    /// <summary>
    /// If the activity is related to a particular transmission, this field will contain the transmission identifier.
    /// </summary>
    [JsonPropertyName("transmissionId")]
    public Guid? TransmissionId { get; set; }

    /// <summary>
    /// The actor that performed the activity.
    /// </summary>
    [JsonPropertyName("performedBy")]
    public Actor PerformedBy { get; set; } = null!;

    /// <summary>
    /// Unstructured text describing the activity. Only set if the activity type is "Information".
    /// </summary>
    [JsonPropertyName("description")]
    public ICollection<Localization> Description { get; set; } = [];
}
