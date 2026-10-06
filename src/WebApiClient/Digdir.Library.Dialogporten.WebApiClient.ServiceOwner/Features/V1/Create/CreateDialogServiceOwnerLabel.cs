using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Create;

public class CreateDialogServiceOwnerLabel
{
    /// <summary>
    /// A label value.
    /// </summary>
    [JsonPropertyName("value")]
    public required string Value { get; set; }
}
