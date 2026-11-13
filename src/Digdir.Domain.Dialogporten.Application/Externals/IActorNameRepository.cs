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
    /// The given actor names are consolidated with the ones tracked by the current db context, and are left
    /// tracked as unchanged with their actual id, so a later save will not touch them.
    ///
    /// When the current db context has started a transaction, the insert joins that transaction, and the
    /// caller must run SaveChangesAsync afterward to persist the ActorName resync events. This ensures that:
    /// 1. The event is always published together with the ActorName itself.
    /// 2. The event is always published together with any Actor changes.
    ///
    /// When there is no current transaction, the insert and the resync events are committed atomically in
    /// an isolated transaction on a separate db context before this method returns. ActorNames created this
    /// way without being referenced by any actor are cleaned up by the resync handler.
    /// </summary>
    /// <param name="actorNames"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<List<ActorName>> EnsureActorNames(IReadOnlyCollection<ActorName> actorNames,
        CancellationToken cancellationToken);
}
