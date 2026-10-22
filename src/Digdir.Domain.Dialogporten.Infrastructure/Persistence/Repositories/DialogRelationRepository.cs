using System.Globalization;
using System.Text;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Relations;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

internal sealed class DialogRelationRepository(DialogDbContext dbContext) : IDialogRelationRepository
{
    private const int Parent = (int)DialogRelationType.Values.Parent;
    private const int Child = (int)DialogRelationType.Values.Child;

    public async Task<List<DialogRelationView>> GetRelations(
        Guid dialogId,
        DialogRelationType.Values? type,
        Guid? after,
        int limit,
        CancellationToken cancellationToken)
    {
        // Each branch is an index range scan (PK and the reverse index), already ordered by the other dialog's id,
        // so PostgreSQL can merge them (Merge Append) under the LIMIT. The second branch inverts the stored type.
        // language=SQL
        const string sql = """
            SELECT x."RelatedDialogId", x."TypeId"
            FROM (
                (SELECT r."RelatedDialogId", r."TypeId"
                 FROM "DialogRelation" r
                 WHERE r."DialogId" = {0}
                   AND r."RelatedDialogId" > {2}
                   AND ({1} = 0 OR r."TypeId" = {1})
                 ORDER BY r."RelatedDialogId"
                 LIMIT {3})
                UNION ALL
                (SELECT r."DialogId",
                        CASE r."TypeId" WHEN 1 THEN 2 WHEN 2 THEN 1 ELSE r."TypeId" END
                 FROM "DialogRelation" r
                 WHERE r."RelatedDialogId" = {0}
                   AND r."DialogId" > {2}
                   AND ({1} = 0 OR r."TypeId" = CASE {1} WHEN 1 THEN 2 WHEN 2 THEN 1 ELSE {1} END)
                 ORDER BY r."DialogId"
                 LIMIT {3})
            ) x
            ORDER BY x."RelatedDialogId"
            LIMIT {3}
            """;

        var rows = await dbContext.Database
            // No type filter is 0, and no keyset is the nil uuid, which sorts before every other uuid.
            .SqlQueryRaw<RelationRow>(sql, dialogId, (int?)type ?? 0, after ?? Guid.Empty, limit)
            .ToListAsync(cancellationToken);

        return rows.Select(x => new DialogRelationView(x.RelatedDialogId, (DialogRelationType.Values)x.TypeId)).ToList();
    }

