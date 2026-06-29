using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Contracts.Context;

public class DialogEndUserContext
{
    /// <summary>
    /// The unique identifier for the end user context revision in UUIDv4 format.
    /// </summary>
    [JsonPropertyName("revision")]
    public Guid Revision { get; set; }

    /// <summary>
    /// System defined labels used to categorize dialogs.
    /// </summary>
    [JsonPropertyName("systemLabels")]
    public ICollection<SystemLabel> SystemLabels { get; set; } = [];
}
