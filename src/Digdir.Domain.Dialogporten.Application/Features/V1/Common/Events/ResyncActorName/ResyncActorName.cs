using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Actors.Events;
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
    private const int BatchSize = 1000;

    public async Task Handle(ResyncActorNameEvent resyncActorNameEvent, CancellationToken cancellationToken)
    {
        var actorNameId = resyncActorNameEvent.ActorNameId;
        var outdatedActorNameEntity = await db.ActorName
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == actorNameId, cancellationToken);

        if (outdatedActorNameEntity is null) return;

        var actorId = outdatedActorNameEntity.ActorId;
        if (actorId is null) return;
        if (!await db.Actor.AnyAsync(x => x.ActorNameEntityId == actorNameId, cancellationToken)) return;

        var getNameResult = await partyNameRegistry.GetName(actorId, cancellationToken);
        await getNameResult.Match(
            result => UpdateActors(result, actorId, actorNameId, cancellationToken),
            notFound => throw new InvalidOperationException($"Actor name not found for ActorId: {notFound.ExternalIdWithPrefix}"),
            unsupported =>
            {
                logger.LogError(
                    "Unable to parse ActorId: {ExternalIdWithPrefix}. Reason: {Reason}",
                    unsupported.ExternalIdWithPrefix, unsupported.Reason
                );
                return Task.CompletedTask;
            },
            unavailable => throw new InvalidOperationException($"Party name registry unavailable. ActorId: {unavailable.ExternalIdWithPrefix}")
        );
    }

    private async Task UpdateActors(
        PartyNameFound result,
        string actorId,
        Guid actorNameId,
        CancellationToken cancellationToken
    )
    {
        var newActorNameId = await actorNameRepository.GetOrCreateActorName(actorId, result.Name, cancellationToken);
        if (newActorNameId == actorNameId) return;

        int updated;
        do
        {
            updated = await db.Actor
                .Where(x => x.ActorNameEntityId == actorNameId)
                .Take(BatchSize)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(x => x.ActorNameEntityId, newActorNameId),
                    cancellationToken
                );
        } while (updated == BatchSize);

        await db.ActorName
            .Where(x => x.Id == actorNameId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
