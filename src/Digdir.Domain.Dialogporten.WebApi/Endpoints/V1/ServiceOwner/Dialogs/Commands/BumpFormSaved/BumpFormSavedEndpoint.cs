using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.BumpFormSaved;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using FastEndpoints;
using MediatR;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Commands.BumpFormSaved;

public sealed class BumpFormSavedEndpoint(ISender sender) : Endpoint<BumpFormSavedRequest>
{
    private readonly ISender _sender = sender ?? throw new ArgumentNullException(nameof(sender));

    public override void Configure()
    {
        Post("dialogs/{dialogId}/actions/bumpformsaved");
        Policies(AuthorizationPolicy.Admin);
        Group<ServiceOwnerGroup>();
        Description(x => x.ExcludeFromDescription());
    }


    public override async Task HandleAsync(BumpFormSavedRequest req, CancellationToken ct)
    {

        var command = new BumpFormSavedCommand
        {
            DialogId = req.DialogId,
            FormSavedAt = req.FormSavedAt,
            IfMatchDialogRevision = req.IfMatchDialogRevision
        };

        var result = await _sender.Send(command, ct);
        await result.Match(
            success =>
            {
                HttpContext.Response.Headers.Append(Constants.ETag, success.Revision.ToString());
                return SendNoContentAsync(ct);
            },
            forbidden => this.ForbiddenAsync(forbidden, ct),
            domainError => this.UnprocessableEntityAsync(domainError, ct),
            notFound => this.NotFoundAsync(notFound, ct),
            concurrencyError => this.PreconditionFailed(ct)
        );
    }
}

public sealed class BumpFormSavedRequest
{
    public Guid DialogId { get; set; }

    [FromHeader(headerName: Constants.IfMatch, isRequired: false, removeFromSchema: true)]
    public Guid? IfMatchDialogRevision { get; set; }

    [FromBody]
    public DateTimeOffset? FormSavedAt { get; set; }
}
