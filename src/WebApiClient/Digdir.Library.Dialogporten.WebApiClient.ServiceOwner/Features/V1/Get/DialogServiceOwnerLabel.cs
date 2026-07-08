using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Get;

public partial class DialogServiceOwnerLabel
{
    /// <summary>
    /// A label value.
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = default!;
}
