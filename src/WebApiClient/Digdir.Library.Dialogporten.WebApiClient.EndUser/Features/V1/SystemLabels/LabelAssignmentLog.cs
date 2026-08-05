using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Common;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.SystemLabels;

public class LabelAssignmentLog
{
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("action")]
    public string Action { get; set; } = default!;

    [JsonPropertyName("performedBy")]
    public Actor PerformedBy { get; set; } = default!;
}
