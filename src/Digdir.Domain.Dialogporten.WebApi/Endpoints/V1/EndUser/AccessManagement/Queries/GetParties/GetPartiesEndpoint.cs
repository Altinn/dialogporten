using Digdir.Domain.Dialogporten.Application.Features.V1.AccessManagement.Queries.GetParties;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using FastEndpoints;
using MediatR;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser.AccessManagement.Queries.GetParties;

public sealed class GetPartiesEndpoint : EndpointWithoutRequest<PartiesDto>
{
    private readonly ISender _sender;

    public GetPartiesEndpoint(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public override void Configure()
    {
        Get("parties");
        Policies(AuthorizationPolicy.EndUser);
        Group<EndUserGroup>();

        Description(d => d.Produces<List<PartiesDto>>());
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _sender.Send(new GetPartiesQuery(), ct);
        await SendOkAsync(result, ct);
    }
}
