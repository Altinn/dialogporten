using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Contracts.Dialog;

public class DialogRevision
{
    /// <summary>
    /// Target dialog id for system labels
    /// </summary>
    [JsonPropertyName("dialogId")]
    public Guid DialogId { get; set; }

    /// <summary>
    /// Optional end user context revision to match against. If supplied and not matching current revision, the entire operation will fail.
    /// </summary>
    [JsonPropertyName("endUserContextRevision")]
    public Guid? EndUserContextRevision { get; set; }
}