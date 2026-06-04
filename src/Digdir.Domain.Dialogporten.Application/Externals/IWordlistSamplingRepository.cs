namespace Digdir.Domain.Dialogporten.Application.Externals;

public interface IWordlistSamplingRepository
{
    Task<long> EstimateTotalRowCountAsync(CancellationToken ct);

    Task<IReadOnlyList<string>> EnumerateServiceResourcesAsync(CancellationToken ct);

    Task<IReadOnlyList<SampledDialogIdentity>> SampleViaTableSampleAsync(
        double percent,
        CancellationToken ct);

    Task<IReadOnlyList<Guid>> SampleByResourceAsync(
        string serviceResource,
        int n,
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
}

public sealed record SampledDialogIdentity(Guid Id, string ServiceResource, DateTimeOffset ContentUpdatedAt);

public sealed record SampledDialogContent(
    Guid Id,
    string ServiceResource,
    IReadOnlyList<SampledDialogLocalization> Localizations);

public sealed record SampledDialogLocalization(string LanguageCode, string Value);
