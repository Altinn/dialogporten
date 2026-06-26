using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Contracts.Activity;

public class DialogSeenLog
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
    /// Flag indicating whether the seen log entry was created by the end user supplied in the query.
    /// </summary>
    [JsonPropertyName("isCurrentEndUser")]
    public bool IsCurrentEndUser { get; set; }
}

public class DialogSeenLogDetails
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("seenAt")]
    public DateTimeOffset SeenAt { get; set; }

    [JsonPropertyName("seenBy")]
    public Actor SeenBy { get; set; } = null!;

    [JsonPropertyName("isViaServiceOwner")]
    public bool? IsViaServiceOwner { get; set; }
}
