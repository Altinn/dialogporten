using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Common;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Search;

public class DialogSeenLogSearchItem
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("seenAt")]
    public DateTimeOffset SeenAt { get; set; }

    [JsonPropertyName("seenBy")]
    public Actor SeenBy { get; set; } = default!;

    [JsonPropertyName("isViaServiceOwner")]
    public bool IsViaServiceOwner { get; set; }

    [JsonPropertyName("isCurrentEndUser")]
    public bool IsCurrentEndUser { get; set; }
}
