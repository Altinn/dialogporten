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
                (
                    SELECT "Id", "RelatedTransmissionId" AS "ParentId"
                    FROM "DialogTransmission"
                    WHERE "DialogId" = {0} AND "RelatedTransmissionId" = ANY({1})
                )
                UNION
                (
                    SELECT "Id", "RelatedTransmissionId" AS "ParentId"
                    FROM "DialogTransmission"
                    WHERE "DialogId" = {0} AND "Id" = ANY({1})
                )
                UNION ALL
                SELECT parent."Id", parent."RelatedTransmissionId" AS "ParentId"
                FROM ancestors a
                INNER JOIN "DialogTransmission" parent ON parent."Id" = a."ParentId"
                WHERE parent."DialogId" = {0}
            )
            SELECT DISTINCT "Id", "ParentId"
            FROM ancestors;
            """;

        var nodes = await dbContext.Database
            .SqlQueryRaw<TransmissionHierarchyNode>(sql, dialogId, idsArray)
            .ToListAsync(cancellationToken);

        return new ReadOnlyCollection<TransmissionHierarchyNode>(nodes);
    }
}
