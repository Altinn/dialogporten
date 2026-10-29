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
        var actorName = await db.ActorName
            .Select(x => new { x.Id, x.ActorId, HasActors = x.ActorEntities.Any() })
            .FirstOrDefaultAsync(x => x.Id == resyncActorNameEvent.ActorNameId, cancellationToken);

        if (actorName?.ActorId is null)
        {
            return;
        }

        if (!actorName.HasActors)
        {
            await DeleteActorName(actorName.Id, cancellationToken);
            return;
        }

        var getNameResult = await partyNameRegistry.GetName(actorName.ActorId, cancellationToken);
        await getNameResult.Match(
            result => UpdateActors(result, actorName.ActorId, actorName.Id, cancellationToken),
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
        Guid nullActorNameId,
        CancellationToken cancellationToken
    )
    {
        var newActorNameId = await actorNameRepository.GetOrCreateActorName(actorId, result.Name, cancellationToken);
        if (newActorNameId == nullActorNameId) return;

        int updatedRows;
        do
        {
            updatedRows = await db.Actor
                .Where(x => x.ActorNameEntityId == nullActorNameId)
                .Take(BatchSize)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(x => x.ActorNameEntityId, newActorNameId),
                    cancellationToken
                );
        } while (updatedRows == BatchSize);

        await DeleteActorName(nullActorNameId, cancellationToken);
    }

    private Task<int> DeleteActorName(Guid actorNameId, CancellationToken cancellationToken) =>
        db.ActorName.Where(x => x.Id == actorNameId).ExecuteDeleteAsync(cancellationToken);
}
