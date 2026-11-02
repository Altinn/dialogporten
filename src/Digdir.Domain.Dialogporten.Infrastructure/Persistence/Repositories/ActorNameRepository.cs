using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Externals;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

internal sealed class ActorNameRepository(DialogDbContext db, ITransactionTime transactionTime) : IActorNameRepository
{
    public async Task<Guid> GetOrCreateActorName(string actorId, string? actorName, CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO "ActorName" ("Id", "ActorId", "Name", "CreatedAt")
            VALUES ({Guid.CreateVersion7()}, {actorId}, {actorName}, {transactionTime.Value})
            ON CONFLICT ("ActorId", "Name") DO NOTHING
            """, cancellationToken);

        var query = new PostgresFormattableStringBuilder()
            .Append($"""
                      SELECT "Id" AS "Value"
                      FROM "ActorName"
                      WHERE "ActorId" = {actorId}
                     """);
        query = actorName is not null
            ? query.Append($" AND \"Name\" = {actorName}")
            : query.Append(" AND \"Name\" IS NULL");
        query.Append(" LIMIT 1");

        return await db.Database
            .SqlQuery<Guid>(query.ToFormattableString())
            .SingleAsync(cancellationToken);
    }

}
