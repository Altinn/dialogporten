using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.EndUserContext.Commands.SetSystemLabels;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using FastEndpoints;
using MediatR;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.EndUserContext.Commands.SetSystemLabel;

public sealed class SetDialogSystemLabelsEndpoint(ISender sender) : Endpoint<SetDialogSystemLabelRequest>
{
    private readonly ISender _sender = sender ?? throw new ArgumentNullException(nameof(sender));

    public override void Configure()
    {
        Put("dialogs/{dialogId}/endusercontext/systemlabels");
        Policies(AuthorizationPolicy.ServiceProvider);
        Group<ServiceOwnerGroup>();

        Description(b => b.ProducesOneOf(
            StatusCodes.Status204NoContent,
            StatusCodes.Status400BadRequest,
            StatusCodes.Status403Forbidden,
            StatusCodes.Status404NotFound,
            StatusCodes.Status410Gone,
            StatusCodes.Status412PreconditionFailed,
            StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(SetDialogSystemLabelRequest req, CancellationToken ct)
    {
        var command = new SetSystemLabelCommand
        {
            DialogId = req.DialogId,
            EndUserId = req.EnduserId,
            SystemLabels = req.SystemLabels,
            IfMatchEndUserContextRevision = req.IfMatchEnduserContextRevision
        };

        var result = await _sender.Send(command, ct);
        await result.Match(
            success =>
            {
                HttpContext.Response.Headers.Append(Constants.ETag, success.Revision.ToString());
                return SendNoContentAsync(ct);
            },
            notFound => this.NotFoundAsync(notFound, ct),
            deleted => this.GoneAsync(deleted, ct),
            domainError => this.UnprocessableEntityAsync(domainError, ct),
            validationError => this.BadRequestAsync(validationError, ct),
            concurrencyError => this.PreconditionFailed(ct));
    }
}

public sealed class SetDialogSystemLabelRequest
{
    [FromHeader(headerName: Constants.IfMatch, isRequired: false, removeFromSchema: true)]
    public Guid? IfMatchEnduserContextRevision { get; set; }

    [QueryParam]
    public string EnduserId { get; init; } = string.Empty;

    public Guid DialogId { get; set; }

    public IReadOnlyCollection<SystemLabel.Values> SystemLabels { get; set; } = [];
}
