using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using FastEndpoints;
using MediatR;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser.Dialogs.Queries.Get;

public sealed class GetDialogEndpoint : Endpoint<GetDialogQuery, DialogDto>
{
    private readonly ISender _sender;

    public GetDialogEndpoint(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public override void Configure()
    {
        Get("dialogs/{dialogId}");
        Policies(AuthorizationPolicy.EndUser);
        Group<EndUserGroup>();

        Description(d => d.ProducesOneOf<DialogDto>(
            StatusCodes.Status200OK,
            StatusCodes.Status404NotFound));
    }

    public override async Task HandleAsync(GetDialogQuery req, CancellationToken ct)
    {
        var result = await _sender.Send(req, ct);
        await result.Match(
            dto =>
            {
                HttpContext.Response.Headers.ETag = dto.Revision.ToString();
                return SendOkAsync(dto, ct);
            },
            notFound => this.NotFoundAsync(notFound, ct),
            deleted => this.GoneAsync(deleted, ct),
            forbidden => this.ForbiddenAsync(forbidden, ct));
    }
}
