using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Get;

public class DialogEndUserContextItem
{
    [JsonPropertyName("dialogId")]
    public Guid DialogId { get; set; }

    [JsonPropertyName("endUserContextRevision")]
    public Guid EndUserContextRevision { get; set; }

    [JsonPropertyName("systemLabels")]
    // TODO(system.text.json): Add ItemConverterType with enum converter when supported
    public ICollection<SystemLabel>? SystemLabels { get; set; } = [];
}
