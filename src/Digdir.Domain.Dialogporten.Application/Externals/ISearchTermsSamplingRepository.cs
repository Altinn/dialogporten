namespace Digdir.Domain.Dialogporten.Application.Externals;

public interface ISearchTermsSamplingRepository
{
    Task<long> EstimateTotalRowCountAsync(CancellationToken ct);

    Task<IReadOnlyList<string>> EnumerateServiceResourcesAsync(CancellationToken ct);

    Task<IReadOnlyList<SampledDialogIdentity>> SampleViaTableSampleAsync(
        double percent,
        IReadOnlyCollection<string> excludedOrgs,
        CancellationToken ct);

    /// <summary>
    /// Resolves each resource's owning service-owner org code by probing a single dialog per
    /// resource (a resource has exactly one owner). Resources with no non-deleted dialogs are
    /// absent from the result. One round-trip per call — pass resources in batches.
    /// </summary>
    Task<IReadOnlyDictionary<string, string>> GetResourceOrgsAsync(
        IReadOnlyCollection<string> serviceResources,
        CancellationToken ct);

    /// <summary>
    /// Batched Stage B pick: for each resource, selects up to <paramref name="n"/> dialogs
    /// uniformly at random (bounded by an internal per-resource scan cap), excluding any ids in
    /// <paramref name="excludeDialogIds"/> (already sampled in Stage A). One round-trip per call.
    /// </summary>
    Task<IReadOnlyDictionary<string, IReadOnlyList<Guid>>> SampleByResourcesAsync(
        IReadOnlyCollection<string> serviceResources,
        int n,
        IReadOnlyCollection<Guid> excludeDialogIds,
        CancellationToken ct);

    Task<IReadOnlyList<SampledDialogContent>> FetchContentAsync(
        IReadOnlyCollection<Guid> dialogIds,
        CancellationToken ct);

    /// <summary>
    /// Bulk word→stem lookup via Postgres <c>ts_lexize(@dict, w)</c>. Dictionary must be a
    /// registered text-search dictionary (e.g. <c>norwegian_stem</c>, <c>english_stem</c>).
    /// Words for which the dictionary returns no lexeme (stop words / unknown) are absent
    /// from the result; callers should treat absence as "keep the word as-is".
    /// </summary>
    Task<IReadOnlyDictionary<string, string>> StemAsync(
        string dictionary,
        IReadOnlyCollection<string> words,
        CancellationToken ct);

    /// <summary>
    /// Atomically replaces the persisted search-term documents (one per language) with the
    /// supplied set, stamping all of them with the same <paramref name="generatedAt"/>. Any
    /// previously stored documents are removed in the same transaction.
    /// </summary>
    Task ReplaceAsync(
        IReadOnlyList<SearchTermListDocument> documents,
        DateTimeOffset generatedAt,
        CancellationToken ct);
}

public sealed record SampledDialogIdentity(Guid Id, string ServiceResource);

public sealed record SampledDialogContent(
    Guid Id,
    string ServiceResource,
    IReadOnlyList<SampledDialogLocalization> Localizations);

public sealed record SampledDialogLocalization(string LanguageCode, string Value);

/// <summary>A single per-language search-term document to persist. <paramref name="WordsJson"/> is the
/// serialized terse words array (<c>[{ "w": …, "s": [ … ] }]</c>) stored verbatim in the jsonb column.</summary>
public sealed record SearchTermListDocument(string Language, string WordsJson);
