using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.DialogActivities.Queries.List;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using FastEndpoints;
using MediatR;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.DialogActivity;

public class ListDialogActivityEndpoint : Endpoint<ListDialogActivityQuery, List<ListDialogActivityDto>>
{
    private readonly ISender _sender;

    public ListDialogActivityEndpoint(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public override void Configure()
    {
        Get("dialogs/{dialogId}/activities");
        Policies(AuthorizationPolicy.Serviceprovider);
        Group<ServiceOwnerGroup>();

        Description(b => b
            .OperationId("GetDialogActivityListSO")
            .ProducesOneOf(
                StatusCodes.Status200OK,
                StatusCodes.Status404NotFound)
        );
    }

    public override async Task HandleAsync(ListDialogActivityQuery req, CancellationToken ct)
    {
        var result = await _sender.Send(req, ct);
        await result.Match(
            dto => SendOkAsync(dto, ct),
            notFound => this.NotFoundAsync(notFound, ct));
    }
}
public sealed class ListDialogActivityEndpointSummary : Summary<ListDialogActivityEndpoint, ListDialogActivityQuery>
{
    public ListDialogActivityEndpointSummary()
    {
        Summary = "Gets a list of dialog activities";
        Description = """
                Gets the list of activities belonging to a dialog
                """;
        Responses[StatusCodes.Status200OK] = string.Format(Constants.SwaggerSummary.ReturnedResult, "activity list");
        Responses[StatusCodes.Status401Unauthorized] = Constants.SwaggerSummary.ServiceOwnerAuthenticationFailure;
        Responses[StatusCodes.Status403Forbidden] = string.Format(Constants.SwaggerSummary.AccessDeniedToDialog, "get");
        Responses[StatusCodes.Status404NotFound] = Constants.SwaggerSummary.DialogNotFound;
    }
}
