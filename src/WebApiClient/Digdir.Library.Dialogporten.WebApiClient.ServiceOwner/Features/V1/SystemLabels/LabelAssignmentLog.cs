using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Common;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.SystemLabels;

public class LabelAssignmentLog
{
    /// <summary>
    /// The date and time when the label was set or removed.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// The name of the label.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// The action performed on the label, either "set" or "remove".
    /// </summary>
    [JsonPropertyName("action")]
    public required string Action { get; set; }

    /// <summary>
    /// The actor that performed the label change.
    /// </summary>
    [JsonPropertyName("performedBy")]
    public required Actor PerformedBy { get; set; }
}
