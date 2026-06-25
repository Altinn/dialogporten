using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.ServiceResourceMetadata;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using MediatR;

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
        CancellationToken cancellationToken)
    {
        // Per-caller authorized + referenced resources (bounded, cached). For callers authorized to a very large
        // number of parties on an unfiltered request, the provider signals the full catalogue should be returned
        // instead (the expensive per-party union is skipped).
        var authorized = await _authorizedServiceResourcesProvider
            .GetAuthorizedServiceResources(request.Parties, cancellationToken);

        // Select from the shared, pre-sorted, all-language catalogue and prune localizations into fresh
        // per-request copies. Filtering preserves the catalogue's name/id ordering.
        var entries = await _catalogue.GetEntries(cancellationToken);
        IEnumerable<ServiceResourceMetadataCatalogueEntry> selected;
        if (authorized.IncludeFullCatalogue)
        {
            selected = entries;
        }
        else
        {
            var authorizedSet = new HashSet<string>(authorized.ResourceUrns, StringComparer.OrdinalIgnoreCase);
            selected = entries.Where(entry => authorizedSet.Contains(entry.ResourceUrn));
        }

        // Prune localizations into fresh per-request copies and re-sort by the pruned (requested-language) name
        // (see ToSortedPrunedItems).
        var items = selected.ToSortedPrunedItems(request.AcceptedLanguages);

        return new SearchAuthorizedServiceResourcesDto
        {
            // Only signal the surprising case: the caller got the full catalogue as a fallback (too many
            // parties) instead of their authorized subset. Null for a normal authorization-scoped result.
            IsFullCatalogueFallback = authorized.IncludeFullCatalogue ? true : null,
            Items = items
        };
    }
}
