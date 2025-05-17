using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.ServiceOwnerLabels.Commands.Set;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using FastEndpoints;
using MediatR;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.DialogServiceOwnerLabels.Set;

public sealed class SetServiceOwnerLabelsEndpoint(ISender sender) : Endpoint<SetServiceOwnerLabelsRequest>
{
    private readonly ISender _sender = sender ?? throw new ArgumentNullException(nameof(sender));

    public override void Configure()
    {
        Put("dialogs/{dialogId}/serviceownerlabels");
        Policies(AuthorizationPolicy.ServiceProvider);
        Group<ServiceOwnerGroup>();

        Description(b => b.ProducesOneOf(
            StatusCodes.Status204NoContent,
            StatusCodes.Status400BadRequest,
            StatusCodes.Status404NotFound,
            StatusCodes.Status412PreconditionFailed,
            StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(SetServiceOwnerLabelsRequest req, CancellationToken ct)
    {
        var command = new SetServiceOwnerLabelsCommand
        {
            DialogId = req.DialogId,
            IfMatchServiceOwnerContextRevision = req.IfMatchServiceOwnerContextRevision,
            ServiceOwnerLabels = req.ServiceOwnerLabels
        };

        var result = await _sender.Send(command, ct);
        await result.Match(
            success =>
            {
                HttpContext.Response.Headers.Append(Constants.ETag, success.Revision.ToString());
                return SendNoContentAsync(ct);
            },
            notFound => this.NotFoundAsync(notFound, ct),
            domainError => this.UnprocessableEntityAsync(domainError, ct),
            validationError => this.BadRequestAsync(validationError, ct),
            concurrencyError => this.PreconditionFailed(ct));
    }
}

public sealed class SetServiceOwnerLabelsRequest
{
    [FromHeader(headerName: Constants.IfMatch, isRequired: false, removeFromSchema: true)]
    public Guid? IfMatchServiceOwnerContextRevision { get; set; }

    public Guid DialogId { get; set; }

    public List<ServiceOwnerLabelDto> ServiceOwnerLabels { get; set; } = [];
}
