using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Search;

public partial class Links
{
    [JsonPropertyName("metadata")]
    public string Metadata { get; set; } = default!;
}
