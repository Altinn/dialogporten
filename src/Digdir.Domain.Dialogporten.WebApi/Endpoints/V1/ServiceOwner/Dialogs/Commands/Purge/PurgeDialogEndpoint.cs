using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Purge;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.CostManagement;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using FastEndpoints;
using MediatR;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Commands.Purge;

[CostTracked(TransactionType.HardDeleteDialog)]
public sealed class PurgeDialogEndpoint : Endpoint<PurgeDialogRequest>
{
    private readonly ISender _sender;

    public PurgeDialogEndpoint(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public override void Configure()
    {
        Post("dialogs/{dialogId}/actions/purge");
        Policies(AuthorizationPolicy.ServiceProvider);
        Group<ServiceOwnerGroup>();

        Description(b => b
            .Accepts<PurgeDialogRequest>()
            .ProducesOneOf(
            StatusCodes.Status204NoContent,
            StatusCodes.Status404NotFound,
            StatusCodes.Status412PreconditionFailed));
    }

    public override async Task HandleAsync(PurgeDialogRequest req, CancellationToken ct)
    {
        var command = new PurgeDialogCommand
        {
            DialogId = req.DialogId,
            IfMatchDialogRevision = req.IfMatchDialogRevision,
            IsSilentUpdate = req.IsSilentUpdate ?? false
        };

        var result = await _sender.Send(command, ct);
        await result.Match(
            success => SendNoContentAsync(ct),
            notFound => this.NotFoundAsync(notFound, ct),
            forbidden => this.ForbiddenAsync(forbidden, ct),
            concurrencyError => this.PreconditionFailed(ct),
            validationError => this.BadRequestAsync(validationError, ct));
    }
}

public sealed class PurgeDialogRequest
{
    public Guid DialogId { get; init; }

    [FromHeader(headerName: Constants.IfMatch, isRequired: false, removeFromSchema: true)]
    public Guid? IfMatchDialogRevision { get; init; }

    [HideFromDocs]
    public bool? IsSilentUpdate { get; init; }
}
