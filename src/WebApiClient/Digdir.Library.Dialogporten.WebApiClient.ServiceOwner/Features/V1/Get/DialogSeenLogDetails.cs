using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Get;

public class DialogSeenLogDetails
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("seenAt")]
    public DateTimeOffset SeenAt { get; set; }

    [JsonPropertyName("seenBy")]
    public Actor SeenBy { get; set; } = default!;

    [JsonPropertyName("isViaServiceOwner")]
    public bool? IsViaServiceOwner { get; set; }
}
