using Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using FastEndpoints;
using MediatR;
using GetDialogLookupQuery = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.DialogLookup.Queries.Get.GetDialogLookupQuery;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser.DialogLookup.Queries.Get;

public sealed class GetDialogLookupEndpoint : Endpoint<GetDialogLookupRequest, EndUserIdentifierLookupDto>
{
    private readonly ISender _sender;

    public GetDialogLookupEndpoint(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public override void Configure()
    {
        Get("dialoglookup");
        Policies(AuthorizationPolicy.EndUser);
        Group<EndUserGroup>();

        Description(b => b.ProducesOneOf<EndUserIdentifierLookupDto>(
            StatusCodes.Status200OK,
            StatusCodes.Status400BadRequest,
            StatusCodes.Status403Forbidden,
            StatusCodes.Status404NotFound));
    }

    public override async Task HandleAsync(GetDialogLookupRequest req, CancellationToken ct)
    {
        var query = new GetDialogLookupQuery
        {
            InstanceUrn = req.InstanceUrn,
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
    public string InstanceUrn { get; set; } = null!;

    [FromHeader(Constants.AcceptLanguage, isRequired: false)]
    public AcceptedLanguages? AcceptedLanguages { get; set; } = null;
}
