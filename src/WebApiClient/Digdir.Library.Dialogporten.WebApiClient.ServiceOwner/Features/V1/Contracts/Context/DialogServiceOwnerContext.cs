using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Contracts.Context;

public class DialogServiceOwnerContext
{
    /// <summary>
    /// A list of labels, not visible in end-user APIs.
    /// </summary>
    [JsonPropertyName("serviceOwnerLabels")]
    public ICollection<ServiceOwnerLabel> ServiceOwnerLabels { get; set; } = [];

    /// <summary>
    /// The unique identifier for the service owner context revision in UUIDv4 format.
    /// </summary>
    [JsonPropertyName("revision")]
    public Guid Revision { get; set; }
}
