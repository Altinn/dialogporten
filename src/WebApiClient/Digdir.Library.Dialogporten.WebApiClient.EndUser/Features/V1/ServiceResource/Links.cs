using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.ServiceResource;

public class Links
{
    [JsonPropertyName("metadata")]
    public string Metadata { get; set; } = null!;
}
