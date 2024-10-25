using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.DialogSeenLogs.Queries.Search;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using FastEndpoints;
using MediatR;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser.DialogSeenLogs.Search;

public sealed class SearchDialogSeenLogEndpoint : Endpoint<SearchDialogSeenLogQuery, List<SearchDialogSeenLogDto>>
{
    private readonly ISender _sender;

    public SearchDialogSeenLogEndpoint(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public override void Configure()
    {
        Get("dialogs/{dialogId}/seenlog");
        Policies(AuthorizationPolicy.EndUser);
        Group<EndUserGroup>();

        Description(d => SearchDialogSeenLogSwaggerConfig.SetDescription(d, GetType()));
    }

    public override async Task HandleAsync(SearchDialogSeenLogQuery req, CancellationToken ct)
    {
        var result = await _sender.Send(req, ct);
        await result.Match(
            dto => SendOkAsync(dto, ct),
            notFound => this.NotFoundAsync(notFound, ct),
            deleted => this.GoneAsync(deleted, ct),
            forbidden => this.ForbiddenAsync(forbidden, ct));
    }
}
