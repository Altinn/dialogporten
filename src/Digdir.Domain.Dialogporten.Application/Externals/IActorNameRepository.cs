using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Events.ResyncActorName;
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
    /// <see cref="EnsureActorNamesTransactional"/>
    /// </summary>
    /// <returns></returns>
    Task<ActorName> EnsureActorNameTransactional(ActorName actorName, CancellationToken cancellationToken);

    /// <summary>
    /// Writes actor names to the database.
    /// - The insert of <see cref="ActorName"/> is thread safe and avoids conflicts by resolving them in sql.
    /// - This method may add a <see cref="ResyncActorName"/> event to the <see cref="ActorName"/> entity.
    ///   The caller must save these events by using EF SaveChangesAsync to persist the events.
    /// - This method must be run within a transaction, otherwise it throws <see cref="InvalidOperationException"/>.
    ///
    /// The transaction requirement ensures two things:
    /// 1. The event is always published together with the <see cref="ActorName"/> itself.
    /// 2. The event is always published together with any <see cref="Actor.ActorNameEntityId"/> changes.
    ///
    /// </summary>
    /// <returns></returns>
    Task<List<ActorName>> EnsureActorNames(IReadOnlyCollection<ActorName> actorNames,
        CancellationToken cancellationToken);

    /// <summary>
    /// Writes actor names to the database.
    /// - The insert of <see cref="ActorName"/> is thread safe and avoids conflicts by resolving them in sql.
    /// - The insert of <see cref="ResyncActorName"/> is *not* thread safe with respect to <see cref="Actor"/>.
    ///   Be careful not to introduce a race condition in <see cref="ResyncActorName"/>
    ///   if you use this method and change the foreign key <see cref="Actor.ActorNameEntityId"/>.
    ///
    /// </summary>
    /// <returns></returns>
    Task<List<ActorName>> EnsureActorNamesTransactional(IReadOnlyCollection<ActorName> actorNames,
        CancellationToken cancellationToken);
}
