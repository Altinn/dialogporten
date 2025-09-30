using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
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
        var partiesAndServices = JsonSerializer.Serialize(authorizedResources.ResourcesByParties
            .Select(x => (party: x.Key, services: x.Value))
            .GroupBy(x => x.services, new HashSetEqualityComparer<string>())
            .Select(x => new
            {
                parties = x.Select(k => k.party).ToArray(),
                services = x.Key.ToArray()
            }));

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
                ,systemLabelsByDialog AS (
                    SELECT d."Id", array_agg(dsl."SystemLabelId") labels
                    FROM "Dialog" d
                    INNER JOIN "DialogEndUserContext" dec on d."Id" = dec."DialogId"
                    INNER JOIN "DialogEndUserContextSystemLabel" dsl on dsl."DialogEndUserContextId" = dec."Id"
                    WHERE {systemLabels}::int[] is not null AND dsl."SystemLabelId" = ANY({systemLabels}::int[])
                    GROUP BY d."Id")
                ,searchTagsByDialog AS (
                    -- TODO: CREATE INDEX idx_dst_dialogid ON "DialogSearchTag"("DialogId");
                    SELECT d."Id", array_agg(dst."Value") tags
                    FROM "Dialog" d
                    INNER JOIN "DialogSearchTag" dst on d."Id" = dst."DialogId"
                    --WHERE {search}::text is not null AND dst."Value" = ANY({systemLabels}::int[])
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
            .AppendIf(search is not null, $""" AND {search?.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)} <@ t.tags =  """)
            .AppendIf(search is not null, $""" AND ds.search @@ websearch_to_tsquery({search}) """)
            .AppendIf(systemLabel is not null, $""" AND {systemLabel} <@ l.labels """);

        var result = await _db.Database
            .SqlQuery<Guid>(queryBuilder.ToFormattableString())
            .ToListAsync(cancellationToken);

        return result;
    }
}

[SuppressMessage("Style", "IDE0060:Remove unused parameter")]
[SuppressMessage("ReSharper", "EntityNameCapturedOnly.Global")]
public sealed class FormattableStringBuilder
{
    private readonly StringBuilder _format = new();
    private readonly Dictionary<object, int> _argNumberByArg = [];

    public FormattableString ToFormattableString() =>
        FormattableStringFactory.Create(_format.ToString(), _argNumberByArg.OrderBy(x => x.Value).Select(x => x.Key));

    public FormattableStringBuilder Append([InterpolatedStringHandlerArgument("")] ref FormattableStringHandler _) => this;

    public FormattableStringBuilder Append(string value)
    {
        _format.Append(value.Replace("{", "{{").Replace("}", "}}"));
        return this;
    }

    public FormattableStringBuilder AppendIf(
        bool condition,
        [InterpolatedStringHandlerArgument(nameof(condition))]
        ref FormattableStringHandler _) => this;

    [InterpolatedStringHandler]
    public readonly ref struct FormattableStringHandler
    {
        private readonly FormattableStringBuilder _builder;

        public FormattableStringHandler(int literalLength, int formattedCount, FormattableStringBuilder builder)
        {
            _builder = builder;
        }

        public FormattableStringHandler(int literalLength, int formattedCount, bool condition, out bool shouldAppend)
        {
            shouldAppend = condition;
            _builder = null!;
        }

        public void AppendLiteral(string value) => _builder._format.Append(value.Replace("{", "{{").Replace("}", "}}"));

        public void AppendFormatted(object? value, int alignment = 0, string? format = null)
        {
            const string @null = "null";

            if (!_builder._argNumberByArg.TryGetValue(value ?? @null, out var argNumber))
            {
                argNumber = _builder._argNumberByArg[value ?? @null] = _builder._argNumberByArg.Count;
            }

            _builder._format.Append('{').Append(argNumber);
            if (alignment != 0) _builder._format.Append(',').Append(alignment);
            if (format is not null) _builder._format.Append(':').Append(format);
            _builder._format.Append('}');
        }
    }
}





