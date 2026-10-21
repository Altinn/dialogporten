using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Events.ResyncActorName;

public sealed class ResyncActorName(
    IDialogDbContext db,
    IPartyNameRegistry partyNameRegistry,
    IActorNameRepository actorNameRepository,
    ILogger<ResyncActorName> logger
) : INotificationHandler<ResyncActorNameEvent>
{
    public async Task Handle(ResyncActorNameEvent resyncActorNameEvent, CancellationToken cancellationToken)
    {
        var actorNameId = resyncActorNameEvent.ActorNameId;
        var outdatedActorNameEntity = await db.ActorName
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == actorNameId, cancellationToken);

        if (outdatedActorNameEntity is null)
        {
            logger.LogWarning(
                "No ActorName with Id {Id}. This probably means a new ActorName conflicted on insert and was created with another id",
                actorNameId
            );
            return;
        }

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
