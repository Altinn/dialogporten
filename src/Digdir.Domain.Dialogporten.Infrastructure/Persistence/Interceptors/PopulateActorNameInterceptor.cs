using System.Diagnostics;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Library.Entity.Abstractions.Features.Creatable;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using MassTransit.Util;
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

        var actors = dbContext.ChangeTracker.Entries<ActorName>()
            .Where(e => e.State is EntityState.Added)
            .Where(e => e.Entity.ActorId is not null)
            .Select(e =>
            {
                var entity = e.Entity;
                entity.ActorId = entity.ActorId?.ToLowerInvariant();
                return entity;
            })
            .ToList();

        var actorNameById = (await Task.WhenAll(actors
                .Select(x => x.ActorId!)
                .Distinct()
                .Select(x => ActorNameByActorId(x, cancellationToken))))
            .ToDictionary(x => x.ActorId, x => x.ActorName);

        foreach (var actor in actors)
        {
            actor.Name = actorNameById[actor.ActorId!];

            if (!string.IsNullOrWhiteSpace(actor.Name))
            {
                continue;
            }

            // We don't want to fail the save operation if we are unable to look up the
            // name for this particular actor, as it is used on enduser get operations.
            if (actor.ActorEntities.All(x => x is DialogSeenLogSeenByActor))
            {
                continue;
            }

            _domainContext.AddError(nameof(Actor.ActorNameEntity.ActorId), $"Unable to look up name for actor id: {actor.ActorId}");
        }

        if (!_domainContext.IsValid)
        {
            _hasBeenExecuted = true;
            return InterceptionResult<int>.SuppressWithResult(0);
        }

        // There may be an actorNameEntity with an Id where name is null.
        // One can argue that this method should consider those as well.
        // however we chose to put that responsibility to actorName clean up job.
        var existingActorNames = await dbContext.Set<ActorName>()
            .Where(x => actorNameById
                .Select(x => new
                {
                    ActorId = (string?)x.Key,
                    Name = x.Value
                })
                .Contains(new
                {
                    x.ActorId,
                    x.Name
                }))
            .ToListAsync(cancellationToken);

        var newAndExistingActorNameTuples = actors
            .GroupBy(x => new
            {
                x.ActorId,
                x.Name
            })
            .GroupJoin(existingActorNames,
                x => x.Key,
                x => new
                {
                    x.ActorId,
                    x.Name
                },
                (left, right) => (left.ToList(), right.SingleOrDefault()));

        foreach (var (newActorNames, existingActorName) in newAndExistingActorNameTuples)
        {
            var (mainInstance, discardInstances) = newActorNames switch
            {
                _ when existingActorName is not null => (existingActorName, newActorNames),
                [var first, ..] => (first, newActorNames.Except([first]).ToList()),
                _ => throw new UnreachableException()
            };

            mainInstance.CreateId();
            mainInstance.Create(_transactionTime.Value);

            foreach (var actor in discardInstances.SelectMany(x => x.ActorEntities))
            {
                actor.ActorNameEntity = mainInstance;
            }

            foreach (var discardInstance in discardInstances)
            {
                dbContext.Entry(discardInstance).State = EntityState.Detached;
            }
        }

        _hasBeenExecuted = true;
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
    private async Task<(string ActorId, string? ActorName)> ActorNameByActorId(string actorId, CancellationToken cancellationToken = default)
    {
        return (actorId, await _partyNameRegistry.GetName(actorId, cancellationToken));
    }
}
