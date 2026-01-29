using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.UpdateTransmission;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using FastEndpoints;
using MediatR;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Commands.UpdateTransmission;

public sealed class UpdateDialogTransmissionEndpoint : Endpoint<UpdateTransmissionRequest>
{
    private readonly ISender _sender;

    public UpdateDialogTransmissionEndpoint(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public override void Configure()
    {
        Put("dialogs/{dialogId}/transmissions/{transmissionId}");
        Policies(AuthorizationPolicy.ServiceProvider);
        Group<ServiceOwnerGroup>();

        Description(b => b.ProducesOneOf(
            StatusCodes.Status204NoContent,
            StatusCodes.Status400BadRequest,
            StatusCodes.Status403Forbidden,
            StatusCodes.Status404NotFound,
            StatusCodes.Status409Conflict,
            StatusCodes.Status410Gone,
            StatusCodes.Status412PreconditionFailed,
            StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(UpdateTransmissionRequest req, CancellationToken ct)
    {
        var command = new UpdateTransmissionCommand
        {
            DialogId = req.DialogId,
            TransmissionId = req.TransmissionId,
            IfMatchDialogRevision = req.IfMatchDialogRevision,
            Dto = req.Dto,
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
            validationFailed => this.BadRequestAsync(validationFailed, ct),
            forbidden => this.ForbiddenAsync(forbidden, ct),
            domainError => this.UnprocessableEntityAsync(domainError, ct),
            concurrencyError => this.PreconditionFailed(ct),
            conflict => this.ConflictAsync(conflict, ct));
    }
}

public sealed class UpdateTransmissionRequest
{
    public Guid DialogId { get; set; }
    public Guid TransmissionId { get; set; }

    [FromBody]
    public UpdateTransmissionDto Dto { get; set; } = null!;

    [FromHeader(headerName: Constants.IfMatch, isRequired: false, removeFromSchema: true)]
    public Guid? IfMatchDialogRevision { get; set; }

    public bool? IsSilentUpdate { get; init; }
}
