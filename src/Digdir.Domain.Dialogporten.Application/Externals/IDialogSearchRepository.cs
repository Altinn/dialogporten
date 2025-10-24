namespace Digdir.Domain.Dialogporten.Application.Externals;

public interface IDialogSearchRepository
{
    Task UpsertFreeTextSearchIndex(Guid dialogId, CancellationToken cancellationToken);
    Task<int> SeedFullAsync(bool resetExisting, CancellationToken ct);
    Task<int> SeedSinceAsync(DateTimeOffset since, bool resetMatching, CancellationToken ct);
    Task<int> SeedStaleAsync(bool resetMatching, CancellationToken ct);
    Task<int> WorkBatchAsync(int batchSize, long workMemBytes, bool staleFirst, CancellationToken ct);
    Task<DialogSearchReindexProgress> GetProgressAsync(CancellationToken ct);
    Task OptimizeIndexAsync(CancellationToken ct);
}

public sealed class DialogSearchReindexProgress
{
    public long Total { get; init; }
    public long Pending { get; init; }
    public long Processing { get; init; }
    public long Done { get; init; }
}
