using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.SearchEndUserContext;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using FastEndpoints;
using MediatR;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Queries.SearchEndUserContext;

public sealed class SearchDialogEndUserContextEndpoint : Endpoint<SearchDialogEndUserContextQuery, PaginatedList<DialogEndUserContextItemDto>>
{
    private readonly ISender _sender;

    public SearchDialogEndUserContextEndpoint(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public override void Configure()
    {
        Get("dialogs/endusercontext");
        Policies(AuthorizationPolicy.ServiceProviderSearch);
        Group<ServiceOwnerGroup>();

        Description(b => b.ClearDefaultProduces(StatusCodes.Status403Forbidden));
    }

    public override async Task HandleAsync(SearchDialogEndUserContextQuery req, CancellationToken ct)
    {
        var result = await _sender.Send(req, ct);
        await result.Match(
            paginatedDto => SendOkAsync(paginatedDto, ct),
            validationError => this.BadRequestAsync(validationError, ct));
    }
}
