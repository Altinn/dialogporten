using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.ServiceResourceMetadata;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using MediatR;
using static Digdir.Domain.Dialogporten.Domain.Common.ServiceResourceUrnFactory;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.ServiceResources.Queries.Search;

public sealed class SearchAuthorizedServiceResourcesQuery : IRequest<SearchAuthorizedServiceResourcesDto>, IFeatureMetricServiceResourceIgnoreRequest
{
    public List<AcceptedLanguage>? AcceptedLanguages { get; set; }

    /// <summary>
    /// Optional party URN filter. Parties not in the caller's authorized set are silently dropped.
    /// </summary>
    public string[]? Parties { get; set; }
}

internal sealed class SearchAuthorizedServiceResourcesQueryHandler
    : IRequestHandler<SearchAuthorizedServiceResourcesQuery, SearchAuthorizedServiceResourcesDto>
{
    private readonly IAuthorizedServiceResourcesProvider _authorizedServiceResourcesProvider;
    private readonly IServiceResourceMetadataCatalogue _catalogue;

    public SearchAuthorizedServiceResourcesQueryHandler(
        IAuthorizedServiceResourcesProvider authorizedServiceResourcesProvider,
        IServiceResourceMetadataCatalogue catalogue)
    {
        ArgumentNullException.ThrowIfNull(authorizedServiceResourcesProvider);
        ArgumentNullException.ThrowIfNull(catalogue);

        _authorizedServiceResourcesProvider = authorizedServiceResourcesProvider;
        _catalogue = catalogue;
    }

    public async Task<SearchAuthorizedServiceResourcesDto> Handle(
        SearchAuthorizedServiceResourcesQuery request,
        CancellationToken ct
    )
    {
        // Per-caller authorized + referenced resources (bounded, cached). For callers authorized to a very large
        // number of parties on an unfiltered request, the provider signals the full catalogue should be returned
        // instead (the expensive per-party union is skipped).
        var authorized = await _authorizedServiceResourcesProvider.GetAuthorizedServiceResources(request.Parties, ct);

        var knownLanguages = await _catalogue.GetKnownLanguages(ct);
        var languages = request.AcceptedLanguages?.Where(x => knownLanguages.Contains(x.LanguageCode)).ToList();
        IReadOnlyList<ServiceResourceMetadataItemDto> items;

        if (authorized.IncludeFullCatalogue)
        {
            items = await _catalogue.GetCatalogueDtos(languages, ct);
        }
        else
        {
            var authorizedSet = new HashSet<string>(authorized.ResourceUrns, StringComparer.OrdinalIgnoreCase);
            var dtos = await _catalogue.GetCatalogueDtos(languages, ct);
            items = dtos.Where(x => authorizedSet.Contains(CreateUrn(x.ServiceResource.Id))).ToList();
        }

        return new SearchAuthorizedServiceResourcesDto
        {
            // Only signal the surprising case: the caller got the full catalogue as a fallback (too many
            // parties) instead of their authorized subset. Null for a normal authorization-scoped result.
            IsFullCatalogueFallback = authorized.IncludeFullCatalogue ? true : null,
            Items = items
        };
    }
}
