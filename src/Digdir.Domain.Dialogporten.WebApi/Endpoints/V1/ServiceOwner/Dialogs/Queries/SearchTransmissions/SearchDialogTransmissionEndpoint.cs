using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.SearchTransmissions;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using FastEndpoints;
using MediatR;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Queries.SearchTransmissions;

public sealed class SearchDialogTransmissionEndpoint : Endpoint<SearchTransmissionQuery, List<TransmissionDto>>
{
    private readonly ISender _sender;

    public SearchDialogTransmissionEndpoint(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public override void Configure()
    {
        Get("dialogs/{dialogId}/transmissions");
        Policies(AuthorizationPolicy.ServiceProvider);
        Group<ServiceOwnerGroup>();

        Description(b => b.ProducesOneOf<List<TransmissionDto>>(
            StatusCodes.Status200OK,
            StatusCodes.Status404NotFound,
            StatusCodes.Status410Gone));
    }

    public override async Task HandleAsync(SearchTransmissionQuery req, CancellationToken ct)
    {
        var result = await _sender.Send(req, ct);
        await result.Match(
            dto => SendOkAsync(dto, ct),
            notFound => this.NotFoundAsync(notFound, ct),
            deleted => this.GoneAsync(deleted, ct));
    }
}
