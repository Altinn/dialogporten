using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Events.ResyncActorName;

public class ResyncActorName(
    IDialogDbContext db,
    IUnitOfWork unitOfWork,
    IPartyNameRegistry partyNameRegistry
) : INotificationHandler<ResyncActorNameEvent>
{
    public async Task Handle(ResyncActorNameEvent resyncActorNameEvent, CancellationToken cancellationToken)
    {
        var outdatedActorNameEntitiy = await db.ActorName
            .Include(x => x.ActorEntities)
            .FirstAsync(x => x.Id == resyncActorNameEvent.ActorNameId, cancellationToken);

        var actorId = outdatedActorNameEntitiy.ActorId;
        if (actorId == null) return;

        var newName = await partyNameRegistry.GetNameOrFail(actorId, cancellationToken);
        var existingActorNewNameEntity = await db.ActorName
            .FirstOrDefaultAsync(x => x.ActorId == actorId && x.Name == newName, cancellationToken);

        var newActorNameEntity = existingActorNewNameEntity ?? new ActorName
        {
            ActorId = actorId,
            Name = newName
        };
        if (existingActorNewNameEntity == null) db.ActorName.Add(newActorNameEntity);

        foreach (var actorEntity in outdatedActorNameEntitiy.ActorEntities)
        {
            actorEntity.ActorNameEntity = newActorNameEntity;
        }

        await unitOfWork
            .DisableAggregateFilter()
            .DisableImmutableFilter()
            .SaveChangesAsync(cancellationToken);
    }
}
