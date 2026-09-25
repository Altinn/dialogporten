using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.SystemLabels;

public class ServiceOwnerLabel
{
    /// <summary>
    /// A label value.
    /// </summary>
    [JsonPropertyName("value")]
    public required string Value { get; set; }
}
