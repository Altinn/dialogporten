using System.Text.Json;
using Dapper;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

internal sealed class ActorNameRepository(DialogDbContext db, NpgsqlDataSource dataSource, ITransactionTime transactionTime) : IActorNameRepository
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
        }

        var inputRows = JsonSerializer.Serialize(actorNames
            .Select(x => new { x.Id, x.ActorId, x.Name }));

        // lang=PostgreSQL
        const string sql =
            """
            WITH input_rows AS (
                SELECT *
                FROM jsonb_to_recordset(@inputRows::jsonb)
                     AS t("Id" uuid, "ActorId" text, "Name" text)
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
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<ActorNameIdMapping>(
            new CommandDefinition(sql, new { inputRows }, cancellationToken: cancellationToken));

        return rows.ToDictionary(x => x.InputActorNameId, x => x.ActualActorNameId);
    }

    private sealed record ActorNameIdMapping(Guid InputActorNameId, Guid ActualActorNameId);
}
