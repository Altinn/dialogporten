using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Interceptors;

internal sealed class PopulateActorNameInterceptor : SaveChangesInterceptor
{
    private readonly IDomainContext _domainContext;
    private readonly IPartyNameRegistry _partyNameRegistry;
    private readonly Lazy<IActorNameRepository> _actorNameRepository;

    public PopulateActorNameInterceptor(
        IDomainContext domainContext,
        IPartyNameRegistry partyNameRegistry,
        Lazy<IActorNameRepository> actorNameRepository)
    {
        _domainContext = domainContext ?? throw new ArgumentNullException(nameof(domainContext));
        _partyNameRegistry = partyNameRegistry ?? throw new ArgumentNullException(nameof(partyNameRegistry));
        _actorNameRepository = actorNameRepository ?? throw new ArgumentNullException(nameof(actorNameRepository));
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

        if (!await TrySetActorNames(addedActorNameEntities, cancellationToken))
        {
            return InterceptionResult<int>.SuppressWithResult(0);
        }

        await _actorNameRepository.Value.EnsureActorNames(addedActorNameEntities, cancellationToken);

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Actor names are enrichment, not validation. If the party name registry is unavailable or has no name
    /// for the actor, we persist the actor with a null name instead of failing the request. Null names are
    /// filled in later by the ResyncActorNameEvent handler.
    /// </summary>
    private async Task<bool> TrySetActorNames(
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
            actorNameById[actorName.ActorId!].Switch(
                found => actorName.Name = found.Name,
                notFound => actorName.Name = null,
                unsupported => _domainContext.AddError(
                    nameof(Actor.ActorNameEntity.ActorId),
                    $"Unable to look up name for actor id: {actorName.ActorId}. Reason: {unsupported.Reason}"),
                unavailable => actorName.Name = null
            );
        }

        return _domainContext.IsValid;
    }

    private async Task<(string ActorId, GetPartyNameResult Result)> ActorNameByActorId(string actorId,
        CancellationToken cancellationToken) =>
        (actorId, await _partyNameRegistry.GetName(actorId, cancellationToken));
}
