using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using FastEndpoints;
using MediatR;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Queries.Search;

public sealed class SearchDialogEndpoint : Endpoint<SearchDialogQuery, PaginatedList<DialogDto>>
{
    private readonly ISender _sender;

    public SearchDialogEndpoint(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public override void Configure()
    {
        Get("dialogs");
        Policies(AuthorizationPolicy.ServiceProviderSearch);
        Group<ServiceOwnerGroup>();

        Description(b => b.ClearDefaultProduces(StatusCodes.Status403Forbidden));
    }

    public override async Task HandleAsync(SearchDialogQuery req, CancellationToken ct)
    {
        var result = await _sender.Send(req, ct);
        await result.Match(
            paginatedDto => SendOkAsync(paginatedDto, ct),
            validationError => this.BadRequestAsync(validationError, ct));
    }
}
