using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateTransmission;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.GetTransmission;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Queries.GetTransnission;
using FastEndpoints;
using MediatR;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;
using TransmissionDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.TransmissionDto;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Commands.CreateTransmission;

public sealed class CreateDialogTransmissionEndpoint : Endpoint<CreateTransmissionRequest>
{
    private readonly ISender _sender;

    public CreateDialogTransmissionEndpoint(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public override void Configure()
    {
        Post("dialogs/{dialogId}/transmissions");
        Policies(AuthorizationPolicy.ServiceProvider);
        Group<ServiceOwnerGroup>();

        Description(b => b.ProducesOneOf(
            StatusCodes.Status201Created,
            StatusCodes.Status400BadRequest,
            StatusCodes.Status404NotFound,
            StatusCodes.Status410Gone,
            StatusCodes.Status412PreconditionFailed,
            StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(CreateTransmissionRequest req, CancellationToken ct)
    {
        var command = new CreateTransmissionCommand
        {
            DialogId = req.DialogId,
            IfMatchDialogRevision = req.IfMatchDialogRevision,
            IsSilentUpdate = req.IsSilentUpdate ?? false,
            Transmissions = [req]
        };

        var result = await _sender.Send(command, ct);

        await result.Match(
            success =>
            {
                HttpContext.Response.Headers.Append(Constants.ETag, success.Revision.ToString());
                var transmissionId = success.TransmissionIds.First();
                return SendCreatedAtAsync<GetDialogTransmissionEndpoint>(
                    new GetTransmissionQuery { DialogId = req.DialogId, TransmissionId = transmissionId }, transmissionId,
                    cancellation: ct);
            },
            notFound => this.NotFoundAsync(notFound, ct),
            gone => this.GoneAsync(gone, ct),
            validationError => this.BadRequestAsync(validationError, ct),
            forbidden => this.ForbiddenAsync(forbidden, ct),
            domainError => this.UnprocessableEntityAsync(domainError, ct),
            concurrencyError => this.PreconditionFailed(cancellationToken: ct));
    }
}

public sealed class CreateTransmissionRequest : TransmissionDto
{
    public Guid DialogId { get; set; }

    [FromHeader(headerName: Constants.IfMatch, isRequired: false, removeFromSchema: true)]
    public Guid? IfMatchDialogRevision { get; set; }

    [HideFromDocs]
    public bool? IsSilentUpdate { get; init; }
}
