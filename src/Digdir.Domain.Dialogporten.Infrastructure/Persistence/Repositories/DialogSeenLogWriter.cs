using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Domain.Dialogporten.Domain.Parties.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

internal sealed class DialogSeenLogWriter(
    DialogDbContext db,
    IPartyNameRegistry partyNameRegistry,
    ITransactionTime transactionTime,
    IUnitOfWork unitOfWork,
    IActorNameRepository actorNameRepository
) : IDialogSeenLogWriter
{

    /// <summary>
    /// This method handles everything that needs to be done when a dialog is "seen":
    /// - Flips IsSeenSinceLastContentUpdate to true, unless another thread modified the dialog content
    /// - Removes any system label "MarkedAsUnopened", unless another thread already removed the system label
    /// - Adds a remove-entry to the LabelAssignmentLog if we removed a SystemLabel
    ///
    /// Important: To prevent excessive row-locking, we allow race conditions.
    /// This means this method must only do atomic and thread-safe updates in single sql statements.
    /// </summary>
    /// <param name="dialog"></param>
    /// <param name="userId"></param>
    /// <param name="cancellationToken"></param>
    public async Task<DialogSeenResult?> OnSeen(
        DialogEntity dialog,
        UserId userId,
        CancellationToken cancellationToken
    )
    {
        var isSeen = dialog.IsSeenBy(userId.ExternalIdWithPrefix) &&
                     !dialog.IsMarkedAsUnopened() &&
                     dialog.IsSeenSinceLastContentUpdate;
        if (isSeen)
        {
            return null;
        }

        // Use a deterministic id to make the seen-log insert idempotent.
        // If multiple seen events are produced without dialog changes in between,
        // they represent the same logical "seen" and should not create duplicates.
        var seenLogId = dialog.Id.CreateDeterministicSubUuidV7(
            $"{dialog.UpdatedAt:O}{dialog.IsSeenSinceLastContentUpdate}{userId.ExternalIdWithPrefix}");

        var seenLogExists = dialog.SeenLog.Any(s => s.Id == seenLogId);
        if (seenLogExists && !dialog.IsMarkedAsUnopened())
        {
            return null;
        }
        var actorNameEntity = await CreateActorNameEntity(userId.ExternalIdWithPrefix, cancellationToken);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        var newSeenLog = await EnsureSeenLog(
            seenLogId,
            dialog.Id,
            actorNameEntity,
            userId.Type,
            transactionTime.Value,
            cancellationToken
        );

        dialog.EndUserContext.UpdateSystemLabels(
            addLabels: [],
            removeLabels: [SystemLabel.Values.MarkedAsUnopened],
            new LabelAssignmentLogActor
            {
                ActorTypeId = ActorType.Values.PartyRepresentative,
                ActorNameEntityId = actorNameEntity.Id,
            }
        );

        var isSeenSinceLastContentUpdateSetToTrue = (await MarkThisRevisionAsSeen(dialog, cancellationToken)) > 0;

        return new DialogSeenResult(
            NewSeenLog: newSeenLog,
            CausedChangesOutsideEf: isSeenSinceLastContentUpdateSetToTrue || newSeenLog is not null,
            IsContentSeen: isSeenSinceLastContentUpdateSetToTrue || dialog.IsSeenSinceLastContentUpdate,
            OutOfSyncActorNameEntityId: actorNameEntity.Name is null ? actorNameEntity.Id : null
        );
    }

    private Task<int> MarkThisRevisionAsSeen(DialogEntity dialog, CancellationToken cancellationToken)
    {
        return db.Database
            .ExecuteSqlInterpolatedAsync($"""
                                          UPDATE "Dialog" 
                                          SET "IsSeenSinceLastContentUpdate"=true
                                          WHERE "Id"={dialog.Id}
                                            AND "ContentUpdatedAt"={dialog.ContentUpdatedAt}
                                          """, cancellationToken);
    }

    private sealed record EnsureSeenLogResult(
        DialogSeenLog? NewDialogSeenLog,
        bool OutOfSyncActorNameId
    );

    private async Task<DialogSeenLog?> EnsureSeenLog(
        Guid seenLogId,
        Guid dialogId,
        ActorName actorNameEntity,
        DialogUserType.Values userType,
        DateTimeOffset seenAt,
        CancellationToken cancellationToken)
    {
        var actorType = ActorType.Values.PartyRepresentative;

        var rowsUpdated = await EnsureSeenLog(seenLogId, dialogId, userType, seenAt, cancellationToken);
        await EnsureSeenByActor(seenLogId, actorNameEntity.Id, actorType, cancellationToken);

        return rowsUpdated == 0
            ? null
            : new DialogSeenLog
            {
                Id = seenLogId,
                CreatedAt = seenAt,
                SeenBy = new DialogSeenLogSeenByActor
                {
                    ActorNameEntityId = actorNameEntity.Id,
                    ActorNameEntity = actorNameEntity,
                    ActorTypeId = actorType
                }
            };
    }

    private async Task<ActorName> CreateActorNameEntity(string actorId, CancellationToken ct)
    {
        var normalizedActorId = actorId.ToLowerInvariant();
        var result = await partyNameRegistry.GetName(normalizedActorId, ct);
        var actorName = result.Match<string?>(
            found => found.Name,
            notFound =>
            {
                if (PartyIdentifier.TryParse(actorId, out var partyId) && partyId is SystemUserIdentifier)
                {
                    return null;
                }

                throw new InvalidOperationException($"Actor {actorId} not found");
            },
            unsupported => throw new InvalidOperationException($"Unable to parse actor {actorId}"),
            unavailable => null
        );

        var actorNameId = await actorNameRepository.GetOrCreateActorName(normalizedActorId, actorName, ct);
        return new ActorName
        {
            Id = actorNameId,
            ActorId = normalizedActorId,
            Name = actorName
        };
    }

    private async Task<int> EnsureSeenLog(
        Guid seenLogId,
        Guid dialogId,
        DialogUserType.Values userType,
        DateTimeOffset seenAt,
        CancellationToken cancellationToken)
    {
        return await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO "DialogSeenLog" ("Id", "CreatedAt", "DialogId", "EndUserTypeId", "IsViaServiceOwner")
             VALUES (
                 {seenLogId},
                 {seenAt},
                 {dialogId},
                 {(int)userType},
                 {userType == DialogUserType.Values.ServiceOwnerOnBehalfOfPerson})
             ON CONFLICT ("Id") DO NOTHING
             """,
            cancellationToken
        );
    }

    private async Task EnsureSeenByActor(
        Guid seenLogId,
        Guid actorNameId,
        ActorType.Values actorType,
        CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"""
                                                       INSERT INTO "Actor" ("Id", "ActorNameEntityId", "ActorTypeId", "CreatedAt", "DialogSeenLogId", "Discriminator")
                                                       VALUES (
                                                           {Guid.CreateVersion7()},
                                                           {actorNameId},
                                                           {(int)actorType},
                                                           {transactionTime.Value},
                                                           {seenLogId},
                                                           {nameof(DialogSeenLogSeenByActor)})
                                                       ON CONFLICT ("DialogSeenLogId")
                                                       WHERE "DialogSeenLogId" IS NOT NULL
                                                       DO NOTHING
                                                       """, cancellationToken);
    }
}
