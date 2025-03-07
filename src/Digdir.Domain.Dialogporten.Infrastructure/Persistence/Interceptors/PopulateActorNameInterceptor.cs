using System.Diagnostics;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Interceptors;

internal sealed class PopulateActorNameInterceptor : SaveChangesInterceptor
{
    private static readonly Type ActorType = typeof(Actor);

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

        var actors = dbContext.ChangeTracker.Entries()
            .Where(x => x.Metadata.ClrType.IsAssignableTo(ActorType))
            .Where(x => x.State is EntityState.Added or EntityState.Modified)
            .Select(x =>
            {
                var actor = (Actor)x.Entity;
                actor.ActorNameEntity!.ActorId = actor.ActorNameEntity.ActorId?.ToLowerInvariant();
                return actor;
            })
            .Where(x => x.ActorNameEntity?.ActorId != null)
            .ToList();

        var actorNameById = new Dictionary<string, string?>();
        foreach (var actorId in actors
            .Select(x => x.ActorNameEntity?.ActorId!)
            .Distinct())
        {
            actorNameById[actorId] = await _partyNameRegistry.GetName(actorId, cancellationToken);
        }

        foreach (var actor in actors)
        {
            if (!actorNameById.TryGetValue(actor.ActorNameEntity?.ActorId!, out var actorName))
            {
                throw new UnreachableException(
                    $"Expected {nameof(actorNameById)} to contain a record for every " +
                    $"actor id. Missing record for actor id: {actor.ActorNameEntity?.ActorId}. Is " +
                    $"the lookup method implemented correctly?");
            }

            if (string.IsNullOrWhiteSpace(actorName))
            {
                // We don't want to fail the save operation if we are unable to look up the
                // name for this particular actor, as it is used on enduser get operations.
                if (actor is DialogSeenLogSeenByActor)
                {
                    continue;
                }

                _domainContext.AddError(nameof(Actor.ActorNameEntity.ActorId), $"Unable to look up name for actor id: {actor.ActorNameEntity?.ActorId}");
                continue;
            }

            var actorNameEntity = await dbContext.Set<ActorName>()
                .FirstOrDefaultAsync(x => x.ActorId == actor.ActorNameEntity!.ActorId && x.Name == actorName, cancellationToken);
            if (actorNameEntity is null)
            {
                actor.ActorNameEntity = new ActorName
                {
                    Id = IdentifiableExtensions.CreateVersion7(),
                    CreatedAt = _transactionTime.Value,
                    Name = actorName,
                    ActorId = actor.ActorNameEntity?.ActorId
                };
                dbContext.Add(actor.ActorNameEntity);
            }
            else
            {
                actor.ActorNameEntity = actorNameEntity;
            }

        }

        _hasBeenExecuted = true;
        return !_domainContext.IsValid
            ? InterceptionResult<int>.SuppressWithResult(0)
            : await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
