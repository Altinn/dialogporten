using System.Diagnostics;
using System.Text.Json;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Library.Entity.Abstractions.Features.Creatable;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

internal sealed class ActorNameRepository(DialogDbContext db, ITransactionTime transactionTime) : IActorNameRepository
{
    public async Task<ActorName> EnsureActorName(ActorName actorName, CancellationToken cancellationToken)
    {
        var names = await EnsureActorNames([actorName], cancellationToken);
        return names.Single();
    }

    public async Task<List<ActorName>> EnsureActorNames(
        IReadOnlyCollection<ActorName> actorNames,
        CancellationToken cancellationToken)
    {
        var consolidatedActorNames = ConsolidateDuplicateActorNameInstances(actorNames);
        var maps = await GetOrCreateActorNames(consolidatedActorNames, cancellationToken);
        var actorNameIdTuples = consolidatedActorNames
            .Join(maps,
                actorName => actorName.Entity.Id,
                map => map.InputActorNameId,
                (actorName, mapping) => (actorName, mapping))
            .ToList();

        if (actorNameIdTuples.Count != consolidatedActorNames.Count)
        {
            throw new UnreachableException(
                "The number of input actor names does not match the " +
                "number of actual actor names returned from the database. " +
                "This indicates a bug in the code or a data integrity issue.");
        }

        foreach (var (actorNameEntry, mapping) in actorNameIdTuples)
        {
            actorNameEntry.State = EntityState.Detached;
            actorNameEntry.Entity.Id = mapping.ActualActorNameId;
            actorNameEntry.Entity.CreatedAt = mapping.ActualCreatedAt;
            actorNameEntry.OriginalValues.SetValues(actorNameEntry.Entity);
            actorNameEntry.State = EntityState.Unchanged;
            if (mapping.Created && actorNameEntry.Entity.Name is null)
            {
                actorNameEntry.Entity.RequestActorNameResync("Newly created actor name");
            }
        }

        return actorNameIdTuples
            .Select(x => x.actorName.Entity)
            .ToList();
    }

    private List<EntityEntry<ActorName>> ConsolidateDuplicateActorNameInstances(IReadOnlyCollection<ActorName> actorNames)
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
            .Select(x => x.mainActorName)
            .ToList();
    }

    private async Task<IReadOnlyCollection<ActorNameIdMapping>> GetOrCreateActorNames(
        IReadOnlyCollection<EntityEntry<ActorName>> actorNames,
        CancellationToken cancellationToken)
    {
        var actorNameById = actorNames
            .Select(x => x.Entity)
            .ToDictionary(_ => IdentifiableExtensions.CreateVersion7());

        var inputRows = JsonSerializer.Serialize(actorNameById
            .Select(x => new { Id = x.Key, x.Value.ActorId, x.Value.Name, x.Value.CreatedAt }));

        // lang=PostgreSQL
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
                   ON (an."ActorId" = i."ActorId" OR (an."ActorId" IS NULL AND i."ActorId" IS NULL))
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
                    r."Id"          AS "ActualActorNameId",
                    r."CreatedAt"   AS "ActualCreatedAt",
                    i."Id" = r."Id" AS "Created"
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

    private sealed record ActorNameIdMapping(
        Guid InputActorNameId,
        Guid ActualActorNameId,
        DateTimeOffset ActualCreatedAt,
        bool Created);
}
