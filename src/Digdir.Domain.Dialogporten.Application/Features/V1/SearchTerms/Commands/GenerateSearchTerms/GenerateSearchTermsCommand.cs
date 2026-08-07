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

    /// <summary>
    /// Service owner org codes whose dialogs are excluded from sampling entirely. Defaults to
    /// the test-only service owners (acn, bft, ttd). Pass an empty list to disable exclusion.
    /// </summary>
    public IReadOnlyList<string>? ExcludedOrgs { get; init; }

    /// <summary>
    /// When set, the generated documents are written as JSONL to this path instead of being
    /// persisted to the database. Lets the command run against read-only environments (or
    /// environments without the SearchTermList table) for output inspection.
    /// </summary>
    public string? OutputPath { get; init; }
}

[GenerateOneOf]
public sealed partial class GenerateSearchTermsResult : OneOfBase<Success, ValidationError>;

internal sealed partial class GenerateSearchTermsCommandHandler : IRequestHandler<GenerateSearchTermsCommand, GenerateSearchTermsResult>
{
    private const int DefaultSampleSize = 7;
    private const int DefaultPoolRows = 150_000;
    private const int DefaultMinLength = 5;
    private static readonly string[] DefaultLanguages = ["nb", "nn", "en"];
    private static readonly string[] DefaultExcludedOrgs = ["acn", "bft", "ttd"];
    private const int OrgProbeBatchSize = 1000;
    private const int StageBBatchSize = 200;
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

    public async Task<GenerateSearchTermsResult> Handle(GenerateSearchTermsCommand request, CancellationToken cancellationToken)
    {
        var sampleSize = request.SampleSize ?? DefaultSampleSize;
        var poolRows = request.PoolRows ?? DefaultPoolRows;
        var minLength = request.MinLength ?? DefaultMinLength;
        var languages = (request.Languages ?? DefaultLanguages)
            .Select(Localization.NormalizeCultureCode)
            .OfType<string>()
            .Where(x => x.Length > 0)
            .ToHashSet(StringComparer.Ordinal);
        var excludedOrgs = (request.ExcludedOrgs ?? DefaultExcludedOrgs)
            .Select(x => x.Trim().ToLowerInvariant())
            .Where(x => x.Length > 0)
            .ToHashSet(StringComparer.Ordinal);

#pragma warning disable CA1873 // joining a handful of codes is negligible; only runs once per command invocation
        LogStarted(sampleSize, poolRows, minLength, string.Join(",", languages), string.Join(",", excludedOrgs));
#pragma warning restore CA1873

        var totalRows = await _repository.EstimateTotalRowCountAsync(cancellationToken);
        LogTotalRows(totalRows);

        var resources = await _repository.EnumerateServiceResourcesAsync(cancellationToken);
        LogEnumeratedResources(resources.Count);

        if (excludedOrgs.Count > 0)
        {
            resources = await FilterExcludedOrgResourcesAsync(resources, excludedOrgs, cancellationToken);
        }

        if (resources.Count == 0)
        {
            // Don't wipe a previously-good persisted set on a degenerate run; leave existing data in place.
            _logger.LogWarning("No service resources found; leaving any existing search terms untouched.");
            return new Success();
        }

        var samplesByResource = await SampleDialogsAsync(resources, totalRows, sampleSize, poolRows, excludedOrgs, cancellationToken);

        var allIds = samplesByResource.Values.SelectMany(x => x).Distinct().ToList();
        if (allIds.Count == 0)
        {
            _logger.LogWarning("No sampled dialog IDs after Stage A/B; leaving any existing search terms untouched.");
            return new Success();
        }

        var contentById = await HydrateContentAsync(allIds, cancellationToken);
        LogHydrated(contentById.Count);

        var statsByLanguage = languages.ToDictionary(
            lang => lang,
            _ => new LanguageStats(),
            StringComparer.Ordinal);

        var survivors = ComputeSurvivors(samplesByResource, languages, minLength, contentById, statsByLanguage);
        var stemsByLanguage = await StemSurvivorsAsync(languages, survivors, cancellationToken);
        await RemoveStemStoplistedSurvivorsAsync(languages, survivors, stemsByLanguage, cancellationToken);
        var canonicalByStemByLanguage = BuildCanonicalMaps(languages, stemsByLanguage, survivors);
        var wordIndex = BuildWordIndex(survivors, stemsByLanguage, canonicalByStemByLanguage, statsByLanguage);

        LogRunStatistics(languages, statsByLanguage, wordIndex, samplesByResource.Count);

        // Pivot the inverted index into one terse document per configured language and persist
        // them atomically. All documents share a single generatedAt so the served ETag / Last-Modified
        // is consistent across languages. Languages with no surviving words get an empty document
        // (so the endpoint serves an empty list rather than 404).
        var generatedAt = _clock.UtcNowOffset;
        var documents = BuildDocuments(languages, wordIndex);
        if (request.OutputPath is not null)
        {
            await WriteDocumentsToFileAsync(request.OutputPath, documents, generatedAt, cancellationToken);
            LogWroteToFile(documents.Count, wordIndex.Count, request.OutputPath);
        }
        else
        {
            await _repository.ReplaceAsync(documents, generatedAt, cancellationToken);
            LogPersisted(documents.Count, wordIndex.Count, generatedAt);
        }

        return new Success();
    }

