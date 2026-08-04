using System.Text.Json;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Features.V1.SearchTerms.Filtering;
using Digdir.Domain.Dialogporten.Application.Features.V1.SearchTerms.Tokenizer;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.Localizations;
using MediatR;
using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.SearchTerms.Commands.GenerateSearchTerms;

public sealed class GenerateSearchTermsCommand : IRequest<GenerateSearchTermsResult>, IFeatureMetricServiceResourceIgnoreRequest
{
    public int? SampleSize { get; init; }
    public int? PoolRows { get; init; }
    public int? MinLength { get; init; }
    public IReadOnlyList<string>? Languages { get; init; }
}

[GenerateOneOf]
public sealed partial class GenerateSearchTermsResult : OneOfBase<Success, ValidationError>;

internal sealed partial class GenerateSearchTermsCommandHandler : IRequestHandler<GenerateSearchTermsCommand, GenerateSearchTermsResult>
{
    private const int DefaultSampleSize = 3;
    private const int DefaultPoolRows = 150_000;
    private const int DefaultMinLength = 5;
    private static readonly string[] DefaultLanguages = ["nb", "nn", "en"];
    private const double MinTableSamplePercent = 0.001;
    private const double MaxTableSamplePercent = 5.0;

    private readonly ISearchTermsSamplingRepository _repository;
    private readonly ISearchTermsTokenizer _tokenizer;
    private readonly ISearchTermsFilter _filter;
    private readonly IClock _clock;
    private readonly ILogger<GenerateSearchTermsCommandHandler> _logger;

    public GenerateSearchTermsCommandHandler(
        ISearchTermsSamplingRepository repository,
        ISearchTermsTokenizer tokenizer,
        ISearchTermsFilter filter,
        IClock clock,
        ILogger<GenerateSearchTermsCommandHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(tokenizer);
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(logger);
        _repository = repository;
        _tokenizer = tokenizer;
        _filter = filter;
        _clock = clock;
        _logger = logger;
    }

