using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Contracts.Common;

public class Links
{
    [JsonPropertyName("metadata")]
    public string Metadata { get; set; } = null!;
}