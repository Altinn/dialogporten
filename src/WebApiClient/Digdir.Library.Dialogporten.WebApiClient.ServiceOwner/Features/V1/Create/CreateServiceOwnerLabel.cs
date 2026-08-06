using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Create;

public class CreateServiceOwnerLabel
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = default!;
}
