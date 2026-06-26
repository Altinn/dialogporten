using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Contracts.Activity;

public class LabelAssignmentLog
{
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("action")]
    public string Action { get; set; } = null!;

    [JsonPropertyName("performedBy")]
    public Actor PerformedBy { get; set; } = null!;
}