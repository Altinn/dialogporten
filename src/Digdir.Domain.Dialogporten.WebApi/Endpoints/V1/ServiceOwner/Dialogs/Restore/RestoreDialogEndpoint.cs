using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Restore;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using FastEndpoints;
using MediatR;

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

        // Amund: Husk å sjækk om OneOf e rætt
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
            IfMatchDialogRevision = req.IfMatchDialogRevision
        };
        var result = await _sender.Send(command, ct);
        // Amund: Husk
        return result.Match();
    }
}

public sealed class RestoreDialogRequest
{
    public Guid DialogId { get; init; }

    [FromHeader(headerName: Constants.IfMatch, isRequired: false, removeFromSchema: true)]
    public Guid? IfMatchDialogRevision { get; init; }
}
