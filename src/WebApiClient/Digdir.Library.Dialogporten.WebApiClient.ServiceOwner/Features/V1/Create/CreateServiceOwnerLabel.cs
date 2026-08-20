using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Create;

public class CreateServiceOwnerLabel
{
    [JsonPropertyName("value")]
    public required string Value { get; set; }
}
