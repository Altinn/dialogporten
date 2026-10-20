using System.Diagnostics;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using static System.StringComparison;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Interceptors;

internal sealed class PopulateActorNameInterceptor : SaveChangesInterceptor
{
    private readonly IDomainContext _domainContext;
    private readonly IPartyNameRegistry _partyNameRegistry;
    private readonly IUserRegistry _userRegistry;

    public PopulateActorNameInterceptor(
        IDomainContext domainContext,
        IPartyNameRegistry partyNameRegistry,
        IUserRegistry userRegistry
    )
    {
        ArgumentNullException.ThrowIfNull(domainContext);
        ArgumentNullException.ThrowIfNull(partyNameRegistry);

        _domainContext = domainContext;
        _partyNameRegistry = partyNameRegistry;
        _userRegistry = userRegistry;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var dbContext = eventData.Context;
        if (dbContext is null)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var addedActorNameEntities = dbContext.ChangeTracker
            .Entries<ActorName>()
            .Where(e => e.State is EntityState.Added)
            .Where(e => e.Entity.ActorId is not null || e.Entity.Name is not null)
            .Select(e =>
            {
                var entity = e.Entity;
                entity.ActorId = entity.ActorId?.ToLowerInvariant();
                return entity;
            })
            .ToList();

        if (addedActorNameEntities.Count == 0)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        if (!await TrySetMissingActorNames(addedActorNameEntities, cancellationToken))
        {
            return InterceptionResult<int>.SuppressWithResult(0);
        }

        // We order the entities to prevent deadlocks when concurrently inserting the same ActorName
        var distinctOrderedActorNameEntities = addedActorNameEntities
            .OrderBy(x => x.ActorId, StringComparer.Ordinal)
            .ThenBy(x => x.Name, StringComparer.Ordinal)
            .DistinctBy(x => (x.ActorId, x.Name))
            .ToList();

        await InsertActorNamesIgnoreConflicts(dbContext, distinctOrderedActorNameEntities, cancellationToken);

        var newActorNameRows = await GetActorNameRows(dbContext, distinctOrderedActorNameEntities, cancellationToken);
        var newActorNameRowsByActorIdAndName = newActorNameRows.ToDictionary(x => (x.ActorId, x.Name), x => x);

        ConsolidateEfActorNameEntityIds(addedActorNameEntities, newActorNameRowsByActorIdAndName, dbContext);
        PublishResyncEventsForMissingActorNames(distinctOrderedActorNameEntities, newActorNameRowsByActorIdAndName);

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task<bool> TrySetMissingActorNames(
        IEnumerable<ActorName> actorNameEntities,
        CancellationToken cancellationToken)
    {
        var relevantActorNameEntities = actorNameEntities
            .Where(x => x.ActorId is not null)
            .ToList();

        var actorNameById = (await Task.WhenAll(relevantActorNameEntities
                .Select(x => x.ActorId!)
                .Distinct()
                .Select(x => ActorNameByActorId(x, cancellationToken))))
            .ToDictionary(x => x.ActorId, x => x.Result);

        foreach (var actorName in relevantActorNameEntities)
        {
            var newActorName = actorNameById[actorName.ActorId!];

            newActorName.Switch(
                found => actorName.Name = found.Name,
                notFound =>
                {
                    if (SystemUserIsGuaranteedToExist(notFound.ExternalIdWithPrefix))
                    {
                        actorName.Name = null;
                        return;
                    }

                    _domainContext.AddError(
                        nameof(Actor.ActorNameEntity.ActorId),
                        $"Unable to look up name for actor id: {actorName.ActorId}"
                    );
                },
                unsupported => _domainContext.AddError(
                    nameof(Actor.ActorNameEntity.ActorId),
                    $"Unable to look up name for actor id: {actorName.ActorId}"
                ),
                unavailable =>
                {
                    if (UserIsGuaranteedToExist(_userRegistry.GetCurrentUserId(), unavailable.ExternalIdWithPrefix))
                    {
                        actorName.Name = null;
                        return;
                    }

                    _domainContext.AddError(
                        nameof(Actor.ActorNameEntity.ActorId),
                        $"Unable to look up name for actor id: {actorName.ActorId}"
                    );
                }
            );
        }

        return _domainContext.IsValid;
    }

    private static void ConsolidateEfActorNameEntityIds(
        List<ActorName> addedActorNameEntities,
        Dictionary<(string? ActorId, string? Name), ActorNameRow> newActorNameRowsByActorIdAndName,
        DbContext dbContext
    )
    {
        foreach (var entity in addedActorNameEntities)
        {
            dbContext.Entry(entity).State = EntityState.Unchanged;
            var newActorNameRow = newActorNameRowsByActorIdAndName.GetValueOrDefault((entity.ActorId, entity.Name))
                                  ?? throw new UnreachableException("We should find all rows we just inserted");

            foreach (var actorEntity in entity.ActorEntities)
            {
                actorEntity.ActorNameEntityId = newActorNameRow.Id;
            }
        }
    }

    /// <summary>
    /// Fast test for whether we can trust that the user behind the ID exists.
    /// We cant allow null names otherwise.
    /// </summary>
    private static bool UserIsGuaranteedToExist(UserId currentUserId, string externalIdWithPrefixInQuestion)
    {
        if (currentUserId.Type is DialogUserType.Values.ServiceOwnerOnBehalfOfPerson) return false;

        return string.Equals(currentUserId.ExternalIdWithPrefix, externalIdWithPrefixInQuestion, OrdinalIgnoreCase);
    }

    /// <summary>
    /// We see up to 1 minute delay before newly created system users are visible in PartyNameRegistry
    /// We can allow null names just in this case.
    /// </summary>
    /// <param name="externalIdWithPrefixInQuestion"></param>
    /// <returns></returns>
    private bool SystemUserIsGuaranteedToExist(string externalIdWithPrefixInQuestion)
    {
        var currentUserId = _userRegistry.GetCurrentUserId();

        return currentUserId.Type is DialogUserType.Values.SystemUser
               && UserIsGuaranteedToExist(currentUserId, externalIdWithPrefixInQuestion);
    }

    private static void PublishResyncEventsForMissingActorNames(
        List<ActorName> actorNameEntities,
        Dictionary<(string? ActorId, string? Name), ActorNameRow> newActorNameRowsByActorIdAndName
    )
    {
        foreach (var entity in actorNameEntities)
        {
            var newActorNameRow = newActorNameRowsByActorIdAndName.GetValueOrDefault((entity.ActorId, entity.Name))
                                  ?? throw new UnreachableException("We should find all rows we just inserted");

            if (newActorNameRow.ActorId is not null && newActorNameRow.Name is null)
            {
                entity.AddResyncActorNameEvent(
                    reason: $"{nameof(PopulateActorNameInterceptor)}: Unable to look up actor name",
                    overrideId: newActorNameRow.Id
                );
            }
        }
    }

    /// <summary>
    /// Inserts ActorNames and ignores all conflicts on ActorId and Name.
    /// This method blocks on concurrent writes of the same ActorId and Name combination.

    /// Input must be ordered by (ActorId, Name) to prevent deadlocks.
    /// An inserted but uncommitted (ActorId, Name) row makes other transactions inserting the same value wait until
    /// this transaction ends. Rows are inserted, sorted by (ActorId, Name), so two transactions never wait on each
    /// other in opposite order (deadlock). This only holds if a transaction inserts all its ActorNames in a single call
    /// (or in calls whose keys are strictly increasing)
    ///
    /// About Isolation Levels:
    /// - Read Committed: Conflicts are ignored, no exception is thrown
    /// - Repeatable Read and stricter: Conflicts are ignored, but an exception is thrown
    /// </summary>
    private static async Task InsertActorNamesIgnoreConflicts(
        DbContext dbContext,
        List<ActorName> added,
        CancellationToken cancellationToken
    )
    {
        if (added.Count == 0) return;
        var sql = new PostgresFormattableStringBuilder();

        sql.Append("""INSERT INTO "ActorName" ("Id", "ActorId", "Name", "CreatedAt") VALUES """);
        for (var i = 0; i < added.Count; i++)
        {
            var entity = added[i];
            sql.Append($"({entity.Id}, {entity.ActorId}, {entity.Name}, {entity.CreatedAt})");
            sql.AppendIf(i != added.Count - 1, ",");
        }

        sql.Append(""" ON CONFLICT ("ActorId", "Name") DO NOTHING;""");
        await dbContext.Database.ExecuteSqlAsync(sql.ToFormattableString(), cancellationToken);
    }

    private static async Task<List<ActorNameRow>> GetActorNameRows(
        DbContext dbContext,
        List<ActorName> added,
        CancellationToken cancellationToken
    )
    {
        if (added.Count == 0) return [];
        var query = new PostgresFormattableStringBuilder()
            .Append($"""
                      SELECT "Id",
                             "ActorId",
                             "Name"
                      FROM "ActorName"
                      WHERE
                     """);
        for (var i = 0; i < added.Count; i++)
        {
            var e = added[i];
            query.Append("(");

            if (e.ActorId is null) query.Append(""" "ActorId" IS NULL""");
            else query.Append($""" "ActorId" = {e.ActorId}""");

            query.Append(" AND ");

            if (e.Name is null) query.Append(""" "Name" IS NULL""");
            else query.Append($""" "Name" = {e.Name}""");

            query.Append(")");
            query.AppendIf(i != added.Count - 1, " OR ");
        }

        return await dbContext.Database
            .SqlQuery<ActorNameRow>(query.ToFormattableString())
            .ToListAsync(cancellationToken);
    }

    private async Task<(string ActorId, GetPartyNameResult Result)> ActorNameByActorId(string actorId,
        CancellationToken cancellationToken) =>
        (actorId, await _partyNameRegistry.GetName(actorId, cancellationToken));

    private static async Task<List<ActorName>> GetExistingActorNames(DbContext dbContext,
        IEnumerable<ActorName> actorNameEntities, CancellationToken cancellationToken)
    {
        // Why are we doing "composite key contains" this way, you ask?
        // See https://stackoverflow.com/a/26201371/2301766
        var distinctIdNameTupples = actorNameEntities
            .Select(x => (x.ActorId, x.Name))
            .Distinct()
            .ToList();
        var hasNull = distinctIdNameTupples.Any(x => x.Name == null);
        var actorIds = distinctIdNameTupples.Select(x => x.ActorId).Distinct();
        var actorNames = distinctIdNameTupples.Select(x => x.Name).Distinct();
        var existingActorNames = await dbContext.Set<ActorName>()
            .Where(x => actorIds.Contains(x.ActorId) && (actorNames.Contains(x.Name) || (hasNull && x.Name == null)))
            .ToListAsync(cancellationToken);
        return existingActorNames
            .Where(x => distinctIdNameTupples.Any(xx => xx.ActorId == x.ActorId && xx.Name == x.Name))
            .ToList();
    }

    private sealed class ActorNameRow
    {
        public Guid Id { get; set; }
        public string? ActorId { get; set; }
        public string? Name { get; set; }
    }
}
