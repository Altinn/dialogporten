using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using MediatR;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.AccessManagement.Queries.GetParties;

public sealed class GetPartiesQuery : IRequest<PartiesDto>, IFeatureMetricServiceResourceIgnoreRequest;

internal sealed class GetPartiesQueryHandler : IRequestHandler<GetPartiesQuery, PartiesDto>
{
    private readonly IUserParties _userParties;

    public GetPartiesQueryHandler(IUserParties userParties)
    {
        _userParties = userParties;
    }

    public async Task<PartiesDto> Handle(GetPartiesQuery request, CancellationToken cancellationToken)
    {
        var authorizedPartiesResult = await _userParties.GetUserParties(cancellationToken);
        return authorizedPartiesResult.ToDto();
    }
}
