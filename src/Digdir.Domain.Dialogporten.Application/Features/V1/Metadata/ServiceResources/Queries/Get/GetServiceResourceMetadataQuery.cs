using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.ServiceResourceMetadata;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using MediatR;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Metadata.ServiceResources.Queries.Get;

public sealed class GetServiceResourceMetadataQuery : IRequest<GetServiceResourceMetadataDto>, IFeatureMetricServiceResourceIgnoreRequest
{
    public List<AcceptedLanguage>? AcceptedLanguages { get; set; }
}

internal sealed class GetServiceResourceMetadataQueryHandler : IRequestHandler<GetServiceResourceMetadataQuery, GetServiceResourceMetadataDto>
{
    private readonly IPartyResourceReferenceRepository _partyResourceReferenceRepository;
    private readonly IServiceResourceMetadataItemBuilder _itemBuilder;

    public GetServiceResourceMetadataQueryHandler(
        IPartyResourceReferenceRepository partyResourceReferenceRepository,
        IServiceResourceMetadataItemBuilder itemBuilder)
    {
        ArgumentNullException.ThrowIfNull(partyResourceReferenceRepository);
        ArgumentNullException.ThrowIfNull(itemBuilder);

        _partyResourceReferenceRepository = partyResourceReferenceRepository;
        _itemBuilder = itemBuilder;
    }

    public async Task<GetServiceResourceMetadataDto> Handle(
        GetServiceResourceMetadataQuery request,
        CancellationToken cancellationToken)
    {
        var resources = await _partyResourceReferenceRepository.GetReferencedResources(cancellationToken);
        var items = await _itemBuilder.BuildItems(resources, request.AcceptedLanguages, cancellationToken);
        return new GetServiceResourceMetadataDto { Items = items };
    }
}
