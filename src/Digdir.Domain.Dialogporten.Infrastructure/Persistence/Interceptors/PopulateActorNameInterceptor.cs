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
        if (eventData.Context is not null)
        {
            await PopulateActorNames(eventData.Context, cancellationToken);
        }

        return _domainContext.IsValid
            ? await base.SavingChangesAsync(eventData, result, cancellationToken)
            : InterceptionResult<int>.SuppressWithResult(0);
    }

    private async ValueTask PopulateActorNames(DbContext dbContext, CancellationToken cancellationToken)
    {
        using var _ = dbContext.DisableAutoDetectChanges();
        dbContext.ChangeTracker.DetectChanges();
        var addedActorNameEntities = dbContext.ChangeTracker
            .Entries<ActorName>()
            .Where(e => e.State is EntityState.Added)
            .Where(e => e.Entity.ActorId is not null || e.Entity.Name is not null)
            .Select(e => e.Entity)
            .ToList();

        if (addedActorNameEntities.Count == 0) return;
        await TrySetActorNames(addedActorNameEntities, cancellationToken);
        if (!_domainContext.IsValid) return;
        await EnsureTransaction(dbContext, cancellationToken);
        await _actorNameRepository.Value.EnsureActorNames(addedActorNameEntities, cancellationToken);
    }

    private static async Task EnsureTransaction(DbContext dbContext, CancellationToken cancellationToken)
    {
        if (dbContext.Database.CurrentTransaction is null)
        {
            // Here we assume that UnitOfWork will pick up this transaction
            // and commit or rollback it when SaveChanges is called.
            await dbContext.Database.BeginTransactionAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Actor names are enrichment, not validation. If the party name registry is unavailable or has no name
    /// for the actor, we persist the actor with a null name instead of failing the request. Null names are
    /// filled in later by the ResyncActorNameEvent handler.
    /// </summary>
    private async Task TrySetActorNames(
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
    }

    private async Task<(string ActorId, GetPartyNameResult Result)> ActorNameByActorId(string actorId,
        CancellationToken cancellationToken) =>
        (actorId, await _partyNameRegistry.GetName(actorId, cancellationToken));
}
