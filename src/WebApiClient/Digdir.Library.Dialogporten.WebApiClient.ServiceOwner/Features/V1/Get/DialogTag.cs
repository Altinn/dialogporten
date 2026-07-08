using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Get;

public partial class DialogTag
{
    /// <summary>
    /// A search tag value.
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = default!;
}
