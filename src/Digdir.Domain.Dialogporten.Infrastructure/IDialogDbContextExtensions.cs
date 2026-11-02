using Digdir.Domain.Dialogporten.Application.Externals;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Infrastructure;

public static class IDialogDbContextExtensions
{
    /// <summary>
    /// Reconciles locally tracked <c>ActorName</c> and <c>Actor</c> entities with the ids that actually exist
    /// in the database, typically after resolving conflicts through
    /// <see cref="IActorNameRepository.GetOrCreateActorNames"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only entities already tracked in <c>db.ActorName.Local</c> and <c>db.Actor.Local</c> are considered; the
    /// database is not queried. Entries whose id is not a key in the map, or whose input and actual ids are
    /// equal, are left untouched.
    /// </para>
    /// <para>
    /// For each remapped <c>ActorName</c>, its id is replaced with the actual id and it is marked as
    /// <see cref="EntityState.Unchanged"/>, so <c>SaveChanges</c> does not try to insert a row that already
    /// exists. Tracked <c>Actor</c> entities that referenced the input id get their
    /// <c>ActorNameEntityId</c> set to the actual id, which EF change detection picks up as a modification.
    /// </para>
    /// <para>
    /// Does not call <c>SaveChanges</c>.
    /// </para>
    /// </remarks>
    /// <param name="db">The database context whose change tracker is updated.</param>
    /// <param name="actualActorNameIdByInput">
    /// A map from the id an <c>ActorName</c> was created with to the id of the persisted row.
    /// </param>
    public static void RemapActorNameIds(
        this IDialogDbContext db,
        IReadOnlyDictionary<Guid, Guid> actualActorNameIdByInput)
    {
        var actorsByActorNameId = db.Actor.Local
            .Where(x => x.ActorNameEntityId.HasValue)
            .GroupBy(x => x.ActorNameEntityId!.Value, db.Actor.Entry)
            .ToDictionary(x => x.Key, x => x.ToList());

        var actorNamesByActorNameId = db.ActorName.Local
            .Select(db.ActorName.Entry)
            .ToDictionary(x => x.Entity.Id);

        foreach (var (input, actual) in actualActorNameIdByInput)
        {
            if (!actorNamesByActorNameId.TryGetValue(input, out var actorName) || actorName.Entity.Id == actual)
            {
                continue;
            }

            actorName.Entity.Id = actual;
            actorName.State = EntityState.Unchanged;
            if (!actorsByActorNameId.TryGetValue(input, out var actors))
            {
                continue;
            }

            foreach (var actorEntry in actors)
            {
                actorEntry.Entity.ActorNameEntityId = actual;
            }
        }
    }
}