    // A resource has exactly one owning service owner, so exclusion is resolved once per
    // resource (one cheap LIMIT 1 probe each, batched into round-trips of OrgProbeBatchSize)
    // instead of filtering "Org" per dialog row in the sampling queries — the per-row filter
    // forced a heap fetch for every candidate row, which dominated Stage B on large tables.
    private async Task<IReadOnlyList<string>> FilterExcludedOrgResourcesAsync(
        IReadOnlyList<string> resources,
        IReadOnlyCollection<string> excludedOrgs,
        CancellationToken cancellationToken)
    {
        var kept = new List<string>(resources.Count);
        var excluded = 0;
        foreach (var chunk in resources.Chunk(OrgProbeBatchSize))
        {
            var orgByResource = await _repository.GetResourceOrgsAsync(chunk, cancellationToken);
            foreach (var resource in chunk)
            {
                if (orgByResource.TryGetValue(resource, out var org)
                    && excludedOrgs.Contains(org.ToLowerInvariant()))
                {
                    excluded++;
                }
                else
                {
                    kept.Add(resource);
                }
            }
        }
        LogExcludedOrgResources(excluded);
        return kept;
    }

    private async Task<Dictionary<string, List<Guid>>> SampleDialogsAsync(
        IReadOnlyList<string> resources,
        long totalRows,
        int sampleSize,
        int poolRows,
        IReadOnlyCollection<string> excludedOrgs,
        CancellationToken cancellationToken)
    {
        // Stage A: global TABLESAMPLE pool.
        var percent = totalRows > 0
            ? Math.Clamp((double)poolRows / totalRows * 100d, MinTableSamplePercent, MaxTableSamplePercent)
            : MaxTableSamplePercent;
        LogStageAStarting(percent);

        var pool = await _repository.SampleViaTableSampleAsync(percent, excludedOrgs, cancellationToken);
        LogStageAPool(pool.Count);

        // Pick N uniformly at random within each resource's pool bucket. Newest-first selection
        // (the original freshness bias) defeated the PII intersection heuristic: one real-world
        // case can emit a burst of near-identical dialogs (an estate settlement notifying every
        // heir, a migrated correspondence batch), and the newest N samples then all carry the same
        // person's name. Random selection restores the independence assumption the intersection
        // relies on. TABLESAMPLE rows also arrive in physical page order, so taking "any N"
        // without shuffling would reintroduce the same burst correlation.
        var samplesByResource = pool
            .GroupBy(r => r.ServiceResource, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(_ => Random.Shared.Next()).Take(sampleSize).Select(r => r.Id).ToList(),
                StringComparer.Ordinal);

        // Stage B: top up under-represented resources via per-resource random picks, excluding
        // ids Stage A already delivered so top-ups can't collide with them. Batched into one
        // LATERAL round-trip per chunk — prod has thousands of long-tail resources, and one
        // query per resource dominated the runtime (especially over a tunneled connection).
        var missingResources = resources
            .Where(r => (samplesByResource.GetValueOrDefault(r)?.Count ?? 0) < sampleSize)
            .ToList();
        var stageBHits = 0;
        foreach (var chunk in missingResources.Chunk(StageBBatchSize))
        {
            var excludeIds = chunk
                .SelectMany(r => samplesByResource.GetValueOrDefault(r) ?? [])
                .ToList();
            var extras = await _repository.SampleByResourcesAsync(chunk, sampleSize, excludeIds, cancellationToken);
            foreach (var (resource, ids) in extras)
            {
                samplesByResource.TryGetValue(resource, out var existing);
                var needed = sampleSize - (existing?.Count ?? 0);
                if (needed <= 0 || ids.Count == 0)
                {
                    continue;
                }
                stageBHits++;
                var take = ids.Take(needed);
                if (existing is null)
                {
                    samplesByResource[resource] = take.ToList();
                }
                else
                {
                    existing.AddRange(take);
                }
            }
        }
        LogStageBHits(stageBHits);

        // A resource must contribute a full set of sampleSize samples for the intersection
        // heuristic to have filtering power — intersecting over one or two dialogs leaks their
        // entire title/summary vocabulary (including personal names). Resources that couldn't be
        // sampled to the full N (fewer dialogs than N, or all their dialogs owned by excluded
        // orgs) are dropped from the run entirely.
        var undersampled = samplesByResource
            .Where(kv => kv.Value.Count < sampleSize)
            .Select(kv => kv.Key)
            .ToList();
        foreach (var resource in undersampled)
        {
            samplesByResource.Remove(resource);
        }
        LogSkippedUndersampled(undersampled.Count, sampleSize);

        return samplesByResource;
    }

