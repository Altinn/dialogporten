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

internal sealed class ActorNameRepository(NpgsqlDataSource dataSource, ITransactionTime transactionTime) : IActorNameRepository
{
    /// <summary>
    /// Gets or creates the given <see cref="ActorName"/> rows in a single round trip, using a connection
    /// from the <see cref="NpgsqlDataSource"/> and Dapper. Does not touch the EF change tracker.
    /// </summary>
    /// <remarks>
    /// Runs outside any EF transaction, so created rows are committed independently of the caller's unit of work.
    /// Ids are assigned to inputs that lack one. See <see cref="EnsureActorNames"/> for how conflicts are resolved.
    /// </remarks>
    /// <param name="actorNames">The actor names to get or create. Enumerated multiple times, so it must be materialized.</param>
    /// <param name="cancellationToken">Token used to cancel the database operation.</param>
    /// <returns>
    /// A map from the input <see cref="ActorName.Id"/> to the id of the persisted row. The ids are equal when the
    /// input row was inserted, and differ when an equivalent row already existed.
    /// </returns>
    public async Task<IReadOnlyDictionary<Guid, Guid>> GetOrCreateActorNames(
        IReadOnlyCollection<ActorName> actorNames,
        CancellationToken cancellationToken)
    {
        foreach (var actorName in actorNames)
        {
            actorName.EnsureId();
            actorName.Create(transactionTime.Value);
        }

        var inputRows = JsonSerializer.Serialize(actorNames
            .Select(x => new { x.Id, x.ActorId, x.Name, x.CreatedAt }));

        // lang=PostgreSQL
        const string sql =
            """
            WITH input_rows AS (
                SELECT *
                FROM jsonb_to_recordset(@inputRows::jsonb)
                     AS t("Id" uuid, "ActorId" text, "Name" text, "CreatedAt" date)
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

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<ActorNameIdMapping>(
            new CommandDefinition(sql, new { inputRows }, cancellationToken: cancellationToken));

        return rows.ToDictionary(x => x.InputActorNameId, x => x.ActualActorNameId);
    }

    private sealed record ActorNameIdMapping(Guid InputActorNameId, Guid ActualActorNameId);
}
