using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.DialogSeenLogs.Queries.Get;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using FastEndpoints;
using MediatR;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser.DialogSeenLogs.Get;

public sealed class GetDialogSeenLogEndpoint : Endpoint<GetSeenLogQuery, SeenLogDto>
{
    private readonly ISender _sender;

    public GetDialogSeenLogEndpoint(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public override void Configure()
    {
        Get("dialogs/{dialogId}/seenlog/{seenLogId}");
        Policies(AuthorizationPolicy.EndUser);
        Group<EndUserGroup>();
        Description(d => d.ProducesOneOf<SeenLogDto>(
            StatusCodes.Status200OK,
            StatusCodes.Status404NotFound));
    }

    public override async Task HandleAsync(GetSeenLogQuery req, CancellationToken ct)
    {
        var result = await _sender.Send(req, ct);
        await result.Match(
            dto => SendOkAsync(dto, ct),
            notFound => this.NotFoundAsync(notFound, ct),
            deleted => this.GoneAsync(deleted, ct),
            forbiden => this.ForbiddenAsync(forbiden, ct));
    }
}
