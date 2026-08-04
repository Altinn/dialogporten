using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.ServiceResources.Queries.Search;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using FastEndpoints;
using MediatR;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser.ServiceResources.Search;

[OpenApiOperationId("SearchAuthorizedServiceResources")]
public sealed class SearchAuthorizedServiceResourcesEndpoint
    : Endpoint<SearchAuthorizedServiceResourcesRequest, SearchAuthorizedServiceResourcesDto>
{
    private readonly ISender _sender;

    public SearchAuthorizedServiceResourcesEndpoint(ISender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);

        _sender = sender;
    }

    public override void Configure()
    {
        Get("serviceresources");
        Policies(AuthorizationPolicy.EndUser);
        Group<EndUserGroup>();

        // Response compression is intentionally NOT enabled on this authenticated endpoint (CRIME/BREACH).
        Description(b => b.ProducesOneOf<SearchAuthorizedServiceResourcesDto>(StatusCodes.Status200OK));
    }

    public override async Task HandleAsync(SearchAuthorizedServiceResourcesRequest req, CancellationToken ct)
    {
        var result = await _sender.Send(new SearchAuthorizedServiceResourcesQuery
        {
            AcceptedLanguages = req.AcceptedLanguages?.AcceptedLanguage,
            Parties = req.Party?.ToArray()
        }, ct);

        await Send.OkAsync(result, ct);
    }
}

[OpenApiTypeName(nameof(SearchAuthorizedServiceResourcesRequest))]
public sealed class SearchAuthorizedServiceResourcesRequest
{
    /// <summary>
    /// Filter by one or more party URNs. Parties the caller is not authorized for are silently ignored.
    /// </summary>
    public List<string>? Party { get; set; }

    [FromHeader(Constants.AcceptLanguage, isRequired: false)]
    public AcceptedLanguages? AcceptedLanguages { get; set; } = null;
}
