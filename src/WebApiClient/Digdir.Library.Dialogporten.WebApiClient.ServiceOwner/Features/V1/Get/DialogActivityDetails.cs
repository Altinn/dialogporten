using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Common;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Get;

public class DialogActivityDetails
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset? CreatedAt { get; set; }

    [JsonPropertyName("extendedType")]
    public Uri? ExtendedType { get; set; }

    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter<DialogActivityType>))]
    public DialogActivityType Type { get; set; }

    [JsonPropertyName("transmissionId")]
    public Guid? TransmissionId { get; set; }

    [JsonPropertyName("performedBy")]
    public Actor PerformedBy { get; set; } = default!;

    [JsonPropertyName("description")]
    public ICollection<Localization>? Description { get; set; } = [];
}
