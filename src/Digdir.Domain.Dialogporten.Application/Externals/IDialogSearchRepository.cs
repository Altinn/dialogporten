namespace Digdir.Domain.Dialogporten.Application.Externals;

public interface IDialogSearchRepository
{
    /// <summary>
/// Upserts the free-text search index for the specified dialog.
/// </summary>
/// <param name="dialogId">The identifier of the dialog whose free-text search index should be inserted or updated.</param>
Task UpsertFreeTextSearchIndex(Guid dialogId, CancellationToken cancellationToken);
    /// <summary>
/// Seeds the entire free-text search index for dialogs.
/// </summary>
/// <param name="resetExisting">If true, existing index data may be cleared before seeding.</param>
/// <param name="ct">Cancellation token to cancel the seeding operation.</param>
/// <returns>The number of index entries created or updated.</returns>
Task<int> SeedFullAsync(bool resetExisting, CancellationToken ct);
    /// <summary>
/// Seeds the free-text search index for dialogs updated since the provided timestamp.
/// </summary>
/// <param name="since">Only dialogs updated at or after this timestamp will be considered for seeding.</param>
/// <param name="resetMatching">If true, matching existing index entries may be reset before they are reseeded.</param>
/// <param name="ct">Token to observe for cancellation.</param>
/// <returns>The number of index entries added or updated as part of the seeding operation.</returns>
Task<int> SeedSinceAsync(DateTimeOffset since, bool resetMatching, CancellationToken ct);
    /// <summary>
/// Seeds index data that has been identified as stale.
/// </summary>
/// <param name="resetMatching">If true, reset matching index entries before seeding.</param>
/// <param name="ct">Cancellation token to cancel the operation.</param>
/// <returns>The number of index entries seeded.</returns>
Task<int> SeedStaleAsync(bool resetMatching, CancellationToken ct);
    /// <summary>
/// Processes a batch of indexing work for the free-text search index.
/// </summary>
/// <param name="batchSize">Maximum number of items to process in this batch.</param>
/// <param name="workMemBytes">Memory budget, in bytes, for processing the batch.</param>
/// <param name="staleFirst">If true, prioritize stale items when selecting work to process.</param>
/// <param name="ct">Cancellation token to observe while processing.</param>
/// <returns>The number of items actually processed.</returns>
Task<int> WorkBatchAsync(int batchSize, long workMemBytes, bool staleFirst, CancellationToken ct);
    /// <summary>
/// Gets the current progress of the dialog search reindexing operation.
/// </summary>
/// <returns>A <see cref="DialogSearchReindexProgress"/> containing totals for Total, Pending, Processing, Done, and Failed items.</returns>
Task<DialogSearchReindexProgress> GetProgressAsync(CancellationToken ct);
}

public sealed record DialogSearchReindexProgress(
    long Total,
    long Pending,
    long Processing,
    long Done,
    long Failed
);