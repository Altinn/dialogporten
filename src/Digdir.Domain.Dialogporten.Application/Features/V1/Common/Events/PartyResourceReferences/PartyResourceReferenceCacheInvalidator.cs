using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Events;
using MediatR;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Events.PartyResourceReferences;

/// <summary>
/// Invalidates per-party pruning cache on create and delete events.
///
/// Note: Dialog soft-deletes are intentionally not tracked in <c>partyresource</c> to avoid
/// extra update-trigger load. This invalidation therefore does not guarantee immediate
/// pruning-table convergence for soft-deletes; convergence is expected after regular
/// purge/hard-delete operations.
/// </summary>
internal sealed class PartyResourceReferenceCacheInvalidator : INotificationHandler<DialogCreatedDomainEvent>, INotificationHandler<DialogDeletedDomainEvent>
{
    private readonly IPartyResourceReferenceRepository _repo;

    public PartyResourceReferenceCacheInvalidator(IPartyResourceReferenceRepository repo)
    {
        ArgumentNullException.ThrowIfNull(repo);

        _repo = repo;
    }

    public async Task Handle(DialogCreatedDomainEvent notification, CancellationToken cancellationToken) =>
        await _repo.InvalidateCachedReferencesForParty(notification.Party, cancellationToken);

    public async Task Handle(DialogDeletedDomainEvent notification, CancellationToken cancellationToken) =>
        await _repo.InvalidateCachedReferencesForParty(notification.Party, cancellationToken);
}
