using Digdir.Domain.Dialogporten.Application.Features.V1.Metadata.Limits.Queries.Get;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.Limits;
using MediatR;
using LimitsType = Digdir.Domain.Dialogporten.GraphQL.EndUser.Limits.Limits;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser;

public partial class Queries
{
    public async Task<LimitsType> GetLimits(
        [Service] ISender mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetLimitsQuery(), cancellationToken);
        return result.ToGraphQl();
    }
}
