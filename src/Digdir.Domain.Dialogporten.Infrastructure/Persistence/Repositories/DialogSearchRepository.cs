using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Digdir.Domain.Dialogporten.Application.Externals;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

internal sealed class DialogSearchRepository(IServiceScopeFactory scopeFactory) : IDialogSearchRepository
{
    // As DbContext is not thread safe, we create a new scope and DbContext instance for each operation,
    // allowing the repository to be a singleton service (fewer allocations) and be used concurrently.
    private readonly IServiceScopeFactory _scopeFactory =
        scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

    public async Task UpsertFreeTextSearchIndex(Guid dialogId, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DialogDbContext>();

        var sql = FormattableStringFactory.Create(
            "SELECT search.upsert_dialogsearch_one({0})",
            dialogId);

        await db.Database.ExecuteSqlAsync(sql, cancellationToken);
    }

    public async Task<int> SeedFullAsync(bool resetExisting, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DialogDbContext>();

        var sql = FormattableStringFactory.Create(
            "SELECT search.seed_dialogsearch_queue_full({0}) AS \"Value\"",
            resetExisting);

        return await db.Database.SqlQuery<int>(sql).SingleAsync(ct);
    }

    public async Task<int> SeedSinceAsync(DateTimeOffset since, bool resetMatching, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DialogDbContext>();

        var utc = since.ToUniversalTime();
        var sql = FormattableStringFactory.Create(
            "SELECT search.seed_dialogsearch_queue_since({0}, {1}) AS \"Value\"",
            utc, resetMatching);

        return await db.Database.SqlQuery<int>(sql).SingleAsync(ct);
    }

    public async Task<int> SeedStaleAsync(bool resetMatching, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DialogDbContext>();

        var sql = FormattableStringFactory.Create(
            "SELECT search.seed_dialogsearch_queue_stale({0}) AS \"Value\"",
            resetMatching);

        return await db.Database.SqlQuery<int>(sql).SingleAsync(ct);
    }

    public async Task<int> WorkBatchAsync(int batchSize, long workMemBytes, bool staleFirst, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DialogDbContext>();

        var sql = staleFirst
            ? FormattableStringFactory.Create(
                "SELECT search.rebuild_dialogsearch_once('stale_first', {0}, {1}) AS \"Value\"",
                batchSize, workMemBytes)
            : FormattableStringFactory.Create(
                "SELECT search.rebuild_dialogsearch_once('standard', {0}, {1}) AS \"Value\"",
                batchSize, workMemBytes);

        return await db.Database.SqlQuery<int>(sql).SingleAsync(ct);
    }

    public async Task<DialogSearchReindexProgress> GetProgressAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DialogDbContext>();

        var progressSql = FormattableStringFactory.Create(
            "SELECT total, pending, processing, done, failed FROM search.dialogsearch_rebuild_progress");
        var progress = await db.Database.SqlQuery<ProgressRow>(progressSql).SingleAsync(ct);

        var ratesSql = FormattableStringFactory.Create(
            "SELECT dps_1m, dps_5m, dps_15m FROM search.dialogsearch_rates");
        var rates = await db.Database.SqlQuery<RatesRow>(ratesSql).SingleAsync(ct);

        return new DialogSearchReindexProgress(
            Total: progress.total,
            Pending: progress.pending,
            Processing: progress.processing,
            Done: progress.done,
            Failed: progress.failed,
            DialogsPerSec1m: rates.dps_1m ?? 0d,
            DialogsPerSec5m: rates.dps_5m ?? 0d,
            DialogsPerSec15m: rates.dps_15m ?? 0d
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

    private sealed class RatesRow
    {
        public double? dps_1m { get; init; }
        public double? dps_5m { get; init; }
        public double? dps_15m { get; init; }
    }
#pragma warning restore IDE1006
}
