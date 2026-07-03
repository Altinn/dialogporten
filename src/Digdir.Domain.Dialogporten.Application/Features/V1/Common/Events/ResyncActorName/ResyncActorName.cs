using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Events.ResyncActorName;

public class ResyncActorName(
    IDialogDbContext db,
    IUnitOfWork unitOfWork
) : INotificationHandler<ResyncActorNameEvent>
{
    public async Task Handle(ResyncActorNameEvent resyncActorNameEvent, CancellationToken cancellationToken)
    {
        var existingActorNameEntity = db.ActorName
            .Include(x => x.ActorEntities)
            .First(x => x.Id == resyncActorNameEvent.ActorNameId);

        if (existingActorNameEntity.Name != null) return;

        var newActorName = new ActorName
        {
            ActorId = existingActorNameEntity.ActorId
        };

        foreach (var actorEntity in existingActorNameEntity.ActorEntities)
        {
            actorEntity.ActorNameEntity = newActorName;
        }

        db.ActorName.Add(newActorName);
        await unitOfWork
            .DisableAggregateFilter()
            .DisableImmutableFilter()
            .SaveChangesAsync(cancellationToken);
    }
}
