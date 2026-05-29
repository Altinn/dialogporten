using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.Metadata.ServiceResources.Queries.Get;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.PreProcessors;
using FastEndpoints;
using MediatR;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Metadata.ServiceResources.Get;

[OpenApiOperationId("GetServiceResourceMetadata")]
public sealed class GetServiceResourceMetadataEndpoint
    : EndpointWithoutRequest<GetServiceResourceMetadataDto>
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
        PreProcessor<RequireJsonAcceptPreProcessor>();
        Group<MetadataGroup>();

        Description(b => b.ProducesOneOf<GetServiceResourceMetadataDto>(StatusCodes.Status200OK));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var acceptedLanguages = AcceptedLanguages.TryParse(
            HttpContext.Request.Headers[Constants.AcceptLanguage],
            out var parsedAcceptedLanguages)
            ? parsedAcceptedLanguages
            : new AcceptedLanguages([]);

        var result = await _sender.Send(new GetServiceResourceMetadataQuery
        {
            AcceptedLanguages = acceptedLanguages.AcceptedLanguage
        }, ct);

        await Send.OkAsync(result, ct);
    }
}
