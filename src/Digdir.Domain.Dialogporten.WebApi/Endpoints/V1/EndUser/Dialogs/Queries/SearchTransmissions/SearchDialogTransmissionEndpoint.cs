using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.SearchTransmissions;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using FastEndpoints;
using MediatR;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser.Dialogs.Queries.SearchTransmissions;

public sealed class SearchDialogTransmissionEndpoint : Endpoint<SearchTransmissionRequest, List<TransmissionDto>>
{
    private readonly ISender _sender;

    public SearchDialogTransmissionEndpoint(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public override void Configure()
    {
        Get("dialogs/{dialogId}/transmissions");
        Policies(AuthorizationPolicy.EndUser);
        Group<EndUserGroup>();

        Description(b => b.ProducesOneOf<List<TransmissionDto>>(
            StatusCodes.Status200OK,
            StatusCodes.Status410Gone,
            StatusCodes.Status404NotFound));
    }

    public override async Task HandleAsync(SearchTransmissionRequest req, CancellationToken ct)
    {
        var query = new SearchTransmissionQuery
        {
            DialogId = req.DialogId,
            AcceptedLanguages = req.AcceptedLanguages?.AcceptedLanguage
        };

        var result = await _sender.Send(query, ct);
        await result.Match(
            dto => SendOkAsync(dto, ct),
            notFound => this.NotFoundAsync(notFound, ct),
            deleted => this.GoneAsync(deleted, ct),
            forbidden => this.ForbiddenAsync(forbidden, ct));
    }
}

public sealed class SearchTransmissionRequest
{
    public Guid DialogId { get; set; }

    [FromHeader(Constants.AcceptLanguage, isRequired: false)]
    public AcceptedLanguages? AcceptedLanguages { get; set; } = null;
}
