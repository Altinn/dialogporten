using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Contracts.Dialog;

public class DialogTag
{
    /// <summary>
    /// A search tag value.
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = null!;
}