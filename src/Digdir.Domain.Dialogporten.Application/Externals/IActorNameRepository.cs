using Digdir.Domain.Dialogporten.Domain.Actors;

namespace Digdir.Domain.Dialogporten.Application.Externals;

public interface IActorNameRepository
{
    /// <inheritdoc cref="EnsureActorNames"/>
    Task<ActorName> EnsureActorName(ActorName actorName, CancellationToken cancellationToken);

    /// <summary>
    /// Ensures that the given actor names exist in the database, and updates the given instances to
    /// reference the persisted rows.
    /// </summary>
    /// <remarks>
    /// When called outside a transaction, newly created actor names that need a resync are committed
    /// immediately, together with their resync events, in a separate short transaction. Call this
    /// before opening a long-running or higher isolation transaction to keep locks on actor names short.
    /// When called inside a transaction, the rows are written in that transaction and resync events are
    /// raised on the entities, to be committed by the caller's next SaveChanges.
    /// </remarks>
    Task<List<ActorName>> EnsureActorNames(IReadOnlyCollection<ActorName> actorNames,
        CancellationToken cancellationToken);
}
