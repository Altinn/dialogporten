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
            StatusCodes.Status403Forbidden,
            StatusCodes.Status404NotFound,
            StatusCodes.Status410Gone,
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
            success =>
            {
                HttpContext.Response.Headers.Append(Constants.ETag, success.Revision.ToString());
                return SendNoContentAsync(ct);
            },
            entityNotFound => this.NotFoundAsync(entityNotFound, ct),
            deleted => this.GoneAsync(deleted, ct),
            forbidden => this.ForbiddenAsync(forbidden, ct),
            concurrencyError => this.PreconditionFailed(ct),
            conflict => this.ConflictAsync(conflict, ct));
    }

}

public sealed class FreezeDialogRequest
{
    public Guid DialogId { get; init; }

    [FromHeader(headerName: Constants.IfMatch, isRequired: false, removeFromSchema: true)]
    public Guid? IfMatchDialogRevision { get; init; }
}
