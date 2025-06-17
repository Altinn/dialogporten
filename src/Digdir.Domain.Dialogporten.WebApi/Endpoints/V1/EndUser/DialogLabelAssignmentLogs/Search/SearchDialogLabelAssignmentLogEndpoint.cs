using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.DialogLabelAssignmentLog.Queries.Search;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using FastEndpoints;
using MediatR;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser.DialogLabelAssignmentLogs.Search;

public sealed class SearchDialogLabelAssignmentLogEndpoint : Endpoint<SearchLabelAssignmentLogQuery, List<LabelAssignmentLogDto>>
{
    private readonly ISender _sender;

    public SearchDialogLabelAssignmentLogEndpoint(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }
    public override void Configure()
    {
        Get("dialogs/{dialogId}/context/labellog");
        Policies(AuthorizationPolicy.EndUser);
        Group<EndUserGroup>();

        Description(d => d.ProducesOneOf<List<LabelAssignmentLogDto>>(
            StatusCodes.Status200OK,
            StatusCodes.Status404NotFound,
            StatusCodes.Status410Gone));
    }
    public override async Task HandleAsync(SearchLabelAssignmentLogQuery req, CancellationToken ct)
    {
        var result = await _sender.Send(req, ct);
        await result.Match(
            dto => SendOkAsync(dto, ct),
            notFound => this.NotFoundAsync(notFound, ct),
            deleted => this.GoneAsync(deleted, ct),
            forbidden => this.ForbiddenAsync(forbidden, ct));
    }
}
