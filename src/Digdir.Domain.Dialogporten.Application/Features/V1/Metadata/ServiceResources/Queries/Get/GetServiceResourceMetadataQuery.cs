using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
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
    private readonly IServiceResourceMetadataCatalogue _catalogue;

    public GetServiceResourceMetadataQueryHandler(IServiceResourceMetadataCatalogue catalogue)
    {
        ArgumentNullException.ThrowIfNull(catalogue);
        _catalogue = catalogue;
    }

    public async Task<GetServiceResourceMetadataDto> Handle(
        GetServiceResourceMetadataQuery request,
        CancellationToken cancellationToken)
    {
        // Full public catalogue: every referenced resource, from the shared cached catalogue, with localizations
        // pruned into fresh per-request copies and re-sorted by the pruned (requested-language) name
        // (see ToSortedPrunedItems).
        var entries = await _catalogue.GetEntries(cancellationToken);
        var items = entries.ToSortedPrunedItems(request.AcceptedLanguages);
        return new GetServiceResourceMetadataDto { Items = items };
    }
}
