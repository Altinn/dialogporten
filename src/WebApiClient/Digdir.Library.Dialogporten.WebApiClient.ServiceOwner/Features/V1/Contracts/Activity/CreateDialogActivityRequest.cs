using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Contracts.Activity;

public class CreateDialogActivityRequest
{
    /// <summary>
    /// A UUIDv7 may be provided to support idempotent additions to the list of activities.
    /// <br/>If not supplied, a new UUIDv7 will be generated.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid? Id { get; set; }

    /// <summary>
    /// If supplied, overrides the creating date and time for the activity.
    /// <br/>If not supplied, the current date /time will be used.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>
    /// Arbitrary URI/URN describing a service-specific activity type.
    /// </summary>
    [JsonPropertyName("extendedType")]
    public Uri? ExtendedType { get; set; }

    /// <summary>
    /// The type of activity
    /// </summary>
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter<DialogActivityType>))]
    public DialogActivityType Type { get; set; }

    /// <summary>
    /// If the activity is related to a particular transmission, this field will contain the transmission identifier.
    /// <br/>Must be present in the request body.
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
    public ICollection<Localization>? Description { get; set; }
}