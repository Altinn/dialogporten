using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.Compliance.Queries.GetPartyGdprExtract;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using FastEndpoints;
using MediatR;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Compliance.Queries.GetComplianceDocument;

[OpenApiOperationId("GetPartyGdprExtract")]
[OpenApiExtras(
    scopes: [AuthorizationScope.Compliance],
    securitySchemes: [OpenApiSecurityScheme.MaskinportenSecurityScheme])
]
public sealed class GetPartyGdprExtractEndpoint : Endpoint<PartyGdprExtractRequest, PartyGdprExtractDto>
{
    private readonly ISender _sender;

    public GetPartyGdprExtractEndpoint(ISender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);

        _sender = sender;
    }

    public override void Configure()
    {
        Get("/parties/{PartyId}/gdpr-extract");
        Policies(AuthorizationPolicy.Compliance);
        Group<ComplianceGroup>();

        Description(b => b.Produces<PartyGdprExtractDto>());
    }

    public override async Task HandleAsync(PartyGdprExtractRequest req, CancellationToken ct)
    {
        var query = new PartyGdprExtractQuery { PartyId = req.PartyId };
        var result = await _sender.Send(query, ct);
        await result.Match(dto => Send.OkAsync(dto, ct)
        );
    }
}

[OpenApiTypeName(nameof(PartyGdprExtractRequest))]
public sealed class PartyGdprExtractRequest
{
    public required string PartyId { get; set; }
}
