using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.UpdateFormSavedActivityTime;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using FastEndpoints;
using MediatR;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Commands.UpdateFormSavedActivityTime;

public sealed class UpdateFormSavedActivityTimeEndpoint(ISender sender) : Endpoint<UpdateFormSavedActivityTimeRequest>
{
    private readonly ISender _sender = sender ?? throw new ArgumentNullException(nameof(sender));

    public override void Configure()
    {
        Post("dialogs/{dialogId:guid}/activities/{activityId:guid}/actions/updateFormSavedActivityTime");
        Policies(AuthorizationPolicy.ServiceProviderAdmin);
        Group<ServiceOwnerGroup>();
        Description(x => x.ExcludeFromDescription());
    }


    public override async Task HandleAsync(UpdateFormSavedActivityTimeRequest req, CancellationToken ct)
    {
        var command = new UpdateFormSavedActivityTimeCommand
        {
            DialogId = req.DialogId,
            ActivityId = req.ActivityId,
            NewCreatedAt = req.NewCreatedAt,
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
            concurrencyError => this.PreconditionFailed(ct),
            conflict => this.ConflictAsync(conflict, ct)
        );
    }
}

public sealed class UpdateFormSavedActivityTimeRequest
{
    public Guid DialogId { get; set; }
    public Guid ActivityId { get; set; }
    [FromHeader(headerName: Constants.IfMatch, isRequired: false, removeFromSchema: true)]
    public Guid? IfMatchDialogRevision { get; set; }
    [FromBody]
    public DateTimeOffset NewCreatedAt { get; set; }
}
