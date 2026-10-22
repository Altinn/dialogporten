using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateRelation;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Relations;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using FastEndpoints;
using MediatR;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Commands.CreateRelation;

[OpenApiOperationId("CreateDialogRelation")]
[OpenApiExtras(
    scopes: [AuthorizationScope.ServiceProvider],
    securitySchemes: [OpenApiSecurityScheme.MaskinportenSecurityScheme])
]
public sealed class CreateDialogRelationEndpoint : Endpoint<CreateDialogRelationRequest>
{
    private readonly ISender _sender;

    public CreateDialogRelationEndpoint(ISender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);

        _sender = sender;
    }

    public override void Configure()
    {
        Post("dialogs/{dialogId}/relations");
        Policies(AuthorizationPolicy.ServiceProvider);
        Group<ServiceOwnerGroup>();

        Description(b => b
            .Accepts<CreateDialogRelationRequest>()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesDpProblemFor(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status401Unauthorized,
                StatusCodes.Status403Forbidden,
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict,
                StatusCodes.Status410Gone,
                StatusCodes.Status412PreconditionFailed,
                StatusCodes.Status422UnprocessableEntity
            ));
    }

    public override async Task HandleAsync(CreateDialogRelationRequest req, CancellationToken ct)
    {
        var command = new CreateDialogRelationCommand
        {
            DialogId = req.DialogId,
            RelatedDialogId = req.RelatedDialogId,
            Type = req.Type,
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
            gone => this.GoneAsync(gone, ct),
            validationError => this.BadRequestAsync(validationError, ct),
            forbidden => this.ForbiddenAsync(forbidden, ct),
            domainError => this.UnprocessableEntityAsync(domainError, ct),
            concurrencyError => this.PreconditionFailed(ct),
            conflict => this.ConflictAsync(conflict, ct));
    }
}

[OpenApiTypeName(nameof(CreateDialogRelationRequest))]
public sealed class CreateDialogRelationRequest
{
    public Guid DialogId { get; set; }

    [FromHeader(headerName: Constants.IfMatch, isRequired: false, removeFromSchema: true)]
    public Guid? IfMatchDialogRevision { get; set; }

    /// <summary>
    /// The dialog to relate to. It must exist and not be deleted, but may belong to another party or service owner.
    /// </summary>
    /// <example>01913cd5-784f-7d3b-abef-4c77b1f0972d</example>
    public Guid RelatedDialogId { get; set; }

    /// <summary>
    /// The type of the relation, as seen from the dialog in the path. "Parent" means that the related dialog becomes
    /// the parent of this dialog.
    /// </summary>
    public DialogRelationType.Values Type { get; set; }
}
