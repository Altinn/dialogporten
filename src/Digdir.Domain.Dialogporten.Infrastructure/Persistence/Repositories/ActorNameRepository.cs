using System.Text.Json;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Library.Entity.Abstractions.Features.Creatable;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

internal sealed class ActorNameRepository(ITransactionTime transactionTime) : IActorNameRepository
{
    public async Task<ActorName> EnsureActorName(DbContext db, ActorName actorName, CancellationToken cancellationToken)
    {
        var names = await EnsureActorNames(db, [actorName], cancellationToken);
        return names.Single();
    }

    public async Task<List<ActorName>> EnsureActorNames(
        DbContext db,
        IReadOnlyCollection<ActorName> actorNames,
        CancellationToken cancellationToken)
    {
        var consolidatedActorNames = ConsolidateDuplicateActorNameInstances(db, actorNames);
        var maps = await GetOrCreateActorNames(db, consolidatedActorNames, cancellationToken);

        var inputIdByActorNames = consolidatedActorNames
            .Join(
                maps,
                actorName => actorName.Id,
                map => map.InputActorNameId,
                (actorName, mapping) => (actorName, mapping.ActualActorNameId))
            .ToList();

        foreach (var (actorName, actualActorNameId) in inputIdByActorNames)
        {
            var entry = db.Entry(actorName);

            entry.State = EntityState.Detached;
            actorName.Id = actualActorNameId;
            entry.OriginalValues.SetValues(actorName);
            entry.State = EntityState.Unchanged;
        }

        return inputIdByActorNames
            .Select(x => x.actorName)
            .ToList();
    }

    private List<ActorName> ConsolidateDuplicateActorNameInstances(DbContext db,
        IReadOnlyCollection<ActorName> actorNames)
    {
        var mainAndDiscardActorNames = db.ChangeTracker
            .Entries<ActorName>()
            .Concat(actorNames.Select(db.Entry))
            .DistinctBy(x => x.Entity)
            .GroupBy(x => (x.Entity.ActorId, x.Entity.Name))
            .IntersectBy(actorNames.Select(x => (x.ActorId, x.Name)), x => x.Key)
            .Select(x =>
            {
                var main = x.FirstOrDefault(e => e.State is EntityState.Unchanged or EntityState.Modified) ?? x.First();
                return (mainActorName: main, discardActorNames: x.Where(y => !ReferenceEquals(y, main)).ToList());
            })
            .ToList();

        foreach (var (mainActorName, duplicatedActorNames) in mainAndDiscardActorNames)
        {
            mainActorName.Entity.EnsureId();
            mainActorName.Entity.Create(transactionTime.Value);

            foreach (var actor in duplicatedActorNames.SelectMany(x => x.Entity.ActorEntities))
            {
                actor.ActorNameEntityId = null;
                actor.ActorNameEntity = mainActorName.Entity;
            }

            foreach (var discardInstance in duplicatedActorNames)
            {
                discardInstance.State = EntityState.Detached;
            }
        }

        return mainAndDiscardActorNames
            .Select(x => x.mainActorName.Entity)
            .ToList();
    }

    private static async Task<IReadOnlyCollection<ActorNameIdMapping>> GetOrCreateActorNames(
        DbContext db,
        IReadOnlyCollection<ActorName> actorNames,
        CancellationToken cancellationToken)
    {
        var actorNameById = actorNames.ToDictionary(_ => IdentifiableExtensions.CreateVersion7());

        var inputRows = JsonSerializer.Serialize(actorNameById
            .Select(x => new { Id = x.Key, x.Value.ActorId, x.Value.Name, x.Value.CreatedAt }));

        var mappings = await db.Database.SqlQuery<ActorNameIdMapping>(
            $"""
             WITH input_rows AS (
                 SELECT *
                 FROM jsonb_to_recordset({inputRows}::jsonb)
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
             """)
            .ToListAsync(cancellationToken);

        return mappings
            .Select(x => x with { InputActorNameId = actorNameById[x.InputActorNameId].Id })
            .ToList();
    }

    private sealed record ActorNameIdMapping(Guid InputActorNameId, Guid ActualActorNameId);
}
