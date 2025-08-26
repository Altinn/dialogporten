using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.CostManagement;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Queries.Get;
using FastEndpoints;
using MediatR;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Commands.Create;

[CostTracked(TransactionType.CreateDialog)]
public sealed class CreateDialogEndpoint : Endpoint<CreateDialogRequest>
{
    private readonly ISender _sender;

    public CreateDialogEndpoint(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public override void Configure()
    {
        Post("dialogs");
        Policies(AuthorizationPolicy.ServiceProvider);
        Group<ServiceOwnerGroup>();

        Description(b => b.ProducesOneOf(
            StatusCodes.Status201Created,
            StatusCodes.Status400BadRequest,
            StatusCodes.Status422UnprocessableEntity,
            StatusCodes.Status409Conflict));
    }

    public override async Task HandleAsync(CreateDialogRequest req, CancellationToken ct)
    {
        var command = new CreateDialogCommand { Dto = req.Dto, IsSilentUpdate = req.IsSilentUpdate ?? false };
        var result = await _sender.Send(command, ct);
        await result.Match(
            success =>
            {
                HttpContext.Response.Headers.Append(Constants.ETag, success.Revision.ToString());
                return SendCreatedAtAsync<GetDialogEndpoint>(new GetDialogQuery { DialogId = success.DialogId },
                    success.DialogId, cancellation: ct);
            },
            domainError => this.UnprocessableEntityAsync(domainError, ct),
            validationError => this.BadRequestAsync(validationError, ct),
            forbidden => this.ForbiddenAsync(forbidden, ct),
            conflict => this.ConflictAsync(conflict, ct));
    }
}

public sealed class CreateDialogRequest
{
    [HideFromDocs]
    public bool? IsSilentUpdate { get; init; }

    [FromBody]
    public CreateDialogDto Dto { get; set; } = null!;
}
