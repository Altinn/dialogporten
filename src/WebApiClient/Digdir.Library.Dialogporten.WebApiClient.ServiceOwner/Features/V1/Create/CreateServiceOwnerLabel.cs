using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Create;

public partial class CreateServiceOwnerLabel
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = default!;
}
