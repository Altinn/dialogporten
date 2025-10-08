using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.GetTransmission;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using FastEndpoints;
using MediatR;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser.Dialogs.Queries.GetTransmission;

public sealed class GetDialogTransmissionEndpoint : Endpoint<GetTransmissionRequest, TransmissionDto>
{
    private readonly ISender _sender;

    public GetDialogTransmissionEndpoint(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public override void Configure()
    {
        Get("dialogs/{dialogId}/transmissions/{transmissionId}");
        Policies(AuthorizationPolicy.EndUser);
        Group<EndUserGroup>();

        Description(b => b.ProducesOneOf<TransmissionDto>(
            StatusCodes.Status200OK,
            StatusCodes.Status410Gone,
            StatusCodes.Status404NotFound));
    }

    public override async Task HandleAsync(GetTransmissionRequest req, CancellationToken ct)
    {
        var query = new GetTransmissionQuery
        {
            DialogId = req.DialogId,
            TransmissionId = req.TransmissionId,
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

public sealed class GetTransmissionRequest
{
    public Guid DialogId { get; set; }
    public Guid TransmissionId { get; set; }

    [FromHeader(Constants.AcceptLanguage, isRequired: false)]
    public AcceptedLanguages? AcceptedLanguages { get; set; } = null;
}
