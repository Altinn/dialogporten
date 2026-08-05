using Dapper;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.SearchTerms;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

internal sealed class SearchTermsSamplingRepository : ISearchTermsSamplingRepository
{
    private readonly DialogDbContext _db;
    private readonly NpgsqlDataSource _dataSource;

    public SearchTermsSamplingRepository(DialogDbContext db, NpgsqlDataSource dataSource)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(dataSource);
        _db = db;
        _dataSource = dataSource;
    }

    public async Task<long> EstimateTotalRowCountAsync(CancellationToken ct)
    {
        var reltuples = await _db.Database
            .SqlQuery<double>($@"SELECT reltuples::float8 AS ""Value"" FROM pg_class WHERE oid = '""Dialog""'::regclass")
            .SingleAsync(ct);
        return (long)Math.Max(0, reltuples);
    }

    public async Task<IReadOnlyList<string>> EnumerateServiceResourcesAsync(CancellationToken ct)
    {
        // partyresource."Resource" is maintained by triggers on Dialog and lists
        // every resource referenced by at least one dialog. Reading it is a single
        // seq scan over a small table — far cheaper than enumerating distinct
        // values from the billion-row Dialog table.
        var results = await _db.Database
            .SqlQuery<string>(
                $"""
                 SELECT 'urn:altinn:resource:' || "UnprefixedResourceIdentifier" AS "Value"
                 FROM partyresource."Resource"
                 WHERE "UnprefixedResourceIdentifier" IS NOT NULL
                 """)
            .ToListAsync(ct);
        return results;
    }

    public async Task<IReadOnlyList<SampledDialogIdentity>> SampleViaTableSampleAsync(
        double percent,
        CancellationToken ct)
    {
        const string sql =
            """
            SELECT "Id", "ServiceResource", "ContentUpdatedAt"
            FROM "Dialog" TABLESAMPLE SYSTEM (@Percent)
            WHERE "Deleted" = false AND "ServiceResource" IS NOT NULL;
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(ct);
        var rows = await connection.QueryAsync<(Guid Id, string ServiceResource, DateTime ContentUpdatedAt)>(
            new CommandDefinition(sql, new { Percent = percent }, cancellationToken: ct));
        return rows
            .Select(r => new SampledDialogIdentity(
                r.Id,
                r.ServiceResource,
                new DateTimeOffset(DateTime.SpecifyKind(r.ContentUpdatedAt, DateTimeKind.Utc))))
            .ToList();
    }

    public async Task<IReadOnlyList<Guid>> SampleByResourceAsync(
        string serviceResource,
        int n,
        CancellationToken ct)
    {
        // ORDER BY ContentUpdatedAt DESC biases the long-tail sample toward recently-updated
        // content. The partial index IX_Dialog_ServiceResource_Party_ContentUpdatedAt_Id_NotDeleted
        // has ContentUpdatedAt as its third sort column, so the planner can serve this via an
        // index scan (merging across Party prefixes). Cost is negligible for long-tail resources.
        var ids = await _db.Database
            .SqlQuery<Guid>(
                $"""
                 SELECT "Id" AS "Value"
                 FROM "Dialog"
                 WHERE "ServiceResource" = {serviceResource} AND "Deleted" = false
                 ORDER BY "ContentUpdatedAt" DESC
                 LIMIT {n}
                 """)
            .ToListAsync(ct);
        return ids;
    }

    public async Task<IReadOnlyList<SampledDialogContent>> FetchContentAsync(
        IReadOnlyCollection<Guid> dialogIds,
        CancellationToken ct)
    {
        if (dialogIds.Count == 0)
        {
            return [];
        }

        var ids = dialogIds.ToArray();

        const string contentSql =
            """
            WITH TargetDialogs (DialogId) AS (SELECT unnest(@DialogIds))
            SELECT d."Id"             AS "DialogId"
                 , d."ServiceResource" AS "ServiceResource"
                 , l."LanguageCode"    AS "LanguageCode"
                 , l."Value"           AS "Value"
            FROM TargetDialogs td
            INNER JOIN "Dialog" d ON d."Id" = td.DialogId
            INNER JOIN "DialogContent" c ON c."DialogId" = d."Id" AND c."TypeId" IN (1, 3)
            INNER JOIN "LocalizationSet" ls ON ls."Discriminator" = 'DialogContentValue' AND ls."DialogContentId" = c."Id"
            INNER JOIN "Localization" l ON l."LocalizationSetId" = ls."Id";
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(ct);
        var parameters = new { DialogIds = ids };

        var contentRows = (await connection.QueryAsync<ContentRow>(
            new CommandDefinition(contentSql, parameters, cancellationToken: ct))).ToList();

        return contentRows
            .GroupBy(r => new { r.DialogId, r.ServiceResource })
            .Select(g => new SampledDialogContent(
                g.Key.DialogId,
                g.Key.ServiceResource,
                g.Select(r => new SampledDialogLocalization(r.LanguageCode, r.Value)).ToList()))
            .ToList();
    }

    public async Task<IReadOnlyDictionary<string, string>> StemAsync(
        string dictionary,
        IReadOnlyCollection<string> words,
        CancellationToken ct)
    {
        // Restrict to known dictionaries so typos fail with a clear error instead of a SQL error.
        if (dictionary is not ("norwegian_stem" or "english_stem" or "simple"))
        {
            throw new ArgumentException(
                $"Unsupported text-search dictionary '{dictionary}'. Allowed: norwegian_stem, english_stem, simple.",
                nameof(dictionary));
        }

        if (words.Count == 0)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        var wordArray = words.ToArray();

        // ts_lexize returns text[] (multiple lexemes for compound words); we take the first.
        // Words the dictionary doesn't recognize (stop words / unknown) yield NULL and are
        // filtered out in SQL, so every returned row has a usable stem.
        const string sql =
            """
            SELECT "Word", "Stem"
            FROM (
                SELECT w AS "Word", (ts_lexize(@Dictionary::regdictionary, w))[1] AS "Stem"
                FROM unnest(@Words::text[]) AS w
            ) stems
            WHERE "Stem" IS NOT NULL AND "Stem" <> '';
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(ct);
        var rows = await connection.QueryAsync<StemRow>(
            new CommandDefinition(sql, new { Dictionary = dictionary, Words = wordArray }, cancellationToken: ct));

        return rows.ToDictionary(r => r.Word, r => r.Stem, StringComparer.Ordinal);
    }

    public async Task ReplaceAsync(
        IReadOnlyList<SearchTermListDocument> documents,
        DateTimeOffset generatedAt,
        CancellationToken ct)
    {
        // Delete-then-insert within a single transaction so readers never observe a partial set
        // (and a failed run leaves the previous documents intact). Id and CreatedAt/UpdatedAt are
        // populated by the auditable-entity + UUIDv7 pipeline on SaveChanges.
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        await _db.SearchTermLists.ExecuteDeleteAsync(ct);

        _db.SearchTermLists.AddRange(documents.Select(d => new SearchTermList
        {
            Language = d.Language,
            Words = d.WordsJson,
            GeneratedAt = generatedAt
        }));
        await _db.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);
    }

    private sealed record ContentRow(Guid DialogId, string ServiceResource, string LanguageCode, string Value);
    private sealed record StemRow(string Word, string Stem);
}