//     $"""
//             WITH partyResourceAccess AS (
//                 SELECT DISTINCT s.service, p.party
//                 FROM jsonb_to_recordset({partiesAndServices}::jsonb) AS x(parties text[], services text[])
//                 CROSS JOIN LATERAL unnest(x.services) AS s(service)
//                 CROSS JOIN LATERAL unnest(x.parties) AS p(party))
//             ,accessibleDialogs AS (
//                 SELECT DISTINCT d."Id"
//                 FROM "Dialog" d
//                 INNER JOIN partyResourceAccess c
//                 ON d."ServiceResource" = c.service AND d."Party" = c.party)
//             ,systemLabelsByDialog AS (
//                 SELECT d."Id", array_agg(dsl."SystemLabelId") labels
//                 FROM "Dialog" d
//                 INNER JOIN "DialogEndUserContext" dec on d."Id" = dec."DialogId"
//                 INNER JOIN "DialogEndUserContextSystemLabel" dsl on dsl."DialogEndUserContextId" = dec."Id"
//                 WHERE {systemLabels}::int[] is not null AND dsl."SystemLabelId" = ANY({systemLabels}::int[])
//                 GROUP BY d."Id")
//             SELECT d."Id"
//             FROM "Dialog" d
//             INNER JOIN accessibleDialogs a ON d."Id" = a."Id"
//             LEFT JOIN "DialogSearch" ds ON ds."DialogId" = d."Id"
//             --LEFT JOIN "DialogSearchTag" dst on d."Id" = dst."DialogId" -- TODO: No many relationship here! 
//             LEFT JOIN systemLabelsByDialog l on l."Id" = d."Id"
//             WHERE d."Deleted" = false
//                 AND (d."VisibleFrom" IS NULL or d."VisibleFrom" < {now}::timestamptz)
//                 AND (d."ExpiresAt" IS NULL or d."ExpiresAt" > {now}::timestamptz)
//                 AND ({org}::text[] IS NULL OR d."Org" = ANY({org}::text[]))
//                 AND ({serviceResource}::text[] IS NULL OR d."ServiceResource" = ANY({serviceResource}::text[]))
//                 AND ({party}::text[] IS NULL OR d."Party" = ANY({party}::text[]))
//                 AND ({extendedStatus}::text[] IS NULL OR d."ExtendedStatus" = ANY({extendedStatus}::text[]))
//                 AND ({externalReference}::text IS NULL OR d."ExternalReference" = {externalReference}::text)
//                 AND ({status}::int[] IS NULL OR d."StatusId" = ANY({status}::int[]))
//                 AND ({createdAfter}::timestamptz IS NULL OR {createdAfter}::timestamptz <= d."CreatedAt")
//                 AND ({createdBefore}::timestamptz IS NULL OR d."CreatedAt" <= {createdBefore}::timestamptz)
//                 AND ({updatedAfter}::timestamptz IS NULL OR {updatedAfter}::timestamptz <= d."UpdatedAt")
//                 AND ({updatedBefore}::timestamptz IS NULL OR d."UpdatedAt" <= {updatedBefore}::timestamptz)
//                 AND ({contentUpdatedAfter}::timestamptz IS NULL OR {contentUpdatedAfter}::timestamptz <= d."ContentUpdatedAt")
//                 AND ({contentUpdatedBefore}::timestamptz IS NULL OR d."ContentUpdatedAt" <= {contentUpdatedBefore}::timestamptz)
//                 AND ({dueAfter}::timestamptz IS NULL OR {dueAfter}::timestamptz <= d."DueAt")
//                 AND ({dueBefore}::timestamptz IS NULL OR d."DueAt" <= {dueBefore}::timestamptz)
//                 AND ({process}::text IS NULL OR d."Process" = {process}::text) -- It's ILike in the code - is that correct?
//                 AND ({excludeApiOnly}::bool IS NULL OR {excludeApiOnly}::bool = false OR {excludeApiOnly}::bool = true AND d."IsApiOnly" = false)
//                 --AND ({search}::text IS NULL OR dst."Value" = {search}::text)
//                 AND ({search}::text IS NULL OR ds.search @@ websearch_to_tsquery({search}::text))
//                 AND ({systemLabel}::int[] IS NULL OR {systemLabel}::int[] <@ l.labels);
//             """
