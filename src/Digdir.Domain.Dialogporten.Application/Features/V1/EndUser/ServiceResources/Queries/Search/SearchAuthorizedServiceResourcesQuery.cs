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
    private readonly IServiceResourceMetadataItemBuilder _itemBuilder;

    public SearchAuthorizedServiceResourcesQueryHandler(
        IAuthorizedServiceResourcesProvider authorizedServiceResourcesProvider,
        IServiceResourceMetadataItemBuilder itemBuilder)
    {
        ArgumentNullException.ThrowIfNull(authorizedServiceResourcesProvider);
        ArgumentNullException.ThrowIfNull(itemBuilder);

        _authorizedServiceResourcesProvider = authorizedServiceResourcesProvider;
        _itemBuilder = itemBuilder;
    }

    public async Task<SearchAuthorizedServiceResourcesDto> Handle(
        SearchAuthorizedServiceResourcesQuery request,
        CancellationToken cancellationToken)
    {
        var resourcesByParty = await _authorizedServiceResourcesProvider
            .GetAuthorizedServiceResourcesByParty(cancellationToken);

        var partyFilter = request.Parties is { Length: > 0 }
            ? new HashSet<string>(request.Parties, StringComparer.OrdinalIgnoreCase)
            : null;

        var authorizedResources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (party, resources) in resourcesByParty)
        {
            if (partyFilter is null || partyFilter.Contains(party))
            {
                authorizedResources.UnionWith(resources);
            }
        }

        var items = await _itemBuilder.BuildItems(authorizedResources, request.AcceptedLanguages, cancellationToken);

        return new SearchAuthorizedServiceResourcesDto { Items = items };
    }
}
