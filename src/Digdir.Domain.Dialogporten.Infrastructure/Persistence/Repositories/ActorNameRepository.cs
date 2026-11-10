using System.Diagnostics;
using System.Text.Json;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Library.Entity.Abstractions.Features.Creatable;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

internal sealed class ActorNameRepository(
    DialogDbContext db,
    ITransactionTime transactionTime) : IActorNameRepository
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
            if (mapping.Created)
            {
                actorNameEntry.Entity.TryRequestActorNameResync("Newly created actor name");
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
                  SELECT t.*,
                         dense_rank() OVER (ORDER BY t."ActorId", t."Name") AS grp
                  FROM jsonb_to_recordset({inputRows}::jsonb)
                       AS t("Id" uuid, "ActorId" text, "Name" text, "CreatedAt" timestamptz)
                  WHERE NOT (t."ActorId" IS NULL AND t."Name" IS NULL)
              )
              , keys AS (                       -- one row per distinct (ActorId, Name)
                  SELECT DISTINCT ON (grp) grp, "Id", "ActorId", "Name", "CreatedAt"
                  FROM input_rows
                  ORDER BY grp, "CreatedAt", "Id"   -- earliest row wins; Id breaks exact ties
              )
              , preexisting AS (
                  SELECT k.grp, an."Id", an."CreatedAt"
                  FROM keys k
                  CROSS JOIN LATERAL (
                      -- ActorId and Name both set
                      SELECT an."Id", an."CreatedAt" FROM "ActorName" an
                      WHERE an."ActorId" = k."ActorId" AND an."Name" = k."Name"
                      UNION ALL
                      -- ActorId is null
                      SELECT an."Id", an."CreatedAt" FROM "ActorName" an
                      WHERE k."ActorId" IS NULL AND an."ActorId" IS NULL AND an."Name" = k."Name"
                      UNION ALL
                      -- Name is null
                      SELECT an."Id", an."CreatedAt" FROM "ActorName" an
                      WHERE k."Name" IS NULL AND an."ActorId" = k."ActorId" AND an."Name" IS NULL
                  ) an
              )
              , inserted AS (
                  INSERT INTO "ActorName" ("Id", "ActorId", "Name", "CreatedAt")
                  SELECT k."Id", k."ActorId", k."Name", k."CreatedAt"
                  FROM keys k
                  WHERE NOT EXISTS (SELECT 1 FROM preexisting p WHERE p.grp = k.grp)
                  ORDER BY k.grp
                  ON CONFLICT ("ActorId", "Name") DO NOTHING
                  RETURNING "Id"
              )
              , concurrent AS ( -- rare: committed by another tx after our snapshot
                  INSERT INTO "ActorName" AS an ("Id", "ActorId", "Name", "CreatedAt")
                  SELECT k."Id", k."ActorId", k."Name", k."CreatedAt"
                  FROM keys k
                  WHERE NOT EXISTS (SELECT 1 FROM preexisting p WHERE p.grp = k.grp)
                    AND NOT EXISTS (SELECT 1 FROM inserted ins WHERE ins."Id" = k."Id")
                  ORDER BY k.grp
                  ON CONFLICT ("ActorId", "Name") DO UPDATE SET "Name" = an."Name"
                  RETURNING "Id", "ActorId", "Name", "CreatedAt"
              )
              , resolved AS (
                  SELECT grp, "Id", "CreatedAt" FROM preexisting
                  UNION ALL
                  SELECT k.grp, k."Id", k."CreatedAt"
                  FROM keys k JOIN inserted ins ON ins."Id" = k."Id"
                  UNION ALL
                  SELECT k.grp, c."Id", c."CreatedAt"
                  FROM keys k
                  JOIN concurrent c
                    ON c."ActorId" IS NOT DISTINCT FROM k."ActorId"
                   AND c."Name"    IS NOT DISTINCT FROM k."Name"
              )
              SELECT k."Id"          AS "InputActorNameId",
                     r."Id"          AS "ActualActorNameId",
                     r."CreatedAt"   AS "ActualCreatedAt",
                     k."Id" = r."Id" AS "Created"
              FROM keys k
              JOIN resolved r USING (grp);
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
