using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Contracts.Context;

public class ServiceOwnerLabel
{
    /// <summary>
    /// A label value.
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = null!;
}
