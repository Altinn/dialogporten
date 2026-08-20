using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Search;

public class Links
{
    [JsonPropertyName("metadata")]
    public required string Metadata { get; set; }
}
