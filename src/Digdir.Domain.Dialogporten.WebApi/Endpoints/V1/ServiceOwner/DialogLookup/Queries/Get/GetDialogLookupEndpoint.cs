using Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using FastEndpoints;
using MediatR;
using GetDialogLookupQuery = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.DialogLookup.Queries.Get.GetDialogLookupQuery;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.DialogLookup.Queries.Get;

public sealed class GetDialogLookupEndpoint : Endpoint<GetDialogLookupRequest, ServiceOwnerIdentifierLookupDto>
{
    private readonly ISender _sender;

    public GetDialogLookupEndpoint(ISender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);

        _sender = sender;
    }

    public override void Configure()
    {
        Get("dialoglookup");
        Policies(AuthorizationPolicy.ServiceProvider);
        Group<ServiceOwnerGroup>();

        Description(b => b.ProducesOneOf<ServiceOwnerIdentifierLookupDto>(
            StatusCodes.Status200OK,
            StatusCodes.Status400BadRequest,
            StatusCodes.Status403Forbidden,
            StatusCodes.Status404NotFound));
    }

    public override async Task HandleAsync(GetDialogLookupRequest req, CancellationToken ct)
    {
        var query = new GetDialogLookupQuery
        {
            InstanceRef = req.InstanceRef,
            AcceptedLanguages = req.AcceptedLanguages?.AcceptedLanguage
        };

        var result = await _sender.Send(query, ct);

        await result.Match(
            dto => SendOkAsync(dto, ct),
            notFound => this.NotFoundAsync(notFound, ct),
            forbidden => this.ForbiddenAsync(forbidden, ct),
            validationError => this.BadRequestAsync(validationError, ct));
    }
}

public sealed class GetDialogLookupRequest
{
    [QueryParam]
    public string InstanceRef { get; set; } = null!;

    [FromHeader(Constants.AcceptLanguage, isRequired: false)]
    public AcceptedLanguages? AcceptedLanguages { get; set; } = null;
}
