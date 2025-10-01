using System.Text.Json;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.CustomSql;

internal sealed class DialogRepository : IDialogRepository
{
    private readonly DialogDbContext _db;

    public DialogRepository(DialogDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<PaginatedList<Guid>> GetRelevantGuids(SearchDialogQuery query, DialogSearchAuthorizationResult authorizedResources, CancellationToken cancellationToken)
    {
        var systemLabels = query.SystemLabel;
        var now = DateTimeOffset.UtcNow;
        var org = query.Org;
        var serviceResource = query.ServiceResource;
        var party = query.Party;
        var extendedStatus = query.ExtendedStatus;
        var externalReference = query.ExternalReference;
        var status = query.Status;
        var createdAfter = query.CreatedAfter;
        var createdBefore = query.CreatedBefore;
        var updatedAfter = query.UpdatedAfter;
        var updatedBefore = query.UpdatedBefore;
        var contentUpdatedAfter = query.ContentUpdatedAfter;
        var contentUpdatedBefore = query.ContentUpdatedBefore;
        var dueAfter = query.DueAfter;
        var dueBefore = query.DueBefore;
        var process = query.Process;
        var excludeApiOnly = query.ExcludeApiOnly;
        var search = query.Search;
        var systemLabel = query.SystemLabel;

        var tagSearch = search?.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var partiesAndServices = JsonSerializer.Serialize(authorizedResources.ResourcesByParties
            .Select(x => (party: x.Key, services: x.Value))
            .GroupBy(x => x.services, new HashSetEqualityComparer<string>())
            .Select(x => new
            {
                parties = x.Select(k => k.party).ToArray(),
                services = x.Key.ToArray()
            }));

        // TODO: CREATE INDEX idx_dst_dialogid ON "DialogSearchTag"("DialogId");
        // TODO: CREATE INDEX indexNameHere ON "Dialog"("Party", "ServiceResource") INCLUDING ("Id");
        var queryBuilder = new FormattableStringBuilder();
        //language=PostgreSQL
        queryBuilder.Append(
                $"""
                 WITH partyResourceAccess AS (
                     SELECT DISTINCT s.service, p.party
                     FROM jsonb_to_recordset({partiesAndServices}::jsonb) AS x(parties text[], services text[])
                     CROSS JOIN LATERAL unnest(x.services) AS s(service)
                     CROSS JOIN LATERAL unnest(x.parties) AS p(party))
                 ,accessibleDialogs AS (
                     SELECT DISTINCT d."Id"
                     FROM "Dialog" d
                     INNER JOIN partyResourceAccess c ON d."ServiceResource" = c.service AND d."Party" = c.party)
                 ,systemLabelsByDialog AS (
                     SELECT d."Id", array_agg(dsl."SystemLabelId") labels
                     FROM "Dialog" d
                     INNER JOIN "DialogEndUserContext" dec on d."Id" = dec."DialogId"
                     INNER JOIN "DialogEndUserContextSystemLabel" dsl on dsl."DialogEndUserContextId" = dec."Id"
                     WHERE {systemLabels}::int[] is not null AND dsl."SystemLabelId" = ANY({systemLabels}::int[])
                     GROUP BY d."Id")
                 ,searchTagsByDialog AS (
                     SELECT d."Id", array_agg(dst."Value") tags
                     FROM "Dialog" d
                     INNER JOIN "DialogSearchTag" dst on d."Id" = dst."DialogId"
                     WHERE {tagSearch}::text[] is not null AND dst."Value" = ANY({tagSearch}::text[])
                     GROUP BY d."Id"
                 )
                 SELECT d."Id"
                 FROM "Dialog" d
                 INNER JOIN accessibleDialogs a ON d."Id" = a."Id"
                 LEFT JOIN "DialogSearch" ds ON ds."DialogId" = d."Id"
                 LEFT JOIN searchTagsByDialog t on d."Id" = t."Id"
                 LEFT JOIN systemLabelsByDialog l on l."Id" = d."Id"
                 WHERE d."Deleted" = false
                     AND (d."VisibleFrom" IS NULL or d."VisibleFrom" < {now})
                     AND (d."ExpiresAt" IS NULL or d."ExpiresAt" > {now})
                 """)
            .AppendIf(org is not null, $""" AND d."Org" = ANY({org}) """)
            .AppendIf(serviceResource is not null, $""" AND d."ServiceResource" = ANY({serviceResource}) """)
            .AppendIf(party is not null, $""" AND d."Party" = ANY({party}) """)
            .AppendIf(extendedStatus is not null, $""" AND d."ExtendedStatus" = ANY({extendedStatus}) """)
            .AppendIf(externalReference is not null, $""" AND d."ExternalReference" = {externalReference} """)
            .AppendIf(status is not null, $""" AND d."StatusId" = ANY({status}) """)
            .AppendIf(createdAfter is not null, $""" AND {createdAfter} <= d."CreatedAt" """)
            .AppendIf(createdBefore is not null, $""" AND d."CreatedAt" <= {createdBefore} """)
            .AppendIf(updatedAfter is not null, $""" AND {updatedAfter} <= d."UpdatedAt" """)
            .AppendIf(updatedBefore is not null, $""" AND d."UpdatedAt" <= {updatedBefore} """)
            .AppendIf(contentUpdatedAfter is not null, $""" AND {contentUpdatedAfter} <= d."ContentUpdatedAt" """)
            .AppendIf(contentUpdatedBefore is not null, $""" AND d."ContentUpdatedAt" <= {contentUpdatedBefore} """)
            .AppendIf(dueAfter is not null, $""" AND {dueAfter} <= d."DueAt" """)
            .AppendIf(dueBefore is not null, $""" AND d."DueAt" <= {dueBefore} """)
            .AppendIf(process is not null, $""" AND d."Process" = {process} """)
            .AppendIf(excludeApiOnly is not null, $""" AND ({excludeApiOnly} = false OR {excludeApiOnly} = true AND d."IsApiOnly" = false) """)
            .AppendIf(search is not null, $""" AND ds.search @@ websearch_to_tsquery({search}) """)
            .AppendIf(search is not null, $""" AND {tagSearch} <@ t.tags """)
            .AppendIf(systemLabel is not null, $""" AND {systemLabel} <@ l.labels """)
            .ApplyPaginationCondition(query.OrderBy.DefaultIfNull(), query.ContinuationToken)
            .ApplyPaginationOrder(query.OrderBy.DefaultIfNull())
            .ApplyPaginationLimit(query.Limit!.Value + 1);

        var items = await _db.Database
            .SqlQuery<Guid>(queryBuilder.ToFormattableString())
            .ToListAsync(cancellationToken);

        return new PaginatedList<Guid>(items, false, null, query.OrderBy.DefaultIfNull().GetOrderString());
    }
}
