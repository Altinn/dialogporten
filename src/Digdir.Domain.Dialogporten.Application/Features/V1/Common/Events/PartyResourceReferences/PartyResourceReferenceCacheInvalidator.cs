using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Events;
using MediatR;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Events.PartyResourceReferences;

internal sealed class PartyResourceReferenceCacheInvalidator(IPartyResourceReferenceRepository repo) : INotificationHandler<DialogCreatedDomainEvent>, INotificationHandler<DialogDeletedDomainEvent>
{
    private readonly IPartyResourceReferenceRepository _repo = repo ?? throw new ArgumentNullException(nameof(repo));

    public async Task Handle(DialogCreatedDomainEvent notification, CancellationToken cancellationToken) =>
        await _repo.InvalidateCachedReferencesForParty(notification.Party, cancellationToken);

    public async Task Handle(DialogDeletedDomainEvent notification, CancellationToken cancellationToken) =>
        await _repo.InvalidateCachedReferencesForParty(notification.Party, cancellationToken);
}
