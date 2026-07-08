using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.SystemLabels;

public partial class ServiceOwnerLabel
{
    /// <summary>
    /// A label value.
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = default!;
}