    // Hydrate in chunks to avoid oversized parameter arrays.
    private async Task<Dictionary<Guid, SampledDialogContent>> HydrateContentAsync(
        List<Guid> allIds,
        CancellationToken cancellationToken)
    {
        const int hydrationBatchSize = 1000;
        var contentById = new Dictionary<Guid, SampledDialogContent>();
        for (var i = 0; i < allIds.Count; i += hydrationBatchSize)
        {
            var batch = allIds.GetRange(i, Math.Min(hydrationBatchSize, allIds.Count - i));
            var rows = await _repository.FetchContentAsync(batch, cancellationToken);
            foreach (var row in rows)
            {
                contentById[row.Id] = row;
            }
        }
        return contentById;
    }

    // Stage 1: per-(resource, language) strict intersection + filter. Collect surviving
    // surface forms; stemming happens in bulk afterward (one SQL round-trip per language).
    private Dictionary<(string Resource, string Language), HashSet<string>> ComputeSurvivors(
        Dictionary<string, List<Guid>> samplesByResource,
        HashSet<string> languages,
        int minLength,
        Dictionary<Guid, SampledDialogContent> contentById,
        Dictionary<string, LanguageStats> statsByLanguage)
    {
        var survivors = new Dictionary<(string Resource, string Language), HashSet<string>>();
        foreach (var (resource, ids) in samplesByResource)
        {
            foreach (var language in languages)
            {
                var intersection = ComputePerLanguageIntersection(language, ids, contentById);
                if (intersection.Count == 0)
                {
                    continue;
                }

                var stats = statsByLanguage[language];
                stats.TermsBeforeFilter += intersection.Count;

                var kept = intersection
                    .Where(word => _filter.ShouldKeep(word, minLength))
                    .ToHashSet(StringComparer.Ordinal);
                if (kept.Count == 0)
                {
                    continue;
                }
                stats.TermsAfterFilter += kept.Count;
                survivors[(resource, language)] = kept;
            }
        }
        return survivors;
    }

