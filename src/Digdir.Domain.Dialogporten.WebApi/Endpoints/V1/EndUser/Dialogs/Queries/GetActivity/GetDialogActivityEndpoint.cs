using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.GetActivity;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using FastEndpoints;
using MediatR;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser.Dialogs.Queries.GetActivity;

public sealed class GetDialogActivityEndpoint : Endpoint<GetActivityRequest, ActivityDto>
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

    public override async Task HandleAsync(GetActivityRequest req, CancellationToken ct)
    {
        var query = new GetActivityQuery
        {
            DialogId = req.DialogId,
            ActivityId = req.ActivityId,
            AcceptedLanguages = req.AcceptedLanguages?.AcceptedLanguage,
        };
        var result = await _sender.Send(query, ct);
        await result.Match(
            dto => SendOkAsync(dto, ct),
            notFound => this.NotFoundAsync(notFound, ct),
            deleted => this.GoneAsync(deleted, ct),
            forbidden => this.ForbiddenAsync(forbidden, ct));
    }
}

public sealed class GetActivityRequest
{
    public Guid DialogId { get; set; }

    public Guid ActivityId { get; set; }

    [FromHeader("Accept-Language", isRequired: false)]
    public AcceptedLanguages? AcceptedLanguages { get; set; } = null;
}
