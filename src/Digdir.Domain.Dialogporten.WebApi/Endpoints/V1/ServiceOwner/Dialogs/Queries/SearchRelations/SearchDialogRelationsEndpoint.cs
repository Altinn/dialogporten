using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.SearchRelations;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using FastEndpoints;
using MediatR;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Queries.SearchRelations;

[OpenApiOperationId("SearchDialogRelations")]
[OpenApiExtras(
    scopes: [AuthorizationScope.ServiceProvider],
    securitySchemes: [OpenApiSecurityScheme.MaskinportenSecurityScheme])
]
public sealed class SearchDialogRelationsEndpoint : Endpoint<SearchDialogRelationsQuery, PaginatedList<DialogRelationDto>>
{
    private readonly ISender _sender;

    public SearchDialogRelationsEndpoint(ISender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);

        _sender = sender;
    }

    public override void Configure()
    {
        Get("dialogs/{dialogId}/relations");
        Policies(AuthorizationPolicy.ServiceProvider);
        Group<ServiceOwnerGroup>();

        Description(b => b
            .Produces<PaginatedList<DialogRelationDto>>()
            .ProducesDpProblemFor(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status401Unauthorized,
                StatusCodes.Status403Forbidden,
                StatusCodes.Status404NotFound
            ));
    }

    public override async Task HandleAsync(SearchDialogRelationsQuery req, CancellationToken ct)
    {
        var result = await _sender.Send(req, ct);
        await result.Match(
            page => Send.OkAsync(page, ct),
            notFound => this.NotFoundAsync(notFound, ct),
            validationError => this.BadRequestAsync(validationError, ct));
    }
}
