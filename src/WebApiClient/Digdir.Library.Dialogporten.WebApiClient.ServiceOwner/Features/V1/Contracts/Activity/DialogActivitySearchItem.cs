using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Contracts.Activity;

public class DialogActivitySearchItem
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("extendedType")]
    public Uri? ExtendedType { get; set; }

    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter<DialogActivityType>))]
    public DialogActivityType Type { get; set; }

    [JsonPropertyName("transmissionId")]
    public Guid? TransmissionId { get; set; }
}