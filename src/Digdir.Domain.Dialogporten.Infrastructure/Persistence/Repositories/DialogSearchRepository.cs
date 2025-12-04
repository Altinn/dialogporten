using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Dapper;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

internal sealed class DialogSearchRepository(DialogDbContext dbContext, ILogger<DialogSearchRepository> logger, NpgsqlDataSource dataSource) : IDialogSearchRepository
{
    private readonly DialogDbContext _db = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly NpgsqlDataSource _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));

    public async Task UpsertFreeTextSearchIndex(Guid dialogId, CancellationToken cancellationToken)
    {
        await _db.Database.ExecuteSqlAsync($@"SELECT search.""UpsertDialogSearchOne""({dialogId})", cancellationToken);
    }

    public async Task<int> SeedFullAsync(bool resetExisting, CancellationToken ct) =>
        await _db.Database
            .SqlQuery<int>($@"SELECT search.""SeedDialogSearchQueueFull""({resetExisting}) AS ""Value""")
            .SingleAsync(ct);

    public async Task<int> SeedSinceAsync(DateTimeOffset since, bool resetMatching, CancellationToken ct) =>
        await _db.Database
            .SqlQuery<int>($@"SELECT search.""SeedDialogSearchQueueSince""({since}, {resetMatching}) AS ""Value""")
            .SingleAsync(ct);

    public async Task<int> SeedStaleAsync(bool resetMatching, CancellationToken ct) =>
        await _db.Database
            .SqlQuery<int>($@"SELECT search.""SeedDialogSearchQueueStale""({resetMatching}) AS ""Value""")
            .SingleAsync(ct);

    public async Task<int> WorkBatchAsync(int batchSize, long workMemBytes, bool staleFirst, CancellationToken ct) =>
        await _db.Database
            .SqlQuery<int>($@"SELECT search.""RebuildDialogSearchOnce""({(staleFirst ? "stale_first" : "standard")}, {batchSize}, {workMemBytes}) AS ""Value""")
            .SingleAsync(ct);

    public async Task<DialogSearchReindexProgress> GetProgressAsync(CancellationToken ct) =>
        await _db.Database
            .SqlQuery<DialogSearchReindexProgress>(
                $"""
                 SELECT "Total", "Pending", "Processing", "Done"
                 FROM search."DialogSearchRebuildProgress"
                 """)
            .SingleAsync(ct);

    public async Task OptimizeIndexAsync(CancellationToken ct)
    {
        await _db.Database.ExecuteSqlAsync($@"VACUUM ANALYZE search.""DialogSearch""", ct);
    }

    [SuppressMessage("Style", "IDE0037:Use inferred member name")]
    public async Task<PaginatedList<DialogEntity>> GetDialogs(
        GetDialogsQuery query,
        DialogSearchAuthorizationResult authorizedResources,
        CancellationToken cancellationToken)
    {
        const int searchSampleLimit = 10_000;

        if (query.Limit > searchSampleLimit)
        {
            throw new ArgumentOutOfRangeException(nameof(query.Limit),
                $"Limit cannot be greater than the search sample limit of {searchSampleLimit}.");
        }

        if (authorizedResources.HasNoAuthorizations)
        {
            return new PaginatedList<DialogEntity>([], false, null, query.OrderBy!.GetOrderString());
        }

        var partiesAndServices = authorizedResources.ResourcesByParties
            .Select(x => (party: x.Key, services: x.Value))
            .GroupBy(x => x.services, new HashSetEqualityComparer<string>())
            .Select(x => new PartiesAndServices(
                x.Select(k => k.party)
                    .Where(p => query.Party.IsNullOrEmpty() || query.Party.Contains(p))
                    .ToArray(),
                x.Key
                    .Where(s => query.ServiceResource.IsNullOrEmpty() || query.ServiceResource.Contains(s))
                    .ToArray()
               )
            )
            .Where(x => x.Parties.Length > 0 && x.Services.Length > 0)
            .ToList();

        LogPartiesAndServicesCount(logger, partiesAndServices);

        // TODO: Respect instance delegated dialogs
        var accessibleFilteredDialogs = new PostgresFormattableStringBuilder()
            .AppendIf(query.Search is null,
                """
                SELECT d."Id"
                FROM "Dialog" d
                WHERE d."Party" = ppm.party

                """)
            .AppendIf(query.Search is not null,
                """
                SELECT d."Id"
                FROM search."DialogSearch" ds 
                JOIN "Dialog" d ON d."Id" = ds."DialogId"
                CROSS JOIN searchString ss
                WHERE ds."Party" = ppm.party AND ds."SearchVector" @@ ss.searchVector

                """)
            .Append(
                """
                AND d."ServiceResource" = ANY(ppm.allowed_services)

                """)
            .AppendIf(query.Deleted is not null, $""" AND d."Deleted" = {query.Deleted}::boolean """)
            .AppendIf(query.VisibleAfter is not null, $""" AND (d."VisibleFrom" IS NULL OR d."VisibleFrom" <= {query.VisibleAfter}::timestamptz) """)
            .AppendIf(query.ExpiresBefore is not null, $""" AND (d."ExpiresAt" IS NULL OR d."ExpiresAt" > {query.ExpiresBefore}::timestamptz) """)
            .AppendIf(!query.Org.IsNullOrEmpty(), $""" AND d."Org" = ANY({query.Org}::text[]) """)
            .AppendIf(!query.ExtendedStatus.IsNullOrEmpty(), $""" AND d."ExtendedStatus" = ANY({query.ExtendedStatus}::text[]) """)
            .AppendIf(query.ExternalReference is not null, $""" AND d."ExternalReference" = {query.ExternalReference}::text """)
            .AppendIf(!query.Status.IsNullOrEmpty(), $""" AND d."StatusId" = ANY({query.Status}::int[]) """)
            .AppendIf(query.CreatedAfter is not null, $""" AND {query.CreatedAfter}::timestamptz <= d."CreatedAt" """)
            .AppendIf(query.CreatedBefore is not null, $""" AND d."CreatedAt" <= {query.CreatedBefore}::timestamptz """)
            .AppendIf(query.UpdatedAfter is not null, $""" AND {query.UpdatedAfter}::timestamptz <= d."UpdatedAt" """)
            .AppendIf(query.UpdatedBefore is not null, $""" AND d."UpdatedAt" <= {query.UpdatedBefore}::timestamptz """)
            .AppendIf(query.ContentUpdatedAfter is not null, $""" AND {query.ContentUpdatedAfter}::timestamptz <= d."ContentUpdatedAt" """)
            .AppendIf(query.ContentUpdatedBefore is not null, $""" AND d."ContentUpdatedAt" <= {query.ContentUpdatedBefore}::timestamptz """)
            .AppendIf(query.DueAfter is not null, $""" AND {query.DueAfter}::timestamptz <= d."DueAt" """)
            .AppendIf(query.DueBefore is not null, $""" AND d."DueAt" <= {query.DueBefore}::timestamptz """)
            .AppendIf(query.Process is not null, $""" AND d."Process" = {query.Process}::text """)
            .AppendIf(query.ExcludeApiOnly is not null, $""" AND ({query.ExcludeApiOnly}::boolean = false OR {query.ExcludeApiOnly}::boolean = true AND d."IsApiOnly" = false) """)
            .AppendIf(!query.SystemLabel.IsNullOrEmpty(),
                $"""
                 AND (
                     SELECT COUNT(sl."SystemLabelId")
                     FROM "DialogEndUserContext" dec 
                     JOIN "DialogEndUserContextSystemLabel" sl ON dec."Id" = sl."DialogEndUserContextId"
                     WHERE dec."DialogId" = d."Id"
                        AND sl."SystemLabelId" = ANY({query.SystemLabel}::int[]) 
                     ) = {query.SystemLabel?.Count}::int
                 """)
            .ApplyPaginationCondition(query.OrderBy!, query.ContinuationToken, alias: "d")
            .ApplyPaginationOrder(query.OrderBy!, alias: "d")
            .ApplyPaginationLimit(query.Limit);


        var queryBuilder = new PostgresFormattableStringBuilder()
            .Append("WITH ")
            .AppendIf(query.Search is not null,
                $"""
                searchString AS (
                   SELECT websearch_to_tsquery(coalesce(isomap."TsConfigName", 'simple')::regconfig, {query.Search}::text) searchVector
                    ,string_to_array({query.Search}::text, ' ') AS searchTerms
                    FROM (VALUES (coalesce({query.SearchLanguageCode}::text, 'simple'))) AS v(isoCode)
                    LEFT JOIN search."Iso639TsVectorMap" isomap ON v.isoCode = isomap."IsoCode"
                    LIMIT 1
                ),
                """)
            .Append(
                $"""
                 raw_permissions AS (
                    SELECT p.party, s.service
                    FROM jsonb_to_recordset({JsonSerializer.Serialize(partiesAndServices)}::jsonb) AS x("Parties" text[], "Services" text[])
                    CROSS JOIN LATERAL unnest(x."Services") AS s(service)
                    CROSS JOIN LATERAL unnest(x."Parties") AS p(party)
                 )
                 ,party_permission_map AS (
                     SELECT party
                          , ARRAY_AGG(service) AS allowed_services
                     FROM raw_permissions
                     GROUP BY party
                 )
                 SELECT d.*
                 FROM (
                     SELECT d_inner."Id"
                     FROM party_permission_map ppm
                     CROSS JOIN LATERAL (
                         {accessibleFilteredDialogs}
                     ) d_inner
                 ) AS filtered_dialogs
                 JOIN "Dialog" d ON d."Id" = filtered_dialogs."Id"

                 """);

        // DO NOT use Include here, as it will use the custom SQL above which is
        // much less efficient than querying further by the resulting dialogIds.
        // We only get dialogs here, and will later query related data as
        // needed based on the IDs.
        var efQuery = _db.Dialogs
            .FromSql(queryBuilder.ToFormattableString())
            .IgnoreQueryFilters()
            .AsNoTracking();

        var dialogs = await efQuery.ToPaginatedListAsync(
            query.OrderBy!,
            query.ContinuationToken,
            query.Limit,
            applyOrder: true,
            applyContinuationToken: false,
            cancellationToken);

        return dialogs;
    }

    public async Task<Dictionary<Guid, int>> FetchGuiAttachmentCountByDialogId(Guid[] dialogIds,
        CancellationToken cancellationToken)
    {
        var (query, parameters) = new PostgresFormattableStringBuilder()
            .Append(
                $"""
                 SELECT a."DialogId"
                      , COUNT(a."Id") AS "GuiAttachmentCount"
                 FROM "Attachment" AS a
                 WHERE a."Discriminator" = 'DialogAttachment'
                   AND a."DialogId" = ANY ({dialogIds}::uuid[])
                   AND EXISTS (
                     SELECT 1
                     FROM "AttachmentUrl" AS au
                     WHERE au."AttachmentId" = a."Id"
                       AND au."ConsumerTypeId" = 1 -- GUI
                 )
                 GROUP BY a."DialogId";
                 """)
            .ToDynamicParameters();

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var result = await connection.QueryAsync<(Guid Id, int GuiAttachmentCount)>(query, parameters);
        return result.ToDictionary(x => x.Id, x => x.GuiAttachmentCount);
    }

    public async Task<Dictionary<Guid, DataContentDto>> FetchContentByDialogId(
        Guid[] dialogIds,
        int userAuthLevel,
        CancellationToken cancellationToken)
    {
        var (query, parameters) = new PostgresFormattableStringBuilder()
            .Append(
                $"""
                 SELECT d."Id" AS dialogId
                      , COALESCE(r."MinimumAuthenticationLevel", 0) AS authLevel
                      , c."TypeId"
                      , c."MediaType"
                      , l."LanguageCode"
                      , l."Value"
                 FROM "Dialog" d
                 LEFT JOIN "ResourcePolicyInformation" AS r ON d."ServiceResource" = r."Resource"
                 INNER JOIN "DialogContent" AS c ON c."DialogId" = d."Id"
                 INNER JOIN "DialogContentType" AS ct ON c."TypeId" = ct."Id" AND ct."OutputInList"
                 INNER JOIN "LocalizationSet" ls ON ls."Discriminator" = 'DialogContentValue' AND ls."DialogContentId" = c."Id"
                 INNER JOIN "Localization" l ON l."LocalizationSetId" = ls."Id"
                 WHERE d."Id" = ANY ({dialogIds}::uuid[]);
                 """)
            .ToDynamicParameters();

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var rawRows = await connection.QueryAsync<RawContentRow>(query, parameters);
        return rawRows
            .GroupBy(x => new { x.DialogId, x.AuthLevel })
            .ToDictionary(x => x.Key.DialogId, row =>
            {
                var hasRequiredAuth = row.Key.AuthLevel <= userAuthLevel;
                var contentValues = row
                    .GroupBy(r => new { r.TypeId, r.MediaType })
                    .Select(x => new DataContentValueDto(TypeId: x.Key.TypeId, MediaType: x.Key.MediaType, Value: x
                        .Select(r => new DataLocalizationDto(r.LanguageCode, r.Value))
                        .ToList()))
                    .ToList();
                return new DataContentDto(
                    Title: PickByAuth(contentValues,
                        sensitive: DialogContentType.Values.Title,
                        nonSensitive: DialogContentType.Values.NonSensitiveTitle,
                        hasRequiredAuth: hasRequiredAuth)!,
                    Summary: PickByAuth(contentValues,
                        sensitive: DialogContentType.Values.Summary,
                        nonSensitive: DialogContentType.Values.NonSensitiveSummary,
                        hasRequiredAuth: hasRequiredAuth),
                    ExtendedStatus: contentValues.FirstOrDefault(x =>
                        x.TypeId == DialogContentType.Values.ExtendedStatus),
                    SenderName: contentValues.FirstOrDefault(x =>
                        x.TypeId == DialogContentType.Values.SenderName));
            });
    }

    public async Task<Dictionary<Guid, DataDialogEndUserContextDto>> FetchEndUserContextByDialogId(
        Guid[] dialogIds,
        CancellationToken cancellationToken)
    {
        var (query, parameters) = new PostgresFormattableStringBuilder()
            .Append(
                $"""
                 SELECT c."DialogId"
                      , c."Revision"
                      , cl."SystemLabelId"
                 FROM "DialogEndUserContext" AS c
                 INNER JOIN "DialogEndUserContextSystemLabel" AS cl ON c."Id" = cl."DialogEndUserContextId"
                 WHERE c."DialogId" = ANY ({dialogIds}::uuid[])
                 """)
            .ToDynamicParameters();

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var rawRows = await connection.QueryAsync<RawEndUserContextRow>(query, parameters);
        return rawRows
            .GroupBy(x => new { x.DialogId, x.Revision })
            .ToDictionary(x => x.Key.DialogId, row => new DataDialogEndUserContextDto(
                row.Key.Revision,
                row.Select(x => x.SystemLabelId)
                    .ToList()));
    }

    public async Task<Dictionary<Guid, List<DataDialogSeenLogDto>>> FetchSeenLogByDialogId(
        Guid[] dialogIds,
        string currentUserId,
        CancellationToken cancellationToken)
    {
        var (query, parameters) = new PostgresFormattableStringBuilder()
            .Append(
                $"""
                 SELECT sl."DialogId"
                      , sl."Id" AS "SeenLogId"
                      , sl."CreatedAt" AS "SeenAt"
                      , sl."IsViaServiceOwner"
                      , a."ActorTypeId" AS "ActorType"
                      , an."ActorId" IS NOT NULL AND an."ActorId" = {currentUserId}::text AS "IsCurrentEndUser"
                      , an."ActorId"
                      , an."Name" AS "ActorName"
                 FROM "Dialog" d
                 INNER JOIN "DialogSeenLog" sl ON d."Id" = sl."DialogId"
                 INNER JOIN "Actor" a ON a."Discriminator" = 'DialogSeenLogSeenByActor' AND sl."Id" = a."DialogSeenLogId"
                 LEFT JOIN "ActorName" an ON a."ActorNameEntityId" = an."Id"
                 WHERE d."Id" = ANY ({dialogIds}::uuid[])
                   AND sl."CreatedAt" >= d."ContentUpdatedAt";
                 """)
            .ToDynamicParameters();

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var rawRows = await connection.QueryAsync<RawSeenLogRow>(query, parameters);
        return rawRows
            .GroupBy(x => x.DialogId)
            .ToDictionary(x => x.Key, x => x.Select(row => new DataDialogSeenLogDto(
                row.SeenLogId,
                row.DialogId,
                row.SeenAt,
                row.IsViaServiceOwner,
                row.IsCurrentEndUser,
                new DataActorDto(row.ActorType,
                    row.ActorId,
                    row.ActorName)))
            .ToList());
    }

    public async Task<Dictionary<Guid, DataDialogActivityDto>> FetchLatestActivitiesByDialogId(
        Guid[] dialogIds,
        CancellationToken cancellationToken)
    {
        var (query, parameters) = new PostgresFormattableStringBuilder()
            .Append(
                $"""
                 WITH
                     latestActivity AS (
                         SELECT DISTINCT ON (da."DialogId") 
                             da."DialogId"
                             , da."Id" AS "ActivityId"
                             , da."CreatedAt"
                             , da."TypeId"
                             , da."ExtendedType"
                             , da."TransmissionId"
                         FROM "DialogActivity" da
                         WHERE da."DialogId" = ANY ({dialogIds}::uuid[])
                         ORDER BY da."DialogId", da."CreatedAt" DESC, da."Id" DESC
                     )
                 SELECT la."DialogId"
                      , la."ActivityId"
                      , la."CreatedAt"
                      , la."TypeId"
                      , la."ExtendedType"
                      , la."TransmissionId"
                      , a."ActorTypeId" AS "ActorType"
                      , an."ActorId"
                      , an."Name"       AS "ActorName"
                      , l."LanguageCode"
                      , l."Value"       AS "Description"
                 FROM latestActivity la
                 INNER JOIN "Actor" a ON a."Discriminator" = 'DialogActivityPerformedByActor' AND a."ActivityId" = la."ActivityId"
                 LEFT JOIN "ActorName" an ON an."Id" = a."ActorNameEntityId"
                 LEFT JOIN "LocalizationSet" ls ON ls."Discriminator" = 'DialogActivityDescription' AND ls."ActivityId" = la."ActivityId"
                 LEFT JOIN "Localization" l ON l."LocalizationSetId" = ls."Id";
                 """)
            .ToDynamicParameters();

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var rawRows = await connection.QueryAsync<RawActivityRow>(query, parameters);
        return rawRows
            .GroupBy(x => x.DialogId)
            .ToDictionary(x => x.Key, x => x
                .GroupBy(y => y.ActivityId)
                .Select(y =>
                {
                    var row = y.First();
                    return new DataDialogActivityDto(
                        row.ActivityId,
                        row.CreatedAt,
                        row.TypeId,
                        Uri.TryCreate(row.ExtendedType, UriKind.RelativeOrAbsolute, out var extendedType)
                            ? extendedType : null,
                        row.TransmissionId,
                        new DataActorDto(
                            row.ActorType,
                            row.ActorId,
                            row.ActorName),
                        y.Where(x => x.Description is not null && x.LanguageCode is not null)
                            .Select(x => new DataLocalizationDto(x.LanguageCode!, x.Description!))
                            .ToList()
                    );
                })
                .Single()
            );
    }

    [SuppressMessage("Style", "IDE0072:Add missing cases")]
    private static DataContentValueDto? PickByAuth(
        List<DataContentValueDto> values,
        DialogContentType.Values sensitive,
        DialogContentType.Values nonSensitive,
        bool hasRequiredAuth) => values
        .Where(x => x.TypeId == sensitive || x.TypeId == nonSensitive)
        .OrderBy(x => x.TypeId switch
        {
            _ when x.TypeId == sensitive && hasRequiredAuth => 0,
            _ when x.TypeId == nonSensitive && !hasRequiredAuth => 1,
            _ when x.TypeId == sensitive => 2,
            _ => 3
        })
        .FirstOrDefault();

    private sealed record PartiesAndServices(string[] Parties, string[] Services);
    private static void LogPartiesAndServicesCount(ILogger<DialogSearchRepository> logger, List<PartiesAndServices> partiesAndServices)
    {
        var totalPartiesCount = partiesAndServices.Sum(g => g.Parties.Length);
        var totalServicesCount = partiesAndServices.Sum(g => g.Services.Length);
        var groupsCount = partiesAndServices.Count;
        var groupSizes = partiesAndServices
            .Select(g => (g.Parties.Length, g.Services.Length))
            .ToList();

        logger.LogInformation(
            "PartiesAndServices: tp={TotalPartiesCount}, ts={TotalServicesCount}, g={GroupsCount}, gs={GroupSizes}",
            totalPartiesCount, totalServicesCount, groupsCount, groupSizes);
    }

    private sealed record RawContentRow(Guid DialogId, int AuthLevel, DialogContentType.Values TypeId, string MediaType,
        string LanguageCode, string Value);
    private sealed record RawEndUserContextRow(Guid DialogId, Guid Revision, SystemLabel.Values SystemLabelId);
    private sealed record RawSeenLogRow(Guid DialogId, Guid SeenLogId, DateTime SeenAt, bool IsViaServiceOwner,
        ActorType.Values ActorType, bool IsCurrentEndUser, string? ActorId, string? ActorName);
    private sealed record RawActivityRow(Guid DialogId, Guid ActivityId, DateTime CreatedAt,
        DialogActivityType.Values TypeId, string? ExtendedType, Guid? TransmissionId, ActorType.Values ActorType,
        string? ActorId, string? ActorName, string? LanguageCode, string? Description);
}
