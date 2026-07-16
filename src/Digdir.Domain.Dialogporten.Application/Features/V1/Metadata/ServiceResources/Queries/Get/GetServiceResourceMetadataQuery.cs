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
        CancellationToken ct)
    {
        var knownLanguages = await _catalogue.GetKnownLanguages(ct);
        var languages = request.AcceptedLanguages?.Where(x => knownLanguages.Contains(x.LanguageCode)).ToList();
        var items = await _catalogue.GetCatalogueDtos(languages, ct);

        return new GetServiceResourceMetadataDto { Items = items };
    }
}
