using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.Metadata.ServiceResources.Queries.Get;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using FastEndpoints;
using MediatR;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Metadata.ServiceResources.Get;

[OpenApiOperationId("GetServiceResourceMetadata")]
public sealed class GetServiceResourceMetadataEndpoint
    : Endpoint<GetServiceResourceMetadataRequest, GetServiceResourceMetadataDto>
{
    private readonly ISender _sender;

    public GetServiceResourceMetadataEndpoint(ISender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);

        _sender = sender;
    }

    public override void Configure()
    {
        Get("metadata/serviceresources");
        Group<MetadataGroup>();

        Description(b => b.ProducesOneOf<GetServiceResourceMetadataDto>(StatusCodes.Status200OK));
    }

    public override async Task HandleAsync(GetServiceResourceMetadataRequest req, CancellationToken ct)
    {
        var result = await _sender.Send(new GetServiceResourceMetadataQuery
        {
            AcceptedLanguages = req.AcceptedLanguages?.AcceptedLanguage
        }, ct);

        await Send.OkAsync(result, ct);
    }
}

[OpenApiTypeName(nameof(GetServiceResourceMetadataRequest))]
public sealed class GetServiceResourceMetadataRequest
{
    [FromHeader(Constants.AcceptLanguage, isRequired: false)]
    public AcceptedLanguages? AcceptedLanguages { get; set; } = null;
}
