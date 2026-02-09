using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateActivity;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.GetActivity;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Queries.GetActivity;
using FastEndpoints;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Commands.CreateActivity;

public sealed class CreateDialogActivityEndpoint : Endpoint<CreateActivityRequest>
{
    private readonly ISender _sender;

    public CreateDialogActivityEndpoint(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public override void Configure()
    {
        Post("dialogs/{dialogId}/activities");
        Policies(AuthorizationPolicy.ServiceProvider);
        Group<ServiceOwnerGroup>();
        Description(b => b
            .Produces<string>(StatusCodes.Status201Created)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ValidationProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ValidationProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ValidationProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ValidationProblemDetails>(StatusCodes.Status410Gone)
            .Produces<ProblemDetails>(StatusCodes.Status412PreconditionFailed)
            .Produces<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)
        );
    }

    public override async Task HandleAsync(CreateActivityRequest req, CancellationToken ct)
    {
        var result = await _sender.Send(new CreateActivityCommand
        {
            DialogId = req.DialogId,
            IfMatchDialogRevision = req.IfMatchDialogRevision,
            IsSilentUpdate = req.IsSilentUpdate ?? false,
            Activities = [req],
        }, ct);

        await result.Match(
            success =>
            {
                HttpContext.Response.Headers.Append(Constants.ETag, success.Revision.ToString());
                var activityId = success.ActivityIds.First();
                return SendCreatedAtAsync<GetDialogActivityEndpoint>(
                    new GetActivityQuery { DialogId = req.DialogId, ActivityId = activityId },
                    activityId,
                    cancellation: ct
                );
            },
            notFound => this.NotFoundAsync(notFound, ct),
            gone => this.GoneAsync(gone, ct),
            validationError => this.BadRequestAsync(validationError, ct),
            forbidden => this.ForbiddenAsync(forbidden, ct),
            domainError => this.UnprocessableEntityAsync(domainError, ct),
            concurrencyError => this.PreconditionFailed(ct),
            conflict => this.ConflictAsync(conflict, ct)
        );
    }
}

public sealed class CreateActivityRequest : CreateActivityDto
{
    public Guid DialogId { get; set; }

    [FastEndpoints.FromHeader(headerName: Constants.IfMatch, isRequired: false, removeFromSchema: true)]
    public Guid? IfMatchDialogRevision { get; set; }

    [HideFromDocs]
    public bool? IsSilentUpdate { get; init; }
}
