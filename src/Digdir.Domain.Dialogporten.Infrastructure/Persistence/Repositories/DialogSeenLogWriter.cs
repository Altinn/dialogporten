using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

internal sealed class DialogSeenLogWriter(
    DialogDbContext db,
    IPartyNameRegistry partyNameRegistry,
    ITransactionTime transactionTime) : IDialogSeenLogWriter
{
    public async Task<DialogSeenLogWriteResult> EnsureSeenLog(
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
        await EnsureSeenLog(seenLogId, dialogId, userType, seenAt, cancellationToken);
        await EnsureSeenByActor(seenLogId, actorNameId, cancellationToken);

        return new DialogSeenLogWriteResult(actorNameId, normalizedActorId, actorName);
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

    private async Task EnsureSeenLog(
        Guid seenLogId,
        Guid dialogId,
        DialogUserType.Values userType,
        DateTimeOffset seenAt,
        CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"""
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
        CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Actor" ("Id", "ActorNameEntityId", "ActorTypeId", "CreatedAt", "DialogSeenLogId", "Discriminator")
            VALUES (
                {Guid.CreateVersion7()},
                {actorNameId},
                {(int)ActorType.Values.PartyRepresentative},
                {transactionTime.Value},
                {seenLogId},
                {nameof(DialogSeenLogSeenByActor)})
            ON CONFLICT ("DialogSeenLogId")
            WHERE "DialogSeenLogId" IS NOT NULL
            DO NOTHING
            """, cancellationToken);
    }
}
