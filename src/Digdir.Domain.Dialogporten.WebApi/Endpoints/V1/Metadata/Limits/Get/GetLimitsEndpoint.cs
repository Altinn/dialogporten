using Digdir.Domain.Dialogporten.Application.Features.V1.Metadata.Limits.Queries.Get;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.PreProcessors;
using FastEndpoints;
using MediatR;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Metadata.Limits.Get;

[OpenApiOperationId("GetLimits")]
public sealed class GetLimitsEndpoint : EndpointWithoutRequest<GetLimitsDto>
{
    private readonly ISender _sender;

    public GetLimitsEndpoint(ISender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);

        _sender = sender;
    }

    public override void Configure()
    {
        Get("metadata/limits");
        PreProcessor<RequireJsonAcceptPreProcessor>();
        Group<MetadataGroup>();

        Description(b => b.ProducesOneOf<GetLimitsDto>(StatusCodes.Status200OK));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _sender.Send(new GetLimitsQuery(), ct);

        await SendOkAsync(result, ct);
    }
}
