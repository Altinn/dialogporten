using Digdir.Domain.Dialogporten.Application.Features.V1.Common.ServiceResourceMetadata;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Metadata.ServiceResources.Queries.Get;

public sealed class GetServiceResourceMetadataDto
{
    public List<ServiceResourceMetadataItemDto> Items { get; set; } = [];
}
