using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common;
using Mediator;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Parties.Queries.Get;

public sealed class GetPartiesQuery : IRequest<PartiesDto>;

internal sealed class GetPartiesQueryHandler : IRequestHandler<GetPartiesQuery, PartiesDto>
{
    private readonly IUserParties _userParties;
    private readonly IMapper _mapper;

    public GetPartiesQueryHandler(IUserParties userParties, IMapper mapper)
    {
        _userParties = userParties;
        _mapper = mapper;
    }

    public async ValueTask<PartiesDto> Handle(GetPartiesQuery request, CancellationToken cancellationToken)
    {
        var authorizedPartiesResult = await _userParties.GetUserParties(cancellationToken);
        return _mapper.Map<PartiesDto>(authorizedPartiesResult);
    }
}