    public async Task<GenerateSearchTermsResult> Handle(GenerateSearchTermsCommand request, CancellationToken ct)
    {
        var sampleSize = request.SampleSize ?? DefaultSampleSize;
        var poolRows = request.PoolRows ?? DefaultPoolRows;
        var minLength = request.MinLength ?? DefaultMinLength;
        var languages = (request.Languages ?? DefaultLanguages)
            .Select(Localization.NormalizeCultureCode)
            .OfType<string>()
            .Where(x => x.Length > 0)
            .ToHashSet(StringComparer.Ordinal);

#pragma warning disable CA1873 // joining 3-4 language codes is negligible; only runs once per command invocation
        LogStarted(sampleSize, poolRows, minLength, string.Join(",", languages));
#pragma warning restore CA1873

        var totalRows = await _repository.EstimateTotalRowCountAsync(ct);
        LogTotalRows(totalRows);

        var resources = await _repository.EnumerateServiceResourcesAsync(ct);
        LogEnumeratedResources(resources.Count);

        if (resources.Count == 0)
        {
            // Don't wipe a previously-good persisted set on a degenerate run; leave existing data in place.
            _logger.LogWarning("No service resources found; leaving any existing search terms untouched.");
            return new Success();
        }

        // Stage A: global TABLESAMPLE pool.
        var percent = totalRows > 0
            ? Math.Clamp((double)poolRows / totalRows * 100d, MinTableSamplePercent, MaxTableSamplePercent)
            : MaxTableSamplePercent;
        LogStageAStarting(percent);

        var pool = await _repository.SampleViaTableSampleAsync(percent, ct);
        LogStageAPool(pool.Count);

        // Collect the full pool per resource, then sort each bucket by ContentUpdatedAt DESC
        // and keep the top N. This biases toward recently-updated content for autocomplete
        // freshness; the trade-off (older vocabulary under-represented) is accepted.
        var poolByResource = new Dictionary<string, List<SampledDialogIdentity>>(StringComparer.Ordinal);
        foreach (var row in pool)
        {
            if (!poolByResource.TryGetValue(row.ServiceResource, out var list))
            {
                list = [];
                poolByResource[row.ServiceResource] = list;
            }
            list.Add(row);
        }
        var samplesByResource = new Dictionary<string, List<Guid>>(StringComparer.Ordinal);
        foreach (var (resource, bucket) in poolByResource)
        {
            samplesByResource[resource] = bucket
                .OrderByDescending(r => r.ContentUpdatedAt)
                .Take(sampleSize)
                .Select(r => r.Id)
                .ToList();
        }

        // Stage B: fill missing resources via direct index lookup.
        var stageBHits = 0;
        foreach (var resource in resources)
        {
            samplesByResource.TryGetValue(resource, out var existing);
            var have = existing?.Count ?? 0;
            if (have >= sampleSize)
            {
                continue;
            }
            var needed = sampleSize - have;
            var extra = await _repository.SampleByResourceAsync(resource, needed, ct);
            if (extra.Count == 0)
            {
                continue;
            }
            stageBHits++;
            if (existing is null)
            {
                samplesByResource[resource] = extra.ToList();
            }
            else
            {
                foreach (var id in extra)
                {
                    if (!existing.Contains(id))
                    {
                        existing.Add(id);
                    }
                }
            }
        }
        LogStageBHits(stageBHits);

        var allIds = samplesByResource.Values.SelectMany(x => x).Distinct().ToList();
        if (allIds.Count == 0)
        {
            _logger.LogWarning("No sampled dialog IDs after Stage A/B; leaving any existing search terms untouched.");
            return new Success();
        }

        // Hydrate in chunks to avoid oversized parameter arrays.
        const int hydrationBatchSize = 1000;
        var contentById = new Dictionary<Guid, SampledDialogContent>();
        for (var i = 0; i < allIds.Count; i += hydrationBatchSize)
        {
            var batch = allIds.GetRange(i, Math.Min(hydrationBatchSize, allIds.Count - i));
            var rows = await _repository.FetchContentAsync(batch, ct);
            foreach (var row in rows)
            {
                contentById[row.Id] = row;
            }
        }
        LogHydrated(contentById.Count);

        // Stage 1: per-(resource, language) strict intersection + filter. Collect surviving
        // surface forms; stemming happens in bulk afterward (one SQL round-trip per language).
        var perResourceLanguageSurvivors = new Dictionary<(string Resource, string Language), HashSet<string>>();
        var statsByLanguage = languages.ToDictionary(
            lang => lang,
            _ => new LanguageStats(),
            StringComparer.Ordinal);
        var resourcesProcessed = 0;

        foreach (var (resource, ids) in samplesByResource)
        {
            resourcesProcessed++;
            foreach (var language in languages)
            {
                var intersection = ComputePerLanguageIntersection(language, ids, contentById);
                if (intersection.Count == 0)
                {
                    continue;
                }

                var stats = statsByLanguage[language];
                stats.TermsBeforeFilter += intersection.Count;

                var kept = new HashSet<string>(StringComparer.Ordinal);
                foreach (var word in intersection)
                {
                    if (_filter.ShouldKeep(word, minLength))
                    {
                        kept.Add(word);
                    }
                }
                if (kept.Count == 0)
                {
                    continue;
                }
                stats.TermsAfterFilter += kept.Count;
                perResourceLanguageSurvivors[(resource, language)] = kept;
            }
        }

        // Stage 2: bulk stem per language via Postgres ts_lexize (same dictionary the search side uses).
        var stemsByLanguage = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);
        foreach (var language in languages)
        {
            var distinctWords = perResourceLanguageSurvivors
                .Where(kv => kv.Key.Language == language)
                .SelectMany(kv => kv.Value)
                .ToHashSet(StringComparer.Ordinal);
            if (distinctWords.Count == 0)
            {
                stemsByLanguage[language] = new Dictionary<string, string>(StringComparer.Ordinal);
                continue;
            }
            var dictionary = ResolveStemDictionary(language);
            LogStemmingLanguage(language, dictionary, distinctWords.Count);
            stemsByLanguage[language] = await _repository.StemAsync(dictionary, distinctWords, ct);
        }

