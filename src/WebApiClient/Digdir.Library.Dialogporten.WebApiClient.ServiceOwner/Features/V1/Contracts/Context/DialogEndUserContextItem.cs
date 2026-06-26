using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Contracts.Context;

public class DialogEndUserContextItem
{
    [JsonPropertyName("dialogId")]
    public Guid DialogId { get; set; }

    [JsonPropertyName("endUserContextRevision")]
    public Guid EndUserContextRevision { get; set; }

    [JsonPropertyName("systemLabels")]
    public ICollection<SystemLabel>? SystemLabels { get; set; }
}