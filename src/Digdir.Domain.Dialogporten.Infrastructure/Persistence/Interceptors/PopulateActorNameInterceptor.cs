using System.Diagnostics;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Library.Entity.Abstractions.Features.Creatable;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Interceptors;

internal sealed class PopulateActorNameInterceptor : SaveChangesInterceptor
{
    private readonly IDomainContext _domainContext;
    private readonly IPartyNameRegistry _partyNameRegistry;
    private readonly ITransactionTime _transactionTime;
    private bool _hasBeenExecuted;

    public PopulateActorNameInterceptor(
        ITransactionTime transactionTime,
        IDomainContext domainContext,
        IPartyNameRegistry partyNameRegistry)
    {
        _domainContext = domainContext ?? throw new ArgumentNullException(nameof(domainContext));
        _partyNameRegistry = partyNameRegistry ?? throw new ArgumentNullException(nameof(partyNameRegistry));
        _transactionTime = transactionTime ?? throw new ArgumentNullException(nameof(transactionTime));
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

        var actorNameEntities = dbContext.ChangeTracker
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

        if (!await TrySetActorNames(actorNameEntities, cancellationToken))
        {
            _hasBeenExecuted = true;
            return InterceptionResult<int>.SuppressWithResult(0);
        }

        await ConsolidateActorNameInstances(dbContext, actorNameEntities, cancellationToken);
        _hasBeenExecuted = true;
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task<bool> TrySetActorNames(IEnumerable<ActorName> actorNameEntities, CancellationToken cancellationToken)
    {
        var relevantActorNameEntities = actorNameEntities
            .Where(x => x.ActorId is not null && !x.ActorId.StartsWith(SystemUserIdentifier.Prefix, StringComparison.InvariantCultureIgnoreCase))
            .ToList();

        var actorNameById = (await Task.WhenAll(relevantActorNameEntities
                .Select(x => x.ActorId!)
                .Distinct()
                .Select(x => ActorNameByActorId(x, cancellationToken))))
            .ToDictionary(x => x.ActorId, x => x.ActorName);

        foreach (var actorName in relevantActorNameEntities)
        {
            actorName.Name = actorNameById[actorName.ActorId!];

            // We don't want to fail the save operation if we are unable to look up the
            // name for this particular actor, as it is used on enduser get operations.
            if (!string.IsNullOrWhiteSpace(actorName.Name)
             || actorName.ActorEntities.All(x => x is DialogSeenLogSeenByActor))
            {
                continue;
            }

            _domainContext.AddError(nameof(Actor.ActorNameEntity.ActorId), $"Unable to look up name for actor id: {actorName.ActorId}");
        }

        return _domainContext.IsValid;
    }

    private async Task ConsolidateActorNameInstances(DbContext dbContext, List<ActorName> added, CancellationToken cancellationToken)
    {
        // There may be an actorNameEntity with an Id where name is null.
        // One can argue that this method should consider those as well.
        // however we chose to put that responsibility to actorName clean up job.
        var existing = await GetExistingActorNames(dbContext, added, cancellationToken);

        var actorNamePairs = added
            .GroupBy(x => new { x.ActorId, x.Name })
            .GroupJoin(
                inner: existing,
                outerKeySelector: x => x.Key,
                innerKeySelector: x => new { x.ActorId, x.Name },
                resultSelector: (left, right) => (left.ToList(), right.SingleOrDefault()));

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
            foreach (var actor in discardActorNames.SelectMany(x => x.ActorEntities))
            {
                actor.ActorNameEntity = mainActorName;
            }

            foreach (var discardInstance in discardActorNames)
            {
                dbContext.Entry(discardInstance).State = EntityState.Detached;
            }
        }
    }

    private async Task<(string ActorId, string? ActorName)> ActorNameByActorId(string actorId, CancellationToken cancellationToken) =>
        (actorId, await _partyNameRegistry.GetName(actorId, cancellationToken));

    private static async Task<List<ActorName>> GetExistingActorNames(DbContext dbContext, IEnumerable<ActorName> actorNameEntities, CancellationToken cancellationToken)
    {
        // Why are we doing "composite key contains" this way, you ask?
        // See https://stackoverflow.com/a/26201371/2301766
        var distinctIdNameTupples = actorNameEntities
            .Select(x => (x.ActorId, x.Name))
            .Distinct()
            .ToList();
        var actorIds = distinctIdNameTupples.Select(x => x.ActorId).Distinct();
        var actorNames = distinctIdNameTupples.Select(x => x.Name).Distinct();
        var existingActorNames = await dbContext.Set<ActorName>()
            .Where(x => actorIds.Contains(x.ActorId) && actorNames.Contains(x.Name))
            .ToListAsync(cancellationToken);
        return existingActorNames
            .Where(x => distinctIdNameTupples.Any(xx => xx.ActorId == x.ActorId && xx.Name == x.Name))
            .ToList();
    }
}
