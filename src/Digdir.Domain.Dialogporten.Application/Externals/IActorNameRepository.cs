using Digdir.Domain.Dialogporten.Domain.Actors;

namespace Digdir.Domain.Dialogporten.Application.Externals;

public interface IActorNameRepository
{
    Task<ActorName> EnsureActorName(ActorName actorName, CancellationToken cancellationToken);

    Task<List<ActorName>> EnsureActorNames(IReadOnlyCollection<ActorName> actorNames,
        CancellationToken cancellationToken);
}
