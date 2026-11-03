using System.Text.Json;
using Dapper;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Library.Entity.Abstractions.Features.Creatable;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

internal sealed class ActorNameRepository(DialogDbContext db, NpgsqlDataSource dataSource, ITransactionTime transactionTime) : IActorNameRepository
{
    public Task EnsureActorName(ActorName actorName, CancellationToken cancellationToken) =>
        EnsureActorNames([actorName], cancellationToken);

    public async Task EnsureActorNames(IReadOnlyCollection<ActorName> actorNames,
        CancellationToken cancellationToken)
    {
        var consolidatedActorNames = ConsolidateActorNameInstances(actorNames);

        var maps = await GetOrCreateActorNames(consolidatedActorNames, cancellationToken);

        var inputIdByActorNames = consolidatedActorNames.Join(maps,
            actorName => actorName.Id,
            map => map.InputActorNameId,
            (actorName, mapping) => (actorName, mapping.ActualActorNameId));

        foreach (var (actorName, actualActorNameId) in inputIdByActorNames)
        {
            var entry = db.Entry(actorName);
            entry.State = EntityState.Detached;
            actorName.Id = actualActorNameId;
            entry.State = EntityState.Unchanged;
        }
    }

    private List<ActorName> ConsolidateActorNameInstances(IReadOnlyCollection<ActorName> actorNames)
    {
        db.ChangeTracker.DetectChanges();

        var mainAndDiscardActorNames = actorNames
            .GroupBy(x => new { x.ActorId, x.Name })
            .Select(x => (mainActorName: x.First(), discardActorNames: x.Skip(1).ToList()))
            .ToList();

        foreach (var (mainActorName, discardActorNames) in mainAndDiscardActorNames)
        {
            mainActorName.EnsureId();
            mainActorName.Create(transactionTime.Value);

            foreach (var actor in discardActorNames.SelectMany(x => x.ActorEntities))
            {
                actor.ActorNameEntity = mainActorName;
            }

            foreach (var discardInstance in discardActorNames)
            {
                db.Entry(discardInstance).State = EntityState.Detached;
            }
        }

        return mainAndDiscardActorNames
            .Select(x => x.mainActorName)
            .ToList();
    }

    private async Task<IReadOnlyCollection<ActorNameIdMapping>> GetOrCreateActorNames(
        IReadOnlyCollection<ActorName> actorNames,
        CancellationToken cancellationToken)
    {
        // lang=PostgreSQL
        const string sql =
            """
            WITH input_rows AS (
                SELECT *
                FROM jsonb_to_recordset(@inputRows::jsonb)
                     AS t("Id" uuid, "ActorId" text, "Name" text, "CreatedAt" timestamptz)
            )
            , inserted_actor_names AS (
                INSERT INTO "ActorName" ("Id", "ActorId", "Name", "CreatedAt")
                    SELECT "Id", "ActorId", "Name", "CreatedAt" FROM input_rows
                    ORDER BY "ActorId", "Name"
                ON CONFLICT ("ActorId", "Name") DO NOTHING
                RETURNING "Id", "ActorId", "Name", "CreatedAt"
            )
            , inserted_or_preexisting_actor_names AS (
                SELECT "Id", "ActorId", "Name", "CreatedAt" FROM inserted_actor_names
                UNION ALL
                SELECT an."Id", an."ActorId", an."Name", an."CreatedAt"
                FROM input_rows i
                JOIN "ActorName" an
                  ON an."ActorId" = i."ActorId"
                  AND an."Name" IS NOT DISTINCT FROM i."Name"
            )
            , concurrently_inserted_actor_names AS (
                INSERT INTO "ActorName" AS an ("Id", "ActorId", "Name", "CreatedAt")
                    SELECT i."Id", i."ActorId", i."Name", i."CreatedAt"
                    FROM input_rows i
                    LEFT JOIN inserted_or_preexisting_actor_names ins
                      ON ins."ActorId" IS NOT DISTINCT FROM i."ActorId"
                      AND ins."Name"    IS NOT DISTINCT FROM i."Name"
                    WHERE ins."Id" IS NULL
                    ORDER BY i."ActorId", i."Name"
                ON CONFLICT ("ActorId", "Name") DO UPDATE SET "Name" = an."Name"
                RETURNING "Id", "ActorId", "Name", "CreatedAt"
            )
            , resolved AS (
                SELECT * FROM inserted_or_preexisting_actor_names
                UNION ALL
                SELECT * FROM concurrently_inserted_actor_names
            )
            SELECT i."Id"          AS "InputActorNameId",
                   r."Id"          AS "ActualActorNameId"
            FROM input_rows i
            JOIN resolved r
              ON r."ActorId" IS NOT DISTINCT FROM i."ActorId"
             AND r."Name"    IS NOT DISTINCT FROM i."Name";
            """;

        var inputRows = JsonSerializer.Serialize(actorNames
            .Select(x => new { x.Id, x.ActorId, x.Name, x.CreatedAt }));

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var maps = await connection.QueryAsync<ActorNameIdMapping>(
            new CommandDefinition(sql, new { inputRows }, cancellationToken: cancellationToken));

        return maps.ToList();
    }

    private sealed record ActorNameIdMapping(Guid InputActorNameId, Guid ActualActorNameId);
}
