using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Application.Features.V1.AccessManagement.Queries.GetParties;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.Parties;
using MediatR;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser;

public partial class Queries
{
    public async Task<List<AuthorizedParty>> GetParties(
        [Service] ISender mediator,
        [Service] IMapper mapper,
        [Service] ILogger<Queries> logger,
        [Service] IUser user,
        CancellationToken cancellationToken)
    {
        var request = new GetPartiesQuery();
        var result = await mediator.Send(request, cancellationToken);

        user.GetPrincipal().TryGetPid(out var pid);
        LogGraphqlPartyResult(logger, pid, result);

        return mapper.Map<List<AuthorizedParty>>(result.AuthorizedParties);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "GraphQL handler, app result for party {Party}: {@Result}")]
    private static partial void LogGraphqlPartyResult(ILogger logger, string? party, PartiesDto result);
}
