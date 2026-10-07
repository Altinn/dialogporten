using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Events.ResyncActorName;

public sealed class ResyncActorName(
    IDialogDbContext db,
    IUnitOfWork unitOfWork,
    IPartyNameRegistry partyNameRegistry,
    ILogger<ResyncActorName> logger
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

        if (resyncActorNameEvent.DisableUpdateableFilter) unitOfWork.DisableUpdatableFilter();

        var result = await unitOfWork
            .DisableAggregateFilter()
            .DisableImmutableFilter()
            .SaveChangesAsync(cancellationToken);

        result.Match<SaveChangesResult>(
            success => success,
            domainError =>
            {
                var errors = domainError.Errors.Select(error => $"{error.PropertyName} = {error.ErrorMessage}");
                var errorsString = string.Join(", ", errors);
                logger.LogError(
                    "Domain error on {Class}, for {Event}. With errors: {Errors}",
                    nameof(ResyncActorName),
                    resyncActorNameEvent.EventId,
                    errorsString
                );
                throw new InvalidOperationException($"Failed to save changes for {nameof(ResyncActorName)}");
            },
            concurrencyError =>
            {
                logger.LogError(
                    "Concurrency error on {Class}, for {Event}",
                    nameof(ResyncActorName),
                    resyncActorNameEvent.EventId
                );
                throw new InvalidOperationException($"Failed to save changes for {nameof(ResyncActorName)}");
            },
            conflict =>
            {
                logger.LogError(
                    "Conflict on error on {Class}, for {Event}: {PropertyName} = {ErrorMessage}",
                    nameof(ResyncActorName),
                    resyncActorNameEvent.EventId,
                    conflict.PropertyName,
                    conflict.ErrorMessage
                );
                throw new InvalidOperationException($"Failed to save changes for {nameof(ResyncActorName)}");
            }
        );
    }
}
