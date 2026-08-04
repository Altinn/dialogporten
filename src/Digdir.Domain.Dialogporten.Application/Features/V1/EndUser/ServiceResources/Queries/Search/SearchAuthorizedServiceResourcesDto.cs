using System.Text.Json.Serialization;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.ServiceResourceMetadata;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.ServiceResources.Queries.Search;

public sealed class SearchAuthorizedServiceResourcesDto
{
    /// <summary>
    /// Set to <c>true</c> only when <see cref="Items"/> is the full referenced catalogue returned as a fallback
    /// instead of the caller's authorized subset: this happens when the caller is authorized to a very large
    /// number of parties on an unfiltered request, so the authorized union is not computed. Absent/null for a
    /// normal authorization-scoped result — supply a party filter to always get an authorization-scoped result.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsFullCatalogueFallback { get; set; }

    public List<ServiceResourceMetadataItemDto> Items { get; set; } = [];
}
