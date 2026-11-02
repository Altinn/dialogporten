using System.Text.Json;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Actors;
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

    public async Task EnsureActorNames(IEnumerable<ActorName> actorNames, CancellationToken cancellationToken)
    {
        var concreteActorNames = actorNames as List<ActorName> ?? actorNames.ToList();

        var json = JsonSerializer.Serialize(concreteActorNames
            .Select(x => new { x.Id, x.ActorId, x.Name }));
        var query = new PostgresFormattableStringBuilder()
            .Append(
                $"""
                 WITH input_rows AS (
                     SELECT *
                     FROM jsonb_to_recordset({json}::jsonb)
                          AS t("Id" uuid, "ActorId" varchar(255), "Name" varchar(255))
                 )
                 , ins AS (
                     INSERT INTO "ActorName" ("Id", "ActorId", "Name")
                     SELECT "Id", "ActorId", "Name" FROM input_rows
                     ORDER BY "ActorId", "Name"
                     ON CONFLICT ("ActorId", "Name") DO NOTHING
                     RETURNING "Id", "ActorId", "Name"
                 )
                 , sel AS (
                     SELECT "Id", "ActorId", "Name" FROM ins
                     UNION ALL
                     SELECT an."Id", an."ActorId", an."Name"
                     FROM input_rows i
                     JOIN "ActorName" an
                       ON an."ActorId" = i."ActorId"
                      AND an."Name" IS NOT DISTINCT FROM i."Name"
                 )
                 , ups AS (
                     INSERT INTO "ActorName" AS an ("Id", "ActorId", "Name")
                     SELECT i."Id", i."ActorId", i."Name"
                     FROM input_rows i
                     LEFT JOIN sel s
                       ON s."ActorId" IS NOT DISTINCT FROM i."ActorId"
                      AND s."Name"    IS NOT DISTINCT FROM i."Name"
                     WHERE s."Id" IS NULL
                     ORDER BY i."ActorId", i."Name"
                     ON CONFLICT ("ActorId", "Name") DO UPDATE SET "Name" = an."Name"
                     RETURNING "Id", "ActorId", "Name"
                 )
                 , resolved AS (
                     SELECT * FROM sel
                     UNION ALL
                     SELECT * FROM ups
                 )
                 SELECT i."Id"          AS "InputActorNameId",
                        r."Id"          AS "ActualActorNameId"
                 FROM input_rows i
                 JOIN resolved r
                   ON r."ActorId" IS NOT DISTINCT FROM i."ActorId"
                  AND r."Name"    IS NOT DISTINCT FROM i."Name";
                 """);

        var result = await db.Database
            .SqlQuery<(Guid, Guid)>(query.ToFormattableString())
            .ToDictionaryAsync(x => x.Item1, x => x.Item2, cancellationToken);

        var actorsByActorNameId = db.ChangeTracker
            .Entries<Actor>()
            .Where(x => x.Entity.ActorNameEntityId.HasValue)
            .GroupBy(x => x.Entity.ActorNameEntityId!.Value)
            .ToDictionary(x => x.Key, x => x.ToList());

        foreach (var actorName in concreteActorNames)
        {
            if (!result.TryGetValue(actorName.Id, out var actualId) || actorName.Id == actualId) continue;
            actorName.Id = actualId;
            db.Entry(actorName).State = EntityState.Unchanged;
            if (!actorsByActorNameId.TryGetValue(actorName.Id, out var actors)) continue;
            foreach (var actorEntry in actors)
            {
                actorEntry.Entity.ActorNameEntityId = actualId;
                actorEntry.State = EntityState.Modified;
            }
        }
    }
}