    // Stage 2: bulk stem per language via Postgres ts_lexize (same dictionary the search side uses).
    private async Task<Dictionary<string, IReadOnlyDictionary<string, string>>> StemSurvivorsAsync(
        HashSet<string> languages,
        Dictionary<(string Resource, string Language), HashSet<string>> survivors,
        CancellationToken cancellationToken)
    {
        var stemsByLanguage = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);
        foreach (var language in languages)
        {
            var distinctWords = survivors
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
            stemsByLanguage[language] = await _repository.StemAsync(dictionary, distinctWords, cancellationToken);
        }
        return stemsByLanguage;
    }

    // Stage 2b: drop survivors whose STEM matches a stoplisted stem. The exact-match check in
    // ComputeSurvivors can't catch inflections (stoplisted 'innsending' lets 'innsendingen'
    // through); stemming both sides with the same dictionary closes that gap without having to
    // enumerate every surface form in the stoplist files. Stopwords the dictionary doesn't
    // recognize are absent from the stem map and simply keep exact-match-only behavior.
    private async Task RemoveStemStoplistedSurvivorsAsync(
        HashSet<string> languages,
        Dictionary<(string Resource, string Language), HashSet<string>> survivors,
        Dictionary<string, IReadOnlyDictionary<string, string>> stemsByLanguage,
        CancellationToken cancellationToken)
    {
        var stopStemsByDictionary = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var language in languages)
        {
            var dictionary = ResolveStemDictionary(language);
            if (!stopStemsByDictionary.TryGetValue(dictionary, out var stopStems))
            {
                var stemmedStopwords = await _repository.StemAsync(dictionary, _filter.Stopwords, cancellationToken);
                stopStems = stemmedStopwords.Values.ToHashSet(StringComparer.Ordinal);
                stopStemsByDictionary[dictionary] = stopStems;
            }

            var stemMap = stemsByLanguage[language];
            var dropped = 0;
            foreach (var (key, words) in survivors)
            {
                if (key.Language != language)
                {
                    continue;
                }
                dropped += words.RemoveWhere(word =>
                    stemMap.TryGetValue(word, out var stem) && stopStems.Contains(stem));
            }
            if (dropped > 0)
            {
                LogStemStoplistDropped(dropped, language);
            }
        }

        // Prune emptied entries so they don't count as resources with surviving words downstream.
        foreach (var key in survivors.Where(kv => kv.Value.Count == 0).Select(kv => kv.Key).ToList())
        {
            survivors.Remove(key);
        }
    }

    // Stage 3a: build a GLOBAL stem→canonical map per language across ALL surviving words.
    // This must be global (not per-resource) so the same stem yields the same surface form
    // everywhere — otherwise resource A emits 'virksomhet' and resource B emits 'virksomheten'
    // for the same underlying stem 'virksom', creating duplicate suggestions in the search-term list.
    private static Dictionary<string, Dictionary<string, string>> BuildCanonicalMaps(
        HashSet<string> languages,
        Dictionary<string, IReadOnlyDictionary<string, string>> stemsByLanguage,
        Dictionary<(string Resource, string Language), HashSet<string>> survivors)
    {
        var canonicalByStemByLanguage = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        foreach (var language in languages)
        {
            var stemMap = stemsByLanguage[language];
            var canonical = new Dictionary<string, string>(StringComparer.Ordinal);
            var words = survivors
                .Where(kv => kv.Key.Language == language)
                .SelectMany(kv => kv.Value);
            foreach (var word in words)
            {
                var stemKey = stemMap.TryGetValue(word, out var stem) ? stem : word;
                if (!canonical.TryGetValue(stemKey, out var current) || IsBetterCanonical(word, current))
                {
                    canonical[stemKey] = word;
                }
            }
            canonicalByStemByLanguage[language] = canonical;
        }
        return canonicalByStemByLanguage;
    }

    // Stage 3b: per (resource, language), map each survivor to its (global) canonical form,
    // dedupe within the resource, and accumulate into the inverted index. Values are HashSets
    // of unprefixed resource IDs — the same (canonical, language, resource) triple may be hit
    // multiple times if several surviving surface forms collapse to the same canonical for
    // that resource.
    private static Dictionary<(string Word, string Language), HashSet<string>> BuildWordIndex(
        Dictionary<(string Resource, string Language), HashSet<string>> survivors,
        Dictionary<string, IReadOnlyDictionary<string, string>> stemsByLanguage,
        Dictionary<string, Dictionary<string, string>> canonicalByStemByLanguage,
        Dictionary<string, LanguageStats> statsByLanguage)
    {
        var wordIndex = new Dictionary<(string Word, string Language), HashSet<string>>();
        foreach (var ((resource, language), survivingWords) in survivors)
        {
            var canonicalsForResource = MapToCanonicalForms(
                survivingWords,
                stemsByLanguage[language],
                canonicalByStemByLanguage[language]);

            var unprefixed = StripResourcePrefix(resource);
            statsByLanguage[language].TermsAfterStemCollapse += canonicalsForResource.Count;
            statsByLanguage[language].ResourcesWithSurvivingWords++;

            foreach (var canon in canonicalsForResource)
            {
                if (!wordIndex.TryGetValue((canon, language), out var set))
                {
                    set = new HashSet<string>(StringComparer.Ordinal);
                    wordIndex[(canon, language)] = set;
                }
                set.Add(unprefixed);
            }
        }
        return wordIndex;
    }

    private static HashSet<string> MapToCanonicalForms(
        HashSet<string> words,
        IReadOnlyDictionary<string, string> stemMap,
        Dictionary<string, string> canonicalByStem)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var word in words)
        {
            var stemKey = stemMap.TryGetValue(word, out var stem) ? stem : word;
            result.Add(canonicalByStem.TryGetValue(stemKey, out var canonical) ? canonical : word);
        }
        return result;
    }

    private void LogRunStatistics(
        HashSet<string> languages,
        Dictionary<string, LanguageStats> statsByLanguage,
        Dictionary<(string Word, string Language), HashSet<string>> wordIndex,
        int resourcesProcessed)
    {
        var distinctCanonicalByLanguage = wordIndex.Keys
            .GroupBy(k => k.Language, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        foreach (var language in languages.OrderBy(x => x, StringComparer.Ordinal))
        {
            var stats = statsByLanguage[language];
            var distinct = distinctCanonicalByLanguage.GetValueOrDefault(language);
            LogPerLanguageStats(
                language,
                stats.TermsBeforeFilter,
                stats.TermsAfterFilter,
                stats.TermsAfterStemCollapse,
                distinct,
                stats.ResourcesWithSurvivingWords);
        }
        LogIntersectionSummary(resourcesProcessed, wordIndex.Count);
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

    // JSONL dump of exactly what ReplaceAsync would have persisted: one line per language document.
    // WordsJson is already-serialized JSON, so it is embedded via WriteRawValue without a re-parse.
    private static async Task WriteDocumentsToFileAsync(
        string path,
        IReadOnlyList<SearchTermListDocument> documents,
        DateTimeOffset generatedAt,
        CancellationToken cancellationToken)
    {
        await using var stream = File.Create(path);
        await using var writer = new Utf8JsonWriter(stream);
        foreach (var document in documents)
        {
            writer.WriteStartObject();
            writer.WriteString("language", document.Language);
            writer.WriteString("generatedAt", generatedAt);
            writer.WritePropertyName("words");
            writer.WriteRawValue(document.WordsJson);
            writer.WriteEndObject();
            await writer.FlushAsync(cancellationToken);
            stream.WriteByte((byte)'\n');
            writer.Reset();
        }
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
            var tokens = TokenizeLocalizations(content, language);
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

    private HashSet<string> TokenizeLocalizations(SampledDialogContent content, string language) =>
        content.Localizations
            .Where(loc => loc.LanguageCode == language)
            .SelectMany(loc => _tokenizer.Tokenize(loc.Value))
            .ToHashSet(StringComparer.Ordinal);

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
        Message = "GenerateSearchTerms started. SampleSize={SampleSize}, PoolRows={PoolRows}, MinLength={MinLength}, Languages={Languages}, ExcludedOrgs={ExcludedOrgs}")]
    private partial void LogStarted(int sampleSize, int poolRows, int minLength, string languages, string excludedOrgs);

    [LoggerMessage(Level = LogLevel.Information, Message = "Estimated total Dialog rows: {TotalRows}")]
    private partial void LogTotalRows(long totalRows);

    [LoggerMessage(Level = LogLevel.Information, Message = "Enumerated {Count} distinct service resources via loose index scan.")]
    private partial void LogEnumeratedResources(int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Excluded {Count} resources owned by excluded service owner orgs.")]
    private partial void LogExcludedOrgResources(int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stage A: TABLESAMPLE SYSTEM({Percent:0.######})")]
    private partial void LogStageAStarting(double percent);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stage A pool size: {Count} rows")]
    private partial void LogStageAPool(int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stage B fallback triggered for {Count} resources")]
    private partial void LogStageBHits(int count);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Skipped {Count} resources with fewer than {SampleSize} sampled dialogs (intersection would have no filtering power).")]
    private partial void LogSkippedUndersampled(int count, int sampleSize);

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
        Message = "Stem-based stoplist dropped {Count} surviving word occurrences for language [{Language}].")]
    private partial void LogStemStoplistDropped(int count, string language);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Search terms persisted: {LanguageCount} language documents, {EntryCount} total (word, language) entries, GeneratedAt={GeneratedAt}.")]
    private partial void LogPersisted(int languageCount, int entryCount, DateTimeOffset generatedAt);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Search terms written to {OutputPath} (database persistence skipped): {LanguageCount} language documents, {EntryCount} total (word, language) entries.")]
    private partial void LogWroteToFile(int languageCount, int entryCount, string outputPath);
}
