using System.Text.Json;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.CustomSql;

internal sealed class DialogRepository(DialogDbContext db, IClock clock) : IDialogRepository
{
    private readonly DialogDbContext _db = db ?? throw new ArgumentNullException(nameof(db));
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));

    public IQueryable<DialogEntity> GetRelevantGuids(SearchDialogQuery2 query, DialogSearchAuthorizationResult authorizedResources)
    {
        var now = _clock.UtcNowOffset;
        var tagSearch = query.Search?.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
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
        var queryBuilder = new FormattableStringBuilder()
            //language=PostgreSQL
            .Append(
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
                 ,searchTag AS (
                     SELECT dst."DialogId"
                     FROM "DialogSearchTag" dst
                     WHERE dst."Value" = ANY({tagSearch}::text[])
                     GROUP BY dst."DialogId"
                     HAVING COUNT(DISTINCT dst."Value") = cardinality({tagSearch}::text[]))
                 ,systemLabel AS (
                     SELECT dec."DialogId"
                     FROM "DialogEndUserContext" dec
                     INNER JOIN "DialogEndUserContextSystemLabel" dsl on dsl."DialogEndUserContextId" = dec."Id"
                     WHERE dsl."SystemLabelId" = ANY({query.SystemLabel}::int[])
                     GROUP BY dec."DialogId"
                     HAVING COUNT(DISTINCT dsl."SystemLabelId") = cardinality({query.SystemLabel}::int[])
                 )
                 SELECT d.*
                 FROM "Dialog" d
                 INNER JOIN accessibleDialogs a ON d."Id" = a."Id"
                 LEFT JOIN "DialogSearch" ds ON ds."DialogId" = d."Id"
                """)
            // Only join if we need to filter on system labels or tags
            .AppendIf(query.SystemLabel is not null, """ INNER JOIN systemLabel l on l."DialogId" = d."Id" """)
            .AppendIf(tagSearch is not null, """ INNER JOIN searchTag t on t."DialogId" = d."Id" """)
            .Append(
                $"""
                WHERE d."Deleted" = false
                    AND (d."VisibleFrom" IS NULL or d."VisibleFrom" < {now})
                    AND (d."ExpiresAt" IS NULL or d."ExpiresAt" > {now})
                """)
            .AppendIf(query.Org is not null, $""" AND d."Org" = ANY({query.Org}) """)
            .AppendIf(query.ServiceResource is not null, $""" AND d."ServiceResource" = ANY({query.ServiceResource}) """)
            .AppendIf(query.Party is not null, $""" AND d."Party" = ANY({query.Party}) """)
            .AppendIf(query.ExtendedStatus is not null, $""" AND d."ExtendedStatus" = ANY({query.ExtendedStatus}) """)
            .AppendIf(query.ExternalReference is not null, $""" AND d."ExternalReference" = {query.ExternalReference} """)
            .AppendIf(query.Status is not null, $""" AND d."StatusId" = ANY({query.Status}) """)
            .AppendIf(query.CreatedAfter is not null, $""" AND {query.CreatedAfter} <= d."CreatedAt" """)
            .AppendIf(query.CreatedBefore is not null, $""" AND d."CreatedAt" <= {query.CreatedBefore} """)
            .AppendIf(query.UpdatedAfter is not null, $""" AND {query.UpdatedAfter} <= d."UpdatedAt" """)
            .AppendIf(query.UpdatedBefore is not null, $""" AND d."UpdatedAt" <= {query.UpdatedBefore} """)
            .AppendIf(query.ContentUpdatedAfter is not null, $""" AND {query.ContentUpdatedAfter} <= d."ContentUpdatedAt" """)
            .AppendIf(query.ContentUpdatedBefore is not null, $""" AND d."ContentUpdatedAt" <= {query.ContentUpdatedBefore} """)
            .AppendIf(query.DueAfter is not null, $""" AND {query.DueAfter} <= d."DueAt" """)
            .AppendIf(query.DueBefore is not null, $""" AND d."DueAt" <= {query.DueBefore} """)
            .AppendIf(query.Process is not null, $""" AND d."Process" = {query.Process} """)
            .AppendIf(query.ExcludeApiOnly is not null, $""" AND ({query.ExcludeApiOnly} = false OR {query.ExcludeApiOnly} = true AND d."IsApiOnly" = false) """)
            .AppendIf(query.Search is not null, $""" AND ds.search @@ websearch_to_tsquery({query.Search}) """);

        return _db.Dialogs.FromSql(queryBuilder.ToFormattableString());
    }
}