    public async Task<List<EndUserRelatedDialog>> SearchAsEndUser(EndUserDialogRelationsQuery query, CancellationToken cancellationToken)
    {
        var parameters = new List<object>
        {
            query.DialogId,                                         // {0}
            query.Now,                                              // {1}
            (int?)query.Type ?? 0,                                  // {2}
            query.ExcludeApiOnly,                                   // {3}
            query.After ?? Guid.Empty,                              // {4}
            query.Limit,                                            // {5}
            query.Parties.ToArray(),                                // {6}
            query.ServiceResources.ToArray(),                       // {7}
            query.Statuses.Select(x => (int)x).ToArray(),           // {8}
            query.AuthorizedResources.DialogIds.ToArray()           // {9}
        };

        // The same party/resource grouping as the search prefilter (PrefilterAuthorizedDialogs).
        var authorization = new StringBuilder("""d."Id" = ANY({9})""");
        foreach (var group in query.AuthorizedResources.ResourcesByParties
                     .GroupBy(kv => kv.Value, new HashSetEqualityComparer<string>()))
        {
            authorization.Append(CultureInfo.InvariantCulture,
                $$""" OR (d."Party" = ANY({{{parameters.Count}}}) AND d."ServiceResource" = ANY({{{parameters.Count + 1}}}))""");
            parameters.Add(group.Select(kv => kv.Key).ToArray());
            parameters.Add(group.Key.ToArray());
        }

        // Both relation branches are bounded by the relation cap, so the filters run on at most that many dialogs.
        // language=SQL
        var sql = $$"""
            SELECT x."RelatedDialogId", x."TypeId", d."Org", d."ServiceResource", d."ServiceResourceType", d."Party",
                   d."StatusId", d."ExtendedStatus", d."CreatedAt", d."UpdatedAt", d."ContentUpdatedAt", d."DueAt",
                   d."IsApiOnly", d."HasRelations"
            FROM (
                SELECT r."RelatedDialogId", r."TypeId"
                FROM "DialogRelation" r
                WHERE r."DialogId" = {0} AND r."RelatedDialogId" > {4}
                UNION ALL
                SELECT r."DialogId", CASE r."TypeId" WHEN 1 THEN 2 WHEN 2 THEN 1 ELSE r."TypeId" END
                FROM "DialogRelation" r
                WHERE r."RelatedDialogId" = {0} AND r."DialogId" > {4}
            ) x
            JOIN "Dialog" d ON d."Id" = x."RelatedDialogId"
            WHERE d."Deleted" = false
              AND (d."VisibleFrom" IS NULL OR d."VisibleFrom" <= {1})
              AND (d."ExpiresAt" IS NULL OR d."ExpiresAt" > {1})
              AND ({3} = false OR d."IsApiOnly" = false)
              AND ({2} = 0 OR x."TypeId" = {2})
              AND (cardinality({6}) = 0 OR d."Party" = ANY({6}))
              AND (cardinality({7}) = 0 OR d."ServiceResource" = ANY({7}))
              AND (cardinality({8}) = 0 OR d."StatusId" = ANY({8}))
              AND ({{authorization}})
            ORDER BY x."RelatedDialogId"
            LIMIT {5}
            """;

        return await dbContext.Database
            .SqlQueryRaw<EndUserRelatedDialog>(sql, parameters.ToArray())
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountRelations(Guid dialogId, int cap, CancellationToken cancellationToken)
    {
        // language=SQL
        const string sql = """
            SELECT count(*)::int AS "Value" FROM (
                (SELECT 1 FROM "DialogRelation" WHERE "DialogId" = {0} LIMIT {1})
                UNION ALL
                (SELECT 1 FROM "DialogRelation" WHERE "RelatedDialogId" = {0} LIMIT {1})
            ) x
            """;

        var count = await dbContext.Database
            .SqlQueryRaw<int>(sql, dialogId, cap)
            .SingleAsync(cancellationToken);

        return Math.Min(count, cap);
    }

    public async Task<List<Guid>> GetAncestors(Guid dialogId, int maxSteps, CancellationToken cancellationToken)
    {
        // The parent of X is stored either as (X, P, Parent) or as (P, X, Child).
        // language=SQL
        var sql = $$"""
            WITH RECURSIVE ancestors("Id", "Depth") AS (
                SELECT {0}::uuid, 0
                UNION ALL
                SELECT p."Id", a."Depth" + 1
                FROM ancestors a
                CROSS JOIN LATERAL (
                    SELECT r."RelatedDialogId" AS "Id" FROM "DialogRelation" r WHERE r."DialogId" = a."Id" AND r."TypeId" = {{Parent}}
                    UNION ALL
                    SELECT r."DialogId" FROM "DialogRelation" r WHERE r."RelatedDialogId" = a."Id" AND r."TypeId" = {{Child}}
                ) p
                WHERE a."Depth" < {1}
            )
            SELECT "Id" AS "Value" FROM ancestors WHERE "Depth" > 0 ORDER BY "Depth"
            """;

        return await dbContext.Database
            .SqlQueryRaw<Guid>(sql, dialogId, maxSteps)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasDescendantsAt(Guid dialogId, int generations, CancellationToken cancellationToken)
    {
        if (generations <= 0)
        {
            return true;
        }

        // The children of X are stored either as (X, C, Child) or as (C, X, Parent).
        // language=SQL
        var sql = $$"""
            WITH RECURSIVE descendants("Id", "Depth") AS (
                SELECT {0}::uuid, 0
                UNION ALL
                SELECT c."Id", d."Depth" + 1
                FROM descendants d
                CROSS JOIN LATERAL (
                    SELECT r."RelatedDialogId" AS "Id" FROM "DialogRelation" r WHERE r."DialogId" = d."Id" AND r."TypeId" = {{Child}}
                    UNION ALL
                    SELECT r."DialogId" FROM "DialogRelation" r WHERE r."RelatedDialogId" = d."Id" AND r."TypeId" = {{Parent}}
                ) c
                WHERE d."Depth" < {1}
            )
            SELECT EXISTS (SELECT 1 FROM descendants WHERE "Depth" = {1}) AS "Value"
            """;

        return await dbContext.Database
            .SqlQueryRaw<bool>(sql, dialogId, generations)
            .SingleAsync(cancellationToken);
    }

    public async Task<HashSet<Guid>> LockDialogs(IEnumerable<Guid> dialogIds, CancellationToken cancellationToken)
    {
        var ids = dialogIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return [];
        }

        // language=SQL
        const string sql = """
            SELECT "Id", "Deleted"
            FROM "Dialog"
            WHERE "Id" = ANY({0})
            ORDER BY "Id"
            FOR UPDATE
            """;

        var rows = await dbContext.Database
            .SqlQueryRaw<LockedDialogRow>(sql, ids)
            .ToListAsync(cancellationToken);

        return rows.Where(x => !x.Deleted).Select(x => x.Id).ToHashSet();
    }

    public Task SetHasRelations(Guid dialogId, bool hasRelations, CancellationToken cancellationToken) =>
        dbContext.Database.ExecuteSqlRawAsync(
            """UPDATE "Dialog" SET "HasRelations" = {1} WHERE "Id" = {0} AND "HasRelations" <> {1}""",
            [dialogId, hasRelations],
            cancellationToken);

    public Task ClearHasRelationsWhereNoneLeft(IEnumerable<Guid> dialogIds, Guid? ignoredDialogId, CancellationToken cancellationToken)
    {
        var ids = dialogIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return Task.CompletedTask;
        }

        // language=SQL
        const string sql = """
            UPDATE "Dialog" d
            SET "HasRelations" = false
            WHERE d."Id" = ANY({0})
              AND d."HasRelations"
              AND NOT EXISTS (SELECT 1 FROM "DialogRelation" r
                              WHERE r."DialogId" = d."Id" AND r."RelatedDialogId" <> {1})
              AND NOT EXISTS (SELECT 1 FROM "DialogRelation" r
                              WHERE r."RelatedDialogId" = d."Id" AND r."DialogId" <> {1})
            """;

        return dbContext.Database.ExecuteSqlRawAsync(
            sql,
            [ids, ignoredDialogId ?? Guid.Empty],
            cancellationToken);
    }

    private sealed record RelationRow(Guid RelatedDialogId, int TypeId);

    private sealed record LockedDialogRow(Guid Id, bool Deleted);
}
