using Digdir.Domain.Dialogporten.Application.Features.V1.AccessManagement.Queries.GetParties;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.PreProcessors;
using FastEndpoints;
using MediatR;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser.AccessManagement.Queries.GetParties;

[OpenApiOperationId("GetParties")]
public sealed class GetPartiesEndpoint : EndpointWithoutRequest<PartiesDto>
{
    private readonly ISender _sender;

    public GetPartiesEndpoint(ISender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);

        _sender = sender;
    }

    public override void Configure()
    {
        Get("parties");
        PreProcessor<RequireJsonAcceptPreProcessor>();
        Policies(AuthorizationPolicy.EndUser);
        Group<EndUserGroup>();

        Description(d => d.Produces<PartiesDto>());
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _sender.Send(new GetPartiesQuery(), ct);
        await SendOkAsync(result, ct);
    }
}
