using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.DialogSystemLabels.Commands.BulkSet;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using FastEndpoints;
using MediatR;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.DialogSystemLabels.BulkSet;

public sealed class BulkSetDialogSystemLabelsEndpoint(ISender sender) : Endpoint<BulkSetDialogSystemLabelsRequest>
{
    private readonly ISender _sender = sender ?? throw new ArgumentNullException(nameof(sender));

    public override void Configure()
    {
        Post("dialogs/endusercontext/systemlabels/actions/bulkset");
        Policies(AuthorizationPolicy.ServiceProvider);
        Group<ServiceOwnerGroup>();

        Description(b => b.ProducesOneOf(
            StatusCodes.Status204NoContent,
            StatusCodes.Status400BadRequest,
            StatusCodes.Status403Forbidden,
            StatusCodes.Status412PreconditionFailed));
    }

    public override async Task HandleAsync(BulkSetDialogSystemLabelsRequest req, CancellationToken ct)
    {
        var command = new BulkSetSystemLabelCommand
        {
            DialogIds = req.DialogIds,
            EnduserId = req.EnduserId,
            SystemLabels = req.SystemLabels,
            IfMatchEnduserContextRevision = req.IfMatchEnduserContextRevision
        };

        var result = await _sender.Send(command, ct);
        await result.Match(
            _ => SendNoContentAsync(ct),
            forbidden => this.ForbiddenAsync(forbidden, ct),
            domainError => this.UnprocessableEntityAsync(domainError, ct),
            validationError => this.BadRequestAsync(validationError, ct),
            concurrencyError => this.PreconditionFailed(ct));
    }
}

public sealed class BulkSetDialogSystemLabelsRequest
{
    [FromHeader(headerName: Constants.IfMatch, isRequired: false, removeFromSchema: true)]
    public Guid? IfMatchEnduserContextRevision { get; init; }

    [QueryParam]
    public string EnduserId { get; init; } = string.Empty;

    public IReadOnlyCollection<Guid> DialogIds { get; init; } = [];

    public IReadOnlyCollection<SystemLabel.Values> SystemLabels { get; init; } = [];
}
