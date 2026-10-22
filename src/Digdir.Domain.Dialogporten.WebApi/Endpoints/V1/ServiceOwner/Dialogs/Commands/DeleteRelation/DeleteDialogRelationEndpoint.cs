using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.DeleteRelation;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using FastEndpoints;
using MediatR;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Commands.DeleteRelation;

[OpenApiOperationId("DeleteDialogRelation")]
[OpenApiExtras(
    scopes: [AuthorizationScope.ServiceProvider],
    securitySchemes: [OpenApiSecurityScheme.MaskinportenSecurityScheme])
]
public sealed class DeleteDialogRelationEndpoint : Endpoint<DeleteDialogRelationRequest>
{
    private readonly ISender _sender;

    public DeleteDialogRelationEndpoint(ISender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);

        _sender = sender;
    }

    public override void Configure()
    {
        Delete("dialogs/{dialogId}/relations/{relatedDialogId}");
        Policies(AuthorizationPolicy.ServiceProvider);
        Group<ServiceOwnerGroup>();

        Description(b => b
            .Produces(StatusCodes.Status204NoContent)
            .ProducesDpProblemFor(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status401Unauthorized,
                StatusCodes.Status403Forbidden,
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict,
                StatusCodes.Status412PreconditionFailed,
                StatusCodes.Status422UnprocessableEntity
            ));
    }

    public override async Task HandleAsync(DeleteDialogRelationRequest req, CancellationToken ct)
    {
        var command = new DeleteDialogRelationCommand
        {
            DialogId = req.DialogId,
            RelatedDialogId = req.RelatedDialogId,
            IfMatchDialogRevision = req.IfMatchDialogRevision
        };

        var result = await _sender.Send(command, ct);
        await result.Match(
            success =>
            {
                HttpContext.Response.Headers.Append(Constants.ETag, success.Revision.ToString());
                return Send.NoContentAsync(ct);
            },
            notFound => this.NotFoundAsync(notFound, ct),
            validationError => this.BadRequestAsync(validationError, ct),
            forbidden => this.ForbiddenAsync(forbidden, ct),
            domainError => this.UnprocessableEntityAsync(domainError, ct),
            concurrencyError => this.PreconditionFailed(ct),
            conflict => this.ConflictAsync(conflict, ct));
    }
}

[OpenApiTypeName(nameof(DeleteDialogRelationRequest))]
public sealed class DeleteDialogRelationRequest
{
    public Guid DialogId { get; set; }

    public Guid RelatedDialogId { get; set; }

    [FromHeader(headerName: Constants.IfMatch, isRequired: false, removeFromSchema: true)]
    public Guid? IfMatchDialogRevision { get; set; }
}
