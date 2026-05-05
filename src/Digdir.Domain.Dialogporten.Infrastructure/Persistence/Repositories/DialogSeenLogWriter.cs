using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

internal sealed class DialogSeenLogWriter(
    DialogDbContext db,
    IPartyNameRegistry partyNameRegistry,
    ITransactionTime transactionTime,
    IUnitOfWork unitOfWork
) : IDialogSeenLogWriter
{
    /// <summary>
    /// This method handles everything that needs to be done when a dialog is "seen":
    /// - Flips IsSeenSinceLastContentUpdate to true, unless another thread modified the dialog content
    /// - Removes any system label "MarkedAsUnopened", unless another thread already removed the system label
    /// - Adds a remove-entry to the LabelAssignmentLog if we removed a SystemLabel
    ///
    /// Important: To prevent excessive row-locking, we allow race conditions.
    /// This means this method must only do atomic updates in single sql statements.
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
            $"{dialog.UpdatedAt:O}{dialog.IsSeenSinceLastContentUpdate}{userId}");

        var seenLogExists = dialog.SeenLog.Any(s => s.Id == seenLogId);
        if (seenLogExists && !dialog.IsMarkedAsUnopened())
        {
            return null;
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        var seenLogWriteResult = await EnsureSeenLog(
            seenLogId,
            dialog.Id,
            userId.ExternalIdWithPrefix,
            userId.Type,
            transactionTime.Value,
            cancellationToken);

        dialog.EndUserContext.UpdateSystemLabels(
            addLabels: [],
            removeLabels: [SystemLabel.Values.MarkedAsUnopened],
            new LabelAssignmentLogActor
            {
                ActorTypeId = ActorType.Values.PartyRepresentative,
                ActorNameEntityId = seenLogWriteResult.ActorNameId
            }
        );

        var rowsUpdated = await db.Dialogs
            .Where(x => x.Id == dialog.Id && x.ContentUpdatedAt == dialog.ContentUpdatedAt)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.IsSeenSinceLastContentUpdate, true), cancellationToken);
        var newIsSeenSinceLastContentUpdate = rowsUpdated > 0 || dialog.IsSeenSinceLastContentUpdate;

        return new DialogSeenResult(seenLogWriteResult.DialogSeenLog, newIsSeenSinceLastContentUpdate);
    }

    private sealed record EnsureSeenLogResult(
        Guid ActorNameId,
        DialogSeenLog? DialogSeenLog
    );

    private async Task<EnsureSeenLogResult> EnsureSeenLog(
        Guid seenLogId,
        Guid dialogId,
        string actorId,
        DialogUserType.Values userType,
        DateTimeOffset seenAt,
        CancellationToken cancellationToken)
    {
        var normalizedActorId = actorId.ToLowerInvariant();
        var actorName = await partyNameRegistry.GetName(normalizedActorId, cancellationToken);
        if (string.IsNullOrWhiteSpace(actorName))
        {
            throw new InvalidOperationException("Unable to look up actor name.");
        }

        var actorNameId = await EnsureActorName(normalizedActorId, actorName, cancellationToken);
        var actorType = ActorType.Values.PartyRepresentative;

        var rowsUpdated = await EnsureSeenLog(seenLogId, dialogId, userType, seenAt, cancellationToken);
        await EnsureSeenByActor(seenLogId, actorNameId, actorType, cancellationToken);

        return new EnsureSeenLogResult
        (
            actorNameId,
            rowsUpdated == 0 ? null : new DialogSeenLog
            {
                Id = seenLogId,
                CreatedAt = seenAt,
                SeenBy = new DialogSeenLogSeenByActor
                {
                    ActorNameEntity = new ActorName
                    {
                        ActorId = normalizedActorId,
                        Name = actorName
                    },
                    ActorTypeId = actorType
                }
            }
        );
    }

    private async Task<Guid> EnsureActorName(string actorId, string actorName, CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"""
                                                       INSERT INTO "ActorName" ("Id", "ActorId", "Name", "CreatedAt")
                                                       VALUES ({Guid.CreateVersion7()}, {actorId}, {actorName}, {transactionTime.Value})
                                                       ON CONFLICT ("ActorId", "Name") DO NOTHING
                                                       """, cancellationToken);

        return await db.Database
            .SqlQuery<Guid>($"""
                             SELECT "Id" AS "Value"
                             FROM "ActorName"
                             WHERE "ActorId" = {actorId}
                               AND "Name" = {actorName}
                             LIMIT 1
                             """)
            .SingleAsync(cancellationToken);
    }

    private async Task<int> EnsureSeenLog(
        Guid seenLogId,
        Guid dialogId,
        DialogUserType.Values userType,
        DateTimeOffset seenAt,
        CancellationToken cancellationToken)
    {
        return await db.Database.ExecuteSqlInterpolatedAsync($"""
                                                       INSERT INTO "DialogSeenLog" ("Id", "CreatedAt", "DialogId", "EndUserTypeId", "IsViaServiceOwner")
                                                       VALUES (
                                                           {seenLogId},
                                                           {seenAt},
                                                           {dialogId},
                                                           {(int)userType},
                                                           {userType == DialogUserType.Values.ServiceOwnerOnBehalfOfPerson})
                                                       ON CONFLICT ("Id") DO NOTHING
                                                       """, cancellationToken);
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
