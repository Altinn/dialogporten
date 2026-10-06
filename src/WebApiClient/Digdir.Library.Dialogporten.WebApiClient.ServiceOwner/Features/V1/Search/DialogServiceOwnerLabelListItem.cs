using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Search;

public class DialogServiceOwnerLabelListItem
{
    /// <summary>
    /// A label value.
    /// </summary>
    [JsonPropertyName("value")]
    public required string Value { get; set; }
}
