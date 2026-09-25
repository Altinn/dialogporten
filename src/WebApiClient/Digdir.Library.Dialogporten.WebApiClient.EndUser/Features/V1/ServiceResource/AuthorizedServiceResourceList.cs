using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.ServiceResource;

public class AuthorizedServiceResourceList
{
    /// <summary>
    /// True only when Items is the full referenced catalogue returned as a fallback instead of the caller's
    /// authorized subset (the caller is authorized to too many parties on an unfiltered request). Absent for
    /// a normal authorization-scoped result. Supply a party filter to always get an authorized result.
    /// </summary>
    [JsonPropertyName("isFullCatalogueFallback")]
    public bool? IsFullCatalogueFallback { get; set; }

    [JsonPropertyName("items")]
    public ICollection<ServiceResourceMetadata> Items { get; set; } = [];
}
