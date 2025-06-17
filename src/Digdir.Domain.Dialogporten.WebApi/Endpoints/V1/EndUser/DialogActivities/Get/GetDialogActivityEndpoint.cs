using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.DialogActivities.Queries.Get;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using FastEndpoints;
using MediatR;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser.DialogActivities.Get;

public sealed class GetDialogActivityEndpoint : Endpoint<GetActivityQuery, ActivityDto>
{
    private readonly ISender _sender;

    public GetDialogActivityEndpoint(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public override void Configure()
    {
        Get("dialogs/{dialogId}/activities/{activityId}");
        Policies(AuthorizationPolicy.EndUser);
        Group<EndUserGroup>();

        Description(b => b.ProducesOneOf<ActivityDto>(
            StatusCodes.Status200OK,
            StatusCodes.Status404NotFound));
    }

    public override async Task HandleAsync(GetActivityQuery req, CancellationToken ct)
    {
        var result = await _sender.Send(req, ct);
        await result.Match(
            dto => SendOkAsync(dto, ct),
            notFound => this.NotFoundAsync(notFound, ct),
            deleted => this.GoneAsync(deleted, ct),
            forbidden => this.ForbiddenAsync(forbidden, ct));
    }
}
