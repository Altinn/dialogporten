using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Common;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Search;

public class DialogActivityListItem
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
    public required Actor PerformedBy { get; set; }

    /// <summary>
    /// Unstructured text describing the activity. Only set if the activity type is "Information".
    /// </summary>
    [JsonPropertyName("description")]
    public ICollection<Localization> Description { get; set; } = [];
}
