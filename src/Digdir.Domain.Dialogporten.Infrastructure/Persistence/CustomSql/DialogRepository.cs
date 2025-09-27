using Digdir.Domain.Dialogporten.Application.Common.Extensions;
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

    public async Task<List<Guid>> GetRelevantGuids(SearchDialogQuery query, DialogSearchAuthorizationResult authorizedResources, CancellationToken cancellationToken)
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

        // Group parties that have the same resources
        var groupedResult = authorizedResources.ResourcesByParties
            .Select(x => (party: x.Key, services: x.Value))
            .GroupBy(x => x.services, new HashSetEqualityComparer<string>())
            .Select(x => new
            {
                parties = x.Select(k => k.party).ToArray(),
                services = x.Key.ToArray()
            })
            .ToArray();

        var partiesAndServices = System.Text.Json.JsonSerializer.Serialize(groupedResult);
        var result = await _db.Database.SqlQuery<Guid>(
            // Language=SQL
            $"""
            WITH partyResourceAccess AS (
                SELECT DISTINCT s.service, p.party
                FROM jsonb_to_recordset({partiesAndServices}::jsonb) AS x(parties text[], services text[])
                CROSS JOIN LATERAL unnest(x.services) AS s(service)
                CROSS JOIN LATERAL unnest(x.parties) AS p(party))
            ,accessibleDialogs AS (
                SELECT DISTINCT d."Id"
                FROM "Dialog" d
                INNER JOIN partyResourceAccess c
                ON d."ServiceResource" = c.service AND d."Party" = c.party)
            ,systemLabelsByDialog AS (
                SELECT d."Id", array_agg(dsl."SystemLabelId") labels
                FROM "Dialog" d
                INNER JOIN "DialogEndUserContext" dec on d."Id" = dec."DialogId"
                INNER JOIN "DialogEndUserContextSystemLabel" dsl on dsl."DialogEndUserContextId" = dec."Id"
                WHERE {systemLabels}::int[] is not null AND dsl."SystemLabelId" = ANY({systemLabels}::int[])
                GROUP BY d."Id"
            )
            SELECT d."Id"
            FROM "Dialog" d
            INNER JOIN accessibleDialogs a ON d."Id" = a."Id"
            LEFT JOIN "DialogSearch" ds ON ds."DialogId" = d."Id"
            --LEFT JOIN "DialogSearchTag" dst on d."Id" = dst."DialogId" -- TODO: No many relationship here! 
            LEFT JOIN systemLabelsByDialog l on l."Id" = d."Id"
            WHERE d."Deleted" = false
                AND (d."VisibleFrom" IS NULL or d."VisibleFrom" < {now}::timestamptz)
                AND (d."ExpiresAt" IS NULL or d."ExpiresAt" > {now}::timestamptz)
                AND ({org}::text[] IS NULL OR d."Org" = ANY({org}::text[]))
                AND ({serviceResource}::text[] IS NULL OR d."ServiceResource" = ANY({serviceResource}::text[]))
                AND ({party}::text[] IS NULL OR d."Party" = ANY({party}::text[]))
                AND ({extendedStatus}::text[] IS NULL OR d."ExtendedStatus" = ANY({extendedStatus}::text[]))
                AND ({externalReference}::text IS NULL OR d."ExternalReference" = {externalReference}::text)
                AND ({status}::int[] IS NULL OR d."StatusId" = ANY({status}::int[]))
                AND ({createdAfter}::timestamptz IS NULL OR {createdAfter}::timestamptz <= d."CreatedAt")
                AND ({createdBefore}::timestamptz IS NULL OR d."CreatedAt" <= {createdBefore}::timestamptz)
                AND ({updatedAfter}::timestamptz IS NULL OR {updatedAfter}::timestamptz <= d."UpdatedAt")
                AND ({updatedBefore}::timestamptz IS NULL OR d."UpdatedAt" <= {updatedBefore}::timestamptz)
                AND ({contentUpdatedAfter}::timestamptz IS NULL OR {contentUpdatedAfter}::timestamptz <= d."ContentUpdatedAt")
                AND ({contentUpdatedBefore}::timestamptz IS NULL OR d."ContentUpdatedAt" <= {contentUpdatedBefore}::timestamptz)
                AND ({dueAfter}::timestamptz IS NULL OR {dueAfter}::timestamptz <= d."DueAt")
                AND ({dueBefore}::timestamptz IS NULL OR d."DueAt" <= {dueBefore}::timestamptz)
                AND ({process}::text IS NULL OR d."Process" = {process}::text) -- It's ILike in the code - is that correct?
                AND ({excludeApiOnly}::bool IS NULL OR {excludeApiOnly}::bool = false OR {excludeApiOnly}::bool = true AND d."IsApiOnly" = false)
                --AND ({search}::text IS NULL OR dst."Value" = {search}::text)
                AND ({search}::text IS NULL OR ds.search @@ websearch_to_tsquery({search}::text))
                AND ({systemLabel}::int[] IS NULL OR {systemLabel}::int[] <@ l.labels);
            """)
            .ToListAsync(cancellationToken);

        return result;
    }
}
