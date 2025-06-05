using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.ServiceOwnerContext.Commands.Update;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.ServiceOwnerContext.ServiceOwnerLabels.Queries.Get;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using FastEndpoints;
using MediatR;
using ServiceOwnerLabelDto =
    Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.ServiceOwnerContext.Commands.Update.
    ServiceOwnerLabelDto;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.ServiceOwnerContext.ServiceOwnerLabels.Create;

public sealed class CreateServiceOwnerLabelEndpoint : Endpoint<CreateServiceOwnerLabelRequest>
{
    private readonly ISender _sender;

    public CreateServiceOwnerLabelEndpoint(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public override void Configure()
    {
        Post("dialogs/{dialogId}/context/labels");
        Policies(AuthorizationPolicy.ServiceProvider);

        Group<ServiceOwnerGroup>();

        Description(b => b.ProducesOneOf(
            StatusCodes.Status204NoContent,
            StatusCodes.Status400BadRequest,
            StatusCodes.Status412PreconditionFailed));
    }

    public override async Task HandleAsync(CreateServiceOwnerLabelRequest req, CancellationToken ct)
    {
        var getServiceOwnerLabelQuery = new GetServiceOwnerLabelsQuery
        {
            DialogId = req.DialogId
        };

        var getServiceOwnerLabelResult = await _sender.Send(getServiceOwnerLabelQuery, ct);
        if (!getServiceOwnerLabelResult.TryPickT0(out var serviceOwnerLabelsResult, out var dialogNotFound))
        {
            await this.NotFoundAsync(dialogNotFound, ct);
            return;
        }

        if (req.IfMatchServiceOwnerContextRevision.HasValue &&
            req.IfMatchServiceOwnerContextRevision.Value != serviceOwnerLabelsResult.Revision)
        {
            await this.PreconditionFailed(ct);
            return;
        }

        var command = new UpdateDialogServiceOwnerContextCommand
        {
            IfMatchServiceOwnerContextRevision = req.IfMatchServiceOwnerContextRevision,
            DialogId = req.DialogId,
            Dto = new UpdateServiceOwnerContextDto
            {
                ServiceOwnerLabels =
                [
                    ..serviceOwnerLabelsResult.Labels
                        .Select(x => new ServiceOwnerLabelDto { Value = x.Value }),
                    new ServiceOwnerLabelDto { Value = req.Dto.Value }
                ]
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

public sealed class CreateServiceOwnerLabelRequest
{
    public Guid DialogId { get; set; }

    [FromBody]
    public LabelDto Dto { get; set; } = null!;

    [FromHeader(headerName: Constants.IfMatch, isRequired: false, removeFromSchema: true)]
    public Guid? IfMatchServiceOwnerContextRevision { get; set; }
}

public sealed class LabelDto
{
    public string Value { get; set; } = null!;
}
