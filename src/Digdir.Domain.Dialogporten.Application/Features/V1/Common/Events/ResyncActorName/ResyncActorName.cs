using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Events.ResyncActorName;

public sealed class ResyncActorName(
    IDialogDbContext db,
    IPartyNameRegistry partyNameRegistry,
    IActorNameRepository actorNameRepository
) : INotificationHandler<ResyncActorNameEvent>
{
    public async Task Handle(ResyncActorNameEvent resyncActorNameEvent, CancellationToken cancellationToken)
    {
        var actorNameId = resyncActorNameEvent.ActorNameId;
        var outdatedActorNameEntity = await db.ActorName
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == actorNameId, cancellationToken);

        if (outdatedActorNameEntity is null) return;

        var actorId = outdatedActorNameEntity.ActorId;
        if (actorId is null) return;

        var newName = await partyNameRegistry.GetName(actorId, cancellationToken);
        var newActorNameId = await actorNameRepository.EnsureActorName(actorId, newName, cancellationToken);
        if (newActorNameId == actorNameId) return;

        await db.Actor
            .Where(x => x.ActorNameEntityId == actorNameId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.ActorNameEntityId, newActorNameId),
                cancellationToken
            );
    }
}
