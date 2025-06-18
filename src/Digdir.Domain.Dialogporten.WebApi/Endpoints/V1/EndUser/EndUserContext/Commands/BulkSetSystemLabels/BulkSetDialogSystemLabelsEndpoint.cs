using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Commands.BulkSetSystemLabels;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using FastEndpoints;
using MediatR;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser.EndUserContext.Commands.BulkSetSystemLabels;

public sealed class BulkSetDialogSystemLabelsEndpoint(ISender sender) : Endpoint<BulkSetSystemLabelCommand>
{
    private readonly ISender _sender = sender ?? throw new ArgumentNullException(nameof(sender));

    public override void Configure()
    {
        Post("dialogs/context/systemlabels/actions/bulkset");
        Policies(AuthorizationPolicy.EndUser);
        Group<EndUserGroup>();

        Description(b => b.ProducesOneOf(
            StatusCodes.Status204NoContent,
            StatusCodes.Status400BadRequest,
            StatusCodes.Status403Forbidden,
            StatusCodes.Status412PreconditionFailed,
            StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(BulkSetSystemLabelCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        await result.Match(
            _ => SendNoContentAsync(ct),
            notFound => this.NotFoundAsync(notFound, ct),
            domainError => this.UnprocessableEntityAsync(domainError, ct),
            validationError => this.BadRequestAsync(validationError, ct),
            _ => this.PreconditionFailed(ct));
    }
}
