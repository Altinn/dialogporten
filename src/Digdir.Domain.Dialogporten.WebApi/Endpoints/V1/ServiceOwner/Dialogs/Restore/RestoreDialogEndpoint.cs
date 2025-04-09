using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Restore;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using FastEndpoints;
using Mediator;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Restore;

public sealed class RestoreDialogEndpoint : Endpoint<RestoreDialogRequest>
{
    private readonly ISender _sender;

    public RestoreDialogEndpoint(ISender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Post("dialogs/{dialogId}/actions/restore");
        Policies(AuthorizationPolicy.ServiceProvider);
        Group<ServiceOwnerGroup>();

        Description(b => b
            .Accepts<RestoreDialogRequest>()
            .ProducesOneOf(
                StatusCodes.Status204NoContent,
                StatusCodes.Status404NotFound,
                StatusCodes.Status412PreconditionFailed));
    }

    public override async Task HandleAsync(RestoreDialogRequest req, CancellationToken ct)
    {
        var command = new RestoreDialogCommand
        {
            DialogId = req.DialogId,
            IfMatchDialogRevision = req.IfMatchDialogRevision,
            IsSilentUpdate = req.IsSilentUpdate ?? false
        };
        var result = await _sender.Send(command, ct);
        await result.Match(
            success =>
            {
                HttpContext.Response.Headers.Append(Constants.ETag, success.Revision.ToString());
                return SendNoContentAsync(ct);
            },
            notFound => this.NotFoundAsync(notFound, ct),
            concurrencyError => this.PreconditionFailed(ct));
    }
}

public sealed class RestoreDialogRequest
{
    public Guid DialogId { get; init; }

    [FromHeader(headerName: Constants.IfMatch, isRequired: false, removeFromSchema: true)]
    public Guid? IfMatchDialogRevision { get; init; }

    [HideFromDocs]
    public bool? IsSilentUpdate { get; init; }
}
