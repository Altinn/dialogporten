using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using MediatR;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.AccessManagement.Queries.GetParties;

public sealed class GetPartiesQuery : IRequest<PartiesDto>, IDoNotCareAboutServiceResource;

internal sealed class GetPartiesQueryHandler : IRequestHandler<GetPartiesQuery, PartiesDto>
{
    private readonly IUserParties _userParties;
    private readonly IMapper _mapper;

    public GetPartiesQueryHandler(IUserParties userParties, IMapper mapper)
    {
        _userParties = userParties;
        _mapper = mapper;
    }

    public async Task<PartiesDto> Handle(GetPartiesQuery request, CancellationToken cancellationToken)
    {
        var authorizedPartiesResult = await _userParties.GetUserParties(cancellationToken);
        return _mapper.Map<PartiesDto>(authorizedPartiesResult);
    }
}
