using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Delete;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using FastEndpoints;
using MediatR;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Commands.Delete;

public sealed class DeleteDialogEndpoint : Endpoint<DeleteDialogRequest>
{
    private readonly ISender _sender;

    public DeleteDialogEndpoint(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public override void Configure()
    {
        Delete("dialogs/{dialogId}");
        Policies(AuthorizationPolicy.ServiceProvider);
        Group<ServiceOwnerGroup>();

        Description(b => b.ProducesOneOf(
            StatusCodes.Status204NoContent,
            StatusCodes.Status400BadRequest,
            StatusCodes.Status404NotFound,
            StatusCodes.Status410Gone,
            StatusCodes.Status412PreconditionFailed));
    }

    public override async Task HandleAsync(DeleteDialogRequest req, CancellationToken ct)
    {
        var command = new DeleteDialogCommand
        {
            Id = req.DialogId,
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
            gone => this.GoneAsync(gone, ct),
            forbidden => this.ForbiddenAsync(forbidden, ct),
            concurrencyError => this.PreconditionFailed(ct),
            conflict => this.ConflictAsync(conflict, ct));
    }
}

public sealed class DeleteDialogRequest
{
    public Guid DialogId { get; set; }

    [FromHeader(headerName: Constants.IfMatch, isRequired: false, removeFromSchema: true)]
    public Guid? IfMatchDialogRevision { get; set; }

    [HideFromDocs]
    public bool? IsSilentUpdate { get; init; }
}
