using System.Runtime.CompilerServices;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

internal sealed class DialogSearchRepository(DialogDbContext dbContext) : IDialogSearchRepository
{
    private readonly DialogDbContext _db =
        dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    /// <summary>
    /// Upserts the full-text search index for the specified dialog in the database.
    /// </summary>
    /// <param name="dialogId">The identifier of the dialog whose search index should be upserted.</param>
    public async Task UpsertFreeTextSearchIndex(Guid dialogId, CancellationToken cancellationToken)
    {
        var sql = FormattableStringFactory.Create(
            "SELECT search.\"UpsertDialogSearchOne\"({0})",
            dialogId);

        await _db.Database.ExecuteSqlAsync(sql, cancellationToken);
    }

    /// <summary>
    /// Seeds the full dialog search rebuild queue in the database.
    /// </summary>
    /// <param name="resetExisting">If true, clear existing queue entries before seeding.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>The number of items seeded into the dialog search queue.</returns>
    public async Task<int> SeedFullAsync(bool resetExisting, CancellationToken ct)
    {
        var sql = FormattableStringFactory.Create(
            "SELECT search.\"SeedDialogSearchQueueFull\"({0}) AS \"Value\"",
            resetExisting);

        return await _db.Database.SqlQuery<int>(sql).SingleAsync(ct);
    }

    /// <summary>
    /// Seeds the dialog search queue with items modified since the specified time.
    /// </summary>
    /// <param name="since">The cutoff timestamp; items modified at or after this time are considered (interpreted as UTC).</param>
    /// <param name="resetMatching">If true, reset matching queue entries before seeding.</param>
    /// <returns>The integer result returned by the seeding function — typically the number of items queued for reindexing.</returns>
    public async Task<int> SeedSinceAsync(DateTimeOffset since, bool resetMatching, CancellationToken ct)
    {
        var utc = since.ToUniversalTime();
        var sql = FormattableStringFactory.Create(
            "SELECT search.\"SeedDialogSearchQueueSince\"({0}, {1}) AS \"Value\"",
            utc, resetMatching);

        return await _db.Database.SqlQuery<int>(sql).SingleAsync(ct);
    }

    /// <summary>
    /// Seeds stale dialogs into the dialog search rebuild queue.
    /// </summary>
    /// <param name="resetMatching">If true, reset existing matching queue entries before seeding.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>The number of dialogs enqueued for rebuild.</returns>
    public async Task<int> SeedStaleAsync(bool resetMatching, CancellationToken ct)
    {
        var sql = FormattableStringFactory.Create(
            "SELECT search.\"SeedDialogSearchQueueStale\"({0}) AS \"Value\"",
            resetMatching);

        return await _db.Database.SqlQuery<int>(sql).SingleAsync(ct);
    }

    /// <summary>
    /// Executes a single database work batch to rebuild the dialog search index using the specified mode.
    /// </summary>
    /// <param name="batchSize">Maximum number of items the batch should process.</param>
    /// <param name="workMemBytes">Amount of working memory in bytes allocated for the database operation.</param>
    /// <param name="staleFirst">If true, process stale entries before others; otherwise use the standard ordering.</param>
    /// <returns>The number of items processed by the batch.</returns>
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

    /// <summary>
    /// Retrieves the current progress of the dialog search reindexing operation.
    /// </summary>
    /// <returns>A DialogSearchReindexProgress containing total, pending, processing, done, and failed counts.</returns>
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