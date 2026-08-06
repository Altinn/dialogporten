using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Common;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Search;

public class DialogSeenLogListItem
{
    /// <summary>
    /// The unique identifier for the seen log entry in UUIDv7 format.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The timestamp when the dialog revision was seen.
    /// </summary>
    [JsonPropertyName("seenAt")]
    public DateTimeOffset SeenAt { get; set; }

    /// <summary>
    /// The actor that saw the dialog revision.
    /// </summary>
    [JsonPropertyName("seenBy")]
    public Actor SeenBy { get; set; } = null!;

    /// <summary>
    /// Flag indicating whether the seen log entry was created via the service owner.
    /// <br/>
    /// <br/>This is used when the service owner uses the service owner API to implement its own frontend.
    /// </summary>
    [JsonPropertyName("isViaServiceOwner")]
    public bool? IsViaServiceOwner { get; set; }

    /// <summary>
    /// Flag indicating whether the seen log entry was created by the current end user.
    /// </summary>
    [JsonPropertyName("isCurrentEndUser")]
    public bool IsCurrentEndUser { get; set; }
}