        // Stage 3a: build a GLOBAL stem→canonical map per language across ALL surviving words.
        // This must be global (not per-resource) so the same stem yields the same surface form
        // everywhere — otherwise resource A emits 'virksomhet' and resource B emits 'virksomheten'
        // for the same underlying stem 'virksom', creating duplicate suggestions in the search-term list.
        var canonicalByStemByLanguage = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        foreach (var language in languages)
        {
            var stemMap = stemsByLanguage[language];
            var canonical = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (key, survivors) in perResourceLanguageSurvivors)
            {
                if (key.Language != language)
                {
                    continue;
                }
                foreach (var word in survivors)
                {
                    var stemKey = stemMap.TryGetValue(word, out var stem) ? stem : word;
                    if (!canonical.TryGetValue(stemKey, out var current) || IsBetterCanonical(word, current))
                    {
                        canonical[stemKey] = word;
                    }
                }
            }
            canonicalByStemByLanguage[language] = canonical;
        }

        // Stage 3b: per (resource, language), map each survivor to its (global) canonical form,
        // dedupe within the resource, and accumulate into the inverted index. Values are HashSets
        // of unprefixed resource IDs — the same (canonical, language, resource) triple may be hit
        // multiple times if several surviving surface forms collapse to the same canonical for
        // that resource.
        var wordIndex = new Dictionary<(string Word, string Language), HashSet<string>>();
        foreach (var ((resource, language), survivors) in perResourceLanguageSurvivors)
        {
            var stemMap = stemsByLanguage[language];
            var canonical = canonicalByStemByLanguage[language];
            var canonicalsForResource = new HashSet<string>(StringComparer.Ordinal);
            foreach (var word in survivors)
            {
                var stemKey = stemMap.TryGetValue(word, out var stem) ? stem : word;
                var canon = canonical.TryGetValue(stemKey, out var c) ? c : word;
                canonicalsForResource.Add(canon);
            }

            var unprefixed = StripResourcePrefix(resource);
            statsByLanguage[language].TermsAfterStemCollapse += canonicalsForResource.Count;
            statsByLanguage[language].ResourcesWithSurvivingWords++;

            foreach (var canon in canonicalsForResource)
            {
                var key = (canon, language);
                if (!wordIndex.TryGetValue(key, out var set))
                {
                    set = new HashSet<string>(StringComparer.Ordinal);
                    wordIndex[key] = set;
                }
                set.Add(unprefixed);
            }
        }

        var distinctCanonicalByLanguage = wordIndex.Keys
            .GroupBy(k => k.Language, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        foreach (var language in languages.OrderBy(x => x, StringComparer.Ordinal))
        {
            var stats = statsByLanguage[language];
            var distinct = distinctCanonicalByLanguage.TryGetValue(language, out var n) ? n : 0;
            LogPerLanguageStats(
                language,
                stats.TermsBeforeFilter,
                stats.TermsAfterFilter,
                stats.TermsAfterStemCollapse,
                distinct,
                stats.ResourcesWithSurvivingWords);
        }
        LogIntersectionSummary(resourcesProcessed, wordIndex.Count);

        // Pivot the inverted index into one terse document per configured language and persist
        // them atomically. All documents share a single generatedAt so the served ETag / Last-Modified
        // is consistent across languages. Languages with no surviving words get an empty document
        // (so the endpoint serves an empty list rather than 404).
        var generatedAt = _clock.UtcNowOffset;
        var documents = BuildDocuments(languages, wordIndex);
        await _repository.ReplaceAsync(documents, generatedAt, ct);
        LogPersisted(documents.Count, wordIndex.Count, generatedAt);

        return new Success();
    }

    private static List<SearchTermListDocument> BuildDocuments(
        HashSet<string> languages,
        Dictionary<(string Word, string Language), HashSet<string>> wordIndex)
    {
        var documents = new List<SearchTermListDocument>(languages.Count);
        foreach (var language in languages.OrderBy(x => x, StringComparer.Ordinal))
        {
            var words = wordIndex
                .Where(kv => kv.Key.Language == language)
                .OrderBy(kv => kv.Key.Word, StringComparer.Ordinal)
                .Select(kv => new SearchTermJson(
                    kv.Key.Word,
                    kv.Value.OrderBy(x => x, StringComparer.Ordinal).ToArray()))
                .ToArray();
            var wordsJson = JsonSerializer.Serialize(words);
            documents.Add(new SearchTermListDocument(language, wordsJson));
        }
        return documents;
    }

    // Terse wire/storage shape: { "w": canonical word, "s": [sorted unprefixed resource ids] }.
    private sealed record SearchTermJson(
        [property: System.Text.Json.Serialization.JsonPropertyName("w")] string Word,
        [property: System.Text.Json.Serialization.JsonPropertyName("s")] IReadOnlyList<string> Resources);

    private static string ResolveStemDictionary(string language) => language switch
    {
        "nb" or "nn" => "norwegian_stem",
        "en" => "english_stem",
        _ => "simple"
    };

    private static bool IsBetterCanonical(string candidate, string current)
    {
        if (candidate.Length != current.Length)
        {
            return candidate.Length < current.Length;
        }
        return StringComparer.Ordinal.Compare(candidate, current) < 0;
    }

    private HashSet<string> ComputePerLanguageIntersection(
        string language,
        List<Guid> ids,
        Dictionary<Guid, SampledDialogContent> contentById)
    {
        HashSet<string>? intersection = null;
        foreach (var id in ids)
        {
            if (!contentById.TryGetValue(id, out var content))
            {
                return [];
            }
            var tokens = new HashSet<string>(StringComparer.Ordinal);
            foreach (var loc in content.Localizations)
            {
                if (loc.LanguageCode != language)
                {
                    continue;
                }
                foreach (var token in _tokenizer.Tokenize(loc.Value))
                {
                    tokens.Add(token);
                }
            }
            if (tokens.Count == 0)
            {
                // Strict per-language rule: a sample with no content for this language collapses the intersection.
                return [];
            }
            if (intersection is null)
            {
                intersection = tokens;
            }
            else
            {
                intersection.IntersectWith(tokens);
            }
            if (intersection.Count == 0)
            {
                return intersection;
            }
        }
        return intersection ?? [];
    }

    private static string StripResourcePrefix(string serviceResource) =>
        serviceResource.StartsWith(Constants.ServiceResourcePrefix, StringComparison.Ordinal)
            ? serviceResource[Constants.ServiceResourcePrefix.Length..]
            : serviceResource;

    private sealed class LanguageStats
    {
        public long TermsBeforeFilter { get; set; }
        public long TermsAfterFilter { get; set; }
        public long TermsAfterStemCollapse { get; set; }
        public int ResourcesWithSurvivingWords { get; set; }
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "GenerateSearchTerms started. SampleSize={SampleSize}, PoolRows={PoolRows}, MinLength={MinLength}, Languages={Languages}")]
    private partial void LogStarted(int sampleSize, int poolRows, int minLength, string languages);

    [LoggerMessage(Level = LogLevel.Information, Message = "Estimated total Dialog rows: {TotalRows}")]
    private partial void LogTotalRows(long totalRows);

    [LoggerMessage(Level = LogLevel.Information, Message = "Enumerated {Count} distinct service resources via loose index scan.")]
    private partial void LogEnumeratedResources(int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stage A: TABLESAMPLE SYSTEM({Percent:0.######})")]
    private partial void LogStageAStarting(double percent);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stage A pool size: {Count} rows")]
    private partial void LogStageAPool(int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stage B fallback triggered for {Count} resources")]
    private partial void LogStageBHits(int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Hydrated {Count} dialogs (title/summary).")]
    private partial void LogHydrated(int count);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Stemming language [{Language}] via dict={Dictionary}: {WordCount} distinct surviving words to stem.")]
    private partial void LogStemmingLanguage(string language, string dictionary, int wordCount);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Per-language [{Language}]: TermsBeforeFilter={TermsBeforeFilter}, TermsAfterFilter={TermsAfterFilter}, TermsAfterStemCollapse={TermsAfterStemCollapse}, DistinctCanonicalWords={DistinctCanonicalWords}, ResourcesWithSurvivingWords={ResourcesWithSurvivingWords}")]
    private partial void LogPerLanguageStats(string language, long termsBeforeFilter, long termsAfterFilter, long termsAfterStemCollapse, int distinctCanonicalWords, int resourcesWithSurvivingWords);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Intersection complete. ResourcesProcessed={ResourcesProcessed}, TotalEntries={TotalEntries} ((word, language) pairs).")]
    private partial void LogIntersectionSummary(int resourcesProcessed, int totalEntries);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Search terms persisted: {LanguageCount} language documents, {EntryCount} total (word, language) entries, GeneratedAt={GeneratedAt}.")]
    private partial void LogPersisted(int languageCount, int entryCount, DateTimeOffset generatedAt);
}
