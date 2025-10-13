using System.Runtime.CompilerServices;
using Digdir.Domain.Dialogporten.Application.Externals;
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
            SELECT "Total", "Pending", "Processing", "Done"
            FROM search."DialogSearchRebuildProgress"
            """);
        var progress = await _db.Database.SqlQuery<ProgressRow>(progressSql).SingleAsync(ct);

        return new DialogSearchReindexProgress(
            Total: progress.Total,
            Pending: progress.Pending,
            Processing: progress.Processing,
            Done: progress.Done
        );
    }

    private sealed class ProgressRow
    {
        public long Total { get; init; }
        public long Pending { get; init; }
        public long Processing { get; init; }
        public long Done { get; init; }
    }
}
