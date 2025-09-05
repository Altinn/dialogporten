using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Freeze;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using FastEndpoints;
using MediatR;
using AuthorizationPolicy = Digdir.Domain.Dialogporten.WebApi.Common.Authorization.AuthorizationPolicy;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Commands.Freeze;

public sealed class FreezeDialogEndpoint(ISender sender) : Endpoint<FreezeDialogRequest>
{
    private readonly ISender _sender = sender ?? throw new ArgumentNullException(nameof(sender));

    public override void Configure()
    {
        Post("dialogs/{dialogId}/actions/freeze");
        Policies(AuthorizationPolicy.ServiceProvider);
        Group<ServiceOwnerGroup>();

        Description(b => b
            .Accepts<FreezeDialogRequest>()
            .ProducesOneOf(
            StatusCodes.Status204NoContent,
            StatusCodes.Status400BadRequest,
            StatusCodes.Status404NotFound,
            StatusCodes.Status412PreconditionFailed));
    }
    public override async Task HandleAsync(FreezeDialogRequest req, CancellationToken ct)
    {
        var command = new FreezeDialogCommand
        {
            Id = req.DialogId,
            IfMatchDialogRevision = req.IfMatchDialogRevision,
        };

        var result = await _sender.Send(command, ct);
        await result.Match(
            success => SendNoContentAsync(ct),
            entityNotFound => this.NotFoundAsync(entityNotFound, ct),
            forbidden => this.ForbiddenAsync(forbidden, ct),
            concurrencyError => this.PreconditionFailed(ct));
    }

}

public sealed class FreezeDialogRequest
{
    public Guid DialogId { get; init; }

    [FromHeader(headerName: Constants.IfMatch, isRequired: false, removeFromSchema: true)]
    public Guid? IfMatchDialogRevision { get; init; }

    [HideFromDocs]
    public bool? IsSilentUpdate { get; init; }
}
