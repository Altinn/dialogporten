using Digdir.Domain.Dialogporten.Application.Externals;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

internal sealed class DialogSearchRepository(DialogDbContext dbContext) : IDialogSearchRepository
{
    private readonly DialogDbContext _db =
        dbContext ?? throw new ArgumentNullException(nameof(dbContext));

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
}
