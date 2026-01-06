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
        if (startIds.Count == 0) return [];

        var idsArray = startIds.ToArray();

        // language=SQL
        const string sql = """
            WITH RECURSIVE ancestors AS (
                    SELECT dt."Id", dt."RelatedTransmissionId" AS "ParentId"
                    FROM "DialogTransmission" dt
                    WHERE dt."Id" = ANY ({1}) AND dt."DialogId" = {0}
                    UNION
                    SELECT parent."Id", parent."RelatedTransmissionId"
                    FROM ancestors a
                    INNER JOIN "DialogTransmission" parent ON parent."Id" = a."ParentId"
                    WHERE parent."DialogId" = {0}
                ),
                siblings AS (
                    SELECT "Id", "RelatedTransmissionId" AS "ParentId"
                    FROM "DialogTransmission"
                    WHERE "RelatedTransmissionId" = ANY ({1}) AND "DialogId" = {0}
                )
            SELECT "Id", "ParentId"
            FROM ancestors
            UNION
            SELECT "Id", "ParentId"
            FROM siblings;
            """;

        var nodes = await dbContext.Database
            .SqlQueryRaw<TransmissionHierarchyNode>(sql, dialogId, idsArray)
            .ToListAsync(cancellationToken);

        return new ReadOnlyCollection<TransmissionHierarchyNode>(nodes);
    }
}
