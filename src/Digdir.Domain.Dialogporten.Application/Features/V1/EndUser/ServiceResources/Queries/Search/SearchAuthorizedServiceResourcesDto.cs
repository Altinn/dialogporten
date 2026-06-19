using Digdir.Domain.Dialogporten.Application.Features.V1.Common.ServiceResourceMetadata;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.ServiceResources.Queries.Search;

public sealed class SearchAuthorizedServiceResourcesDto
{
    public List<ServiceResourceMetadataItemDto> Items { get; set; } = [];
}
