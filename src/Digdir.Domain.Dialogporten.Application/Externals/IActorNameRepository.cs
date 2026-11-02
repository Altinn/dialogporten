using Digdir.Domain.Dialogporten.Domain.Actors;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Application.Externals;

public interface IActorNameRepository
{
    Task<Guid> GetOrCreateActorName(
        string actorId,
        string? actorName,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Gets or creates the given <see cref="ActorName"/> rows in a single round trip, without involving the EF
    /// change tracker. Runs outside any EF transaction.
    /// </summary>
    /// <param name="actorNames">The actor names to get or create. Enumerated multiple times, so it must be materialized.</param>
    /// <param name="cancellationToken">Token used to cancel the database operation.</param>
    /// <returns>
    /// A map from the input <see cref="ActorName.Id"/> to the id of the persisted row. The ids are equal when the
    /// input row was inserted, and differ when an equivalent row already existed.
    /// </returns>
    Task<IReadOnlyDictionary<Guid, Guid>> GetOrCreateActorNames(
        IReadOnlyCollection<ActorName> actorNames,
        CancellationToken cancellationToken
    );
}
