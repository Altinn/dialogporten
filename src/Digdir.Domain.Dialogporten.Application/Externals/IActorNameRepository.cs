using Digdir.Domain.Dialogporten.Domain.Actors;

namespace Digdir.Domain.Dialogporten.Application.Externals;

public interface IActorNameRepository
{

    /// <summary>
    /// <see cref="EnsureActorNames"/>
    /// </summary>
    /// <returns></returns>
    Task<ActorName> EnsureActorName(ActorName actorName, CancellationToken cancellationToken);

    /// <summary>
    /// Writes actor names to the database. The insert is thread safe and avoids conflicts by resolving them in sql.
    /// This method must be run within a transaction otherwise it throws.
    /// The caller must run a SaveChangesAsync after this call, if it wants to persist the ActorName resync events.
    /// The required transaction ensures two things:
    /// 1. The event is always published together with the ActorName itself.
    /// 2. The event is always published together with any Actor changes.
    ///
    /// </summary>
    /// <param name="actorNames"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<List<ActorName>> EnsureActorNames(IReadOnlyCollection<ActorName> actorNames,
        CancellationToken cancellationToken);
}
