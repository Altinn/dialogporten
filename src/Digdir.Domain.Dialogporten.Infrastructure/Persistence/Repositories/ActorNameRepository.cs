using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Externals;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

internal sealed class ActorNameRepository(DialogDbContext db, ITransactionTime transactionTime) : IActorNameRepository
{
    public async Task<Guid> GetOrCreateActorName(string actorId, string? actorName, CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"""
                                                       INSERT INTO "ActorName" ("Id", "ActorId", "Name", "CreatedAt")
                                                       VALUES ({Guid.CreateVersion7()}, {actorId}, {actorName}, {transactionTime.Value})
                                                       ON CONFLICT ("ActorId", "Name") DO NOTHING
                                                       """, cancellationToken);

        var query = new PostgresFormattableStringBuilder()
            .Append($"""
                      SELECT "Id" AS "Value"
                      FROM "ActorName"
                      WHERE "ActorId" = {actorId}
                     """)
            .AppendIf(actorName != null, $""" AND "Name" = {actorName}""")
            .AppendIf(actorName == null, """ AND "Name" IS NULL""")
            .Append(" LIMIT 1");

        return await db.Database
            .SqlQuery<Guid>(query.ToFormattableString())
            .SingleAsync(cancellationToken);
    }

}
