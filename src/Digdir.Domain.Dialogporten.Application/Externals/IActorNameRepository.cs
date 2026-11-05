using Digdir.Domain.Dialogporten.Domain.Actors;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Application.Externals;

public interface IActorNameRepository
{
    Task<ActorName> EnsureActorName(DbContext db, ActorName actorName, CancellationToken cancellationToken);

    Task<List<ActorName>> EnsureActorNames(DbContext db, IReadOnlyCollection<ActorName> actorNames,
        CancellationToken cancellationToken);
}
