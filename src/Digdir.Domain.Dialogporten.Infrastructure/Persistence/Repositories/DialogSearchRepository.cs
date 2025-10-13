using System.Runtime.CompilerServices;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

internal sealed class DialogSearchRepository(DialogDbContext dbContext) : IDialogSearchRepository
{
    private readonly DialogDbContext _db =
        dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task UpsertFreeTextSearchIndex(Guid dialogId, CancellationToken cancellationToken)
    {
        var sql = FormattableStringFactory.Create(
            "SELECT search.\"UpsertDialogSearchOne\"({0})",
            dialogId);

        await _db.Database.ExecuteSqlAsync(sql, cancellationToken);
    }

    public async Task<int> SeedFullAsync(bool resetExisting, CancellationToken ct)
    {
        var sql = FormattableStringFactory.Create(
            "SELECT search.\"SeedDialogSearchQueueFull\"({0}) AS \"Value\"",
            resetExisting);

        return await _db.Database.SqlQuery<int>(sql).SingleAsync(ct);
    }

    public async Task<int> SeedSinceAsync(DateTimeOffset since, bool resetMatching, CancellationToken ct)
    {
        var utc = since.ToUniversalTime();
        var sql = FormattableStringFactory.Create(
            "SELECT search.\"SeedDialogSearchQueueSince\"({0}, {1}) AS \"Value\"",
            utc, resetMatching);

        return await _db.Database.SqlQuery<int>(sql).SingleAsync(ct);
    }

    public async Task<int> SeedStaleAsync(bool resetMatching, CancellationToken ct)
    {
        var sql = FormattableStringFactory.Create(
            "SELECT search.\"SeedDialogSearchQueueStale\"({0}) AS \"Value\"",
            resetMatching);

        return await _db.Database.SqlQuery<int>(sql).SingleAsync(ct);
    }

    public async Task<int> WorkBatchAsync(int batchSize, long workMemBytes, bool staleFirst, CancellationToken ct)
    {
        var sql = staleFirst
            ? FormattableStringFactory.Create(
                "SELECT search.\"RebuildDialogSearchOnce\"('stale_first', {0}, {1}) AS \"Value\"",
                batchSize, workMemBytes)
            : FormattableStringFactory.Create(
                "SELECT search.\"RebuildDialogSearchOnce\"('standard', {0}, {1}) AS \"Value\"",
                batchSize, workMemBytes);

        return await _db.Database.SqlQuery<int>(sql).SingleAsync(ct);
    }

    public async Task<DialogSearchReindexProgress> GetProgressAsync(CancellationToken ct)
    {
        var progressSql = FormattableStringFactory.Create(
            """
            SELECT "Total" AS total,
                   "Pending" AS pending,
                   "Processing" AS processing,
                   "Done" AS done,
                   "Failed" AS failed
            FROM search."DialogSearchRebuildProgress"
            """);
        var progress = await _db.Database.SqlQuery<ProgressRow>(progressSql).SingleAsync(ct);

        return new DialogSearchReindexProgress(
            Total: progress.total,
            Pending: progress.pending,
            Processing: progress.processing,
            Done: progress.done,
            Failed: progress.failed
        );
    }

#pragma warning disable IDE1006
    private sealed class ProgressRow
    {
        public long total { get; init; }
        public long pending { get; init; }
        public long processing { get; init; }
        public long done { get; init; }
        public long failed { get; init; }
    }

#pragma warning restore IDE1006
}
