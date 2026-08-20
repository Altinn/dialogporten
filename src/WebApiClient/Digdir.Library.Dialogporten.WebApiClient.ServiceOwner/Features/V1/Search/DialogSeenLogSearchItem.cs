using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Common;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Search;

public class DialogSeenLogSearchItem
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("seenAt")]
    public DateTimeOffset SeenAt { get; set; }

    [JsonPropertyName("seenBy")]
    public required Actor SeenBy { get; set; }

    [JsonPropertyName("isViaServiceOwner")]
    public bool? IsViaServiceOwner { get; set; }
}
