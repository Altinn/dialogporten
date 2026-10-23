using System.Diagnostics;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Library.Entity.Abstractions.Features.Creatable;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using static System.StringComparison;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Interceptors;

internal sealed class PopulateActorNameInterceptor : SaveChangesInterceptor
{
    private readonly IDomainContext _domainContext;
    private readonly IPartyNameRegistry _partyNameRegistry;
    private readonly ITransactionTime _transactionTime;
    private readonly IUserRegistry _userRegistry;
    private bool _hasBeenExecuted;

    public PopulateActorNameInterceptor(
        ITransactionTime transactionTime,
        IDomainContext domainContext,
        IPartyNameRegistry partyNameRegistry,
        IUserRegistry userRegistry
    )
    {
        ArgumentNullException.ThrowIfNull(transactionTime);
        ArgumentNullException.ThrowIfNull(domainContext);
        ArgumentNullException.ThrowIfNull(partyNameRegistry);

        _domainContext = domainContext;
        _partyNameRegistry = partyNameRegistry;
        _userRegistry = userRegistry;
        _transactionTime = transactionTime;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        // If the interceptor has already run during this transaction, we don't want to run it again.
        // This is to avoid doing the same work over multiple retries.
        if (_hasBeenExecuted)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

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
            _hasBeenExecuted = true;
            return InterceptionResult<int>.SuppressWithResult(0);
        }

        var finalActorNames = await ConsolidateActorNameInstances(dbContext, addedActorNameEntities, cancellationToken);
        MarkActorNamesForResync(finalActorNames);
        _hasBeenExecuted = true;
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void MarkActorNamesForResync(List<ActorName> actorNameEntities)
    {
        var actorNames = actorNameEntities
            .Where(x => x.ActorId is not null && x.Name is null)
            .DistinctBy(x => x.ActorId);

        foreach (var actorName in actorNames)
        {
            actorName.AddResyncActorNameEvent($"{nameof(PopulateActorNameInterceptor)}: Unable to look up actor name");
        }
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

    private async Task<List<ActorName>> ConsolidateActorNameInstances(
        DbContext dbContext,
        List<ActorName> added,
        CancellationToken cancellationToken
    )
    {
        var existing = await GetExistingActorNames(dbContext, added, cancellationToken);

        var actorNamePairs = added
            .GroupBy(x => new { x.ActorId, x.Name })
            .GroupJoin(
                inner: existing,
                outerKeySelector: x => x.Key,
                innerKeySelector: x => new { x.ActorId, x.Name },
                resultSelector: (left, right) => (left.ToList(), right.SingleOrDefault()));

        var finalActorNames = new List<ActorName>();
        foreach (var (newActorNames, existingActorName) in actorNamePairs)
        {
            var (mainActorName, discardActorNames) = newActorNames switch
            {
                _ when existingActorName is not null => (existingActorName, newActorNames),
                [var first, ..] => (first, newActorNames.Except([first]).ToList()),
                _ => throw new UnreachableException()
            };

            mainActorName.EnsureId();
            mainActorName.Create(_transactionTime.Value);
            finalActorNames.Add(mainActorName);

            foreach (var actor in discardActorNames.SelectMany(x => x.ActorEntities))
            {
                actor.ActorNameEntity = mainActorName;
            }

            foreach (var discardInstance in discardActorNames)
            {
                dbContext.Entry(discardInstance).State = EntityState.Detached;
            }
        }

        return finalActorNames;
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
}
