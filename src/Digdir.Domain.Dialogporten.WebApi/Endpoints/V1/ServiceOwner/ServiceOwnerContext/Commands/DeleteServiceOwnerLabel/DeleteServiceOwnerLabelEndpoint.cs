using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.ServiceOwnerContext.Commands.Update;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.ServiceOwnerContext.Queries.GetServiceOwnerLabels;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Common.CostManagement;
using FastEndpoints;
using MediatR;
using ServiceOwnerLabelDto =
    Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.ServiceOwnerContext.Commands.Update.
    ServiceOwnerLabelDto;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.ServiceOwnerContext.Commands.DeleteServiceOwnerLabel;

[CostTracked(TransactionType.SetDialogLabel)]
public sealed class DeleteServiceOwnerLabelEndpoint : Endpoint<DeleteServiceOwnerLabelRequest>
{
    private readonly ISender _sender;

    public DeleteServiceOwnerLabelEndpoint(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public override void Configure()
    {
        Delete("dialogs/{dialogId}/context/labels/{label}");
        Policies(AuthorizationPolicy.ServiceProvider);
        Group<ServiceOwnerGroup>();

        Description(b => b.ProducesOneOf(
            StatusCodes.Status204NoContent,
            StatusCodes.Status404NotFound,
            StatusCodes.Status412PreconditionFailed));
    }

    public override async Task HandleAsync(DeleteServiceOwnerLabelRequest req, CancellationToken ct)
    {
        var getServiceOwnerLabelQuery = new GetServiceOwnerLabelsQuery { DialogId = req.DialogId };

        var getServiceOwnerLabelResult = await _sender.Send(getServiceOwnerLabelQuery, ct);
        if (!getServiceOwnerLabelResult.TryPickT0(out var serviceOwnerLabelsResult, out var dialogNotFound))
        {
            await this.NotFoundAsync(dialogNotFound, ct);
            return;
        }

        var existingLabels = serviceOwnerLabelsResult.Labels
            .Select(x => new ServiceOwnerLabelDto { Value = x.Value })
            .ToList();

        var labelToRemove = existingLabels.FirstOrDefault(x => x.Value.Equals(req.Label, StringComparison.OrdinalIgnoreCase));
        if (labelToRemove == null)
        {
            await this.NotFoundAsync(new EntityNotFound(nameof(DeleteServiceOwnerLabelRequest.Label), [req.Label]), ct);
            return;
        }

        existingLabels.Remove(labelToRemove);

        var command = new UpdateDialogServiceOwnerContextCommand
        {
            DialogId = req.DialogId,
            IfMatchServiceOwnerContextRevision = req.IfMatchServiceOwnerContextRevision,
            Dto = new UpdateServiceOwnerContextDto
            {
                ServiceOwnerLabels = existingLabels
            }
        };

        var result = await _sender.Send(command, ct);

        await result.Match(success =>
            {
                HttpContext.Response.Headers.Append(Constants.ETag, success.Revision.ToString());
                return SendNoContentAsync(ct);
            },
            validationError => this.BadRequestAsync(validationError, ct),
            notFound => this.NotFoundAsync(notFound, ct),
            domainError => this.UnprocessableEntityAsync(domainError, ct),
            concurrencyError => this.PreconditionFailed(ct));
    }
}

public sealed class DeleteServiceOwnerLabelRequest
{
    public Guid DialogId { get; set; }

    public string Label { get; set; } = null!;

    [FromHeader(headerName: Constants.IfMatch, isRequired: false, removeFromSchema: true)]
    public Guid? IfMatchServiceOwnerContextRevision { get; set; }
}
