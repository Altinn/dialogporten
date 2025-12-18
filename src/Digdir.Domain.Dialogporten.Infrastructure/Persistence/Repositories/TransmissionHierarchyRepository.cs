using System.Collections.ObjectModel;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

internal sealed class TransmissionHierarchyRepository(DialogDbContext dbContext) : ITransmissionHierarchyRepository
{
    public async Task<IReadOnlyCollection<TransmissionHierarchyNode>> GetHierarchyNodes(
        Guid dialogId,
        IReadOnlyCollection<Guid> startIds,
        CancellationToken cancellationToken)
    {
        if (startIds.Count == 0)
        {
            return Array.Empty<TransmissionHierarchyNode>();
        }

        var idsArray = startIds.ToArray();

        const string sql = """
            WITH RECURSIVE ancestors AS (
                SELECT "Id", "RelatedTransmissionId" AS "ParentId"
                FROM "DialogTransmission"
                WHERE "DialogId" = {0} AND "Id" = ANY({1})
                UNION ALL
                SELECT parent."Id", parent."RelatedTransmissionId" AS "ParentId"
                FROM "DialogTransmission" parent
                INNER JOIN ancestors a ON parent."Id" = a."ParentId"
                WHERE parent."DialogId" = {0}
            ),
            hierarchy AS (
                SELECT DISTINCT "Id", "ParentId"
                FROM ancestors
                WHERE "ParentId" IS NULL
                UNION ALL
                SELECT child."Id", child."RelatedTransmissionId" AS "ParentId"
                FROM "DialogTransmission" child
                INNER JOIN hierarchy h ON child."RelatedTransmissionId" = h."Id"
                WHERE child."DialogId" = {0}
            )
            SELECT "Id", "ParentId"
            FROM hierarchy;
            """;

        var nodes = await dbContext.Database
            .SqlQueryRaw<TransmissionHierarchyNode>(sql, dialogId, idsArray)
            .ToListAsync(cancellationToken);

        return new ReadOnlyCollection<TransmissionHierarchyNode>(nodes);
    }
}
