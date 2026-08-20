using System.Text.Json;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Features.V1.SearchTerms.Commands.GenerateSearchTerms;
using Digdir.Domain.Dialogporten.Application.Features.V1.SearchTerms.Filtering;
using Digdir.Domain.Dialogporten.Application.Features.V1.SearchTerms.Tokenizer;
using Digdir.Domain.Dialogporten.Application.Unit.Tests.Common;
using Digdir.Domain.Dialogporten.Domain.SearchTerms;
using Xunit;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Features.V1.SearchTerms;

public sealed class GenerateSearchTermsCommandHandlerTests
{
    // Test-owned constants: the intersection/filter behavior under test is defined relative to
    // these, so they are pinned here rather than read from the handler's defaults.
    private const int SampleSize = 3;
    private const int MinLength = 5;
    private const string ResourcePrefix = "urn:altinn:resource:";
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 16, 8, 15, 15, TimeSpan.Zero);

    private readonly FakeSamplingRepository _repository = new();

    [Fact]
    public async Task Only_Words_Common_To_All_Samples_Survive()
    {
        // 'skattemelding' appears in every sampled dialog; the person names appear in one each.
        AddDialog("skatt-app", "skattemelding for kari nordmann");
        AddDialog("skatt-app", "skattemelding for petter hansen");
        AddDialog("skatt-app", "skattemelding purremelding");

        await Handle(Command());

        var words = NbWords();
        words.Should().ContainSingle();
        words[0].Word.Should().Be("skattemelding");
        words[0].Resources.Should().Equal("skatt-app");
    }

    [Fact]
    public async Task Resources_With_Fewer_Dialogs_Than_SampleSize_Are_Skipped_Entirely()
    {
        AddDialog("full-app", "skattemelding kari", "skattemelding petter", "skattemelding purring");
        // Two dialogs < SampleSize: intersecting them would leak their shared vocabulary
        // (including the personal name), so the resource must contribute nothing at all.
        AddDialog("sparse-app", "arbeidsavklaring kari nordmann", "arbeidsavklaring kari nordmann");

        await Handle(Command());

        NbWords().Select(w => w.Word).Should().Equal("skattemelding");
    }

    [Fact]
    public async Task Sample_Without_Content_For_A_Language_Collapses_That_Language_For_The_Resource()
    {
        AddDialogLocalized("skatt-app", ("nb", "skattemelding levert"), ("en", "taxreport delivered"));
        AddDialogLocalized("skatt-app", ("nb", "skattemelding purring"), ("en", "taxreport reminder"));
        AddDialogLocalized("skatt-app", ("nb", "skattemelding klage")); // no English content

        await Handle(Command(languages: ["nb", "en"]));

        NbWords().Select(w => w.Word).Should().Equal("skattemelding");
        // 'taxreport' was common to the two dialogs that had English content, but the strict rule
        // requires every sample to carry the language. The language still gets a (empty) document.
        Words("en").Should().BeEmpty();
    }

    [Fact]
    public async Task Stopwords_And_Short_Words_Are_Filtered_From_Survivors()
    {
        // All three words survive the intersection; 'altinn' is stoplisted and 'sak' is below
        // MinLength, so only 'skattemelding' may be published.
        AddDialog("skatt-app",
            "altinn sak skattemelding en",
            "altinn sak skattemelding to",
            "altinn sak skattemelding tre");

        await Handle(Command());

        NbWords().Select(w => w.Word).Should().Equal("skattemelding");
    }

    [Fact]
    public async Task Inflections_Of_Stoplisted_Words_Are_Removed_Via_Stem_Matching()
    {
        // 'innsendinger' is not in the stoplist as a surface form, but stems to the same lexeme
        // as the stoplisted 'innsending' — the stem stage must catch what exact matching cannot.
        _repository.SetStems("norwegian_stem",
            ("innsending", "innsend"),
            ("innsendinger", "innsend"),
            ("skattemelding", "skattemeld"));
        AddDialog("skatt-app",
            "innsendinger skattemelding en",
            "innsendinger skattemelding to",
            "innsendinger skattemelding tre");

        await Handle(Command());

        NbWords().Select(w => w.Word).Should().Equal("skattemelding");
    }

    [Fact]
    public async Task Words_Sharing_A_Stem_Collapse_To_One_Canonical_Form_Across_Resources()
    {
        _repository.SetStems("norwegian_stem",
            ("virksomhet", "virksom"),
            ("virksomheten", "virksom"));
        AddDialog("app-a", "virksomheten registrert", "virksomheten endret", "virksomheten slettet");
        AddDialog("app-b", "virksomhet registrert", "virksomhet endret", "virksomhet slettet");

        await Handle(Command());

        // The canonical form is global (shortest surface form wins), so both resources publish
        // the same word instead of one suggestion per inflection.
        var virksomhet = NbWords().Single(w => w.Word == "virksomhet");
        virksomhet.Resources.Should().Equal("app-a", "app-b");
        NbWords().Should().NotContain(w => w.Word == "virksomheten");
    }

    [Fact]
    public async Task Resources_Owned_By_Excluded_Orgs_Contribute_Nothing()
    {
        _repository.OrgByResource[ResourcePrefix + "test-app"] = "ttd";
        AddDialog("skatt-app", "skattemelding en", "skattemelding to", "skattemelding tre");
        AddDialog("test-app", "testeord en", "testeord to", "testeord tre");

        await Handle(Command(excludedOrgs: ["ttd"]));

        NbWords().Select(w => w.Word).Should().Equal("skattemelding");
    }

    [Fact]
    public async Task Documents_Are_Deterministic_Sorted_And_Stamped_With_The_Clock()
    {
        AddDialog("app-b", "felles betaord", "felles betaord", "felles betaord");
        AddDialog("app-a", "felles alfaord", "felles alfaord", "felles alfaord");

        await Handle(Command(languages: ["nb", "en"]));

        _repository.ReplacedGeneratedAt.Should().Be(FixedNow);
        // One document per configured language, ordinal-ordered, including an empty one for a
        // language with no survivors (served as an empty list rather than 404).
        _repository.ReplacedDocuments!.Select(d => d.Language).Should().Equal("en", "nb");

        var words = NbWords();
        words.Select(w => w.Word).Should().Equal("alfaord", "betaord", "felles");
        words.Single(w => w.Word == "felles").Resources.Should().Equal("app-a", "app-b");
    }

    [Fact]
    public async Task OutputPath_Writes_Jsonl_Instead_Of_Persisting()
    {
        AddDialog("skatt-app", "skattemelding en", "skattemelding to", "skattemelding tre");
        var path = Path.Combine(Path.GetTempPath(), $"searchterms-{Guid.NewGuid():N}.jsonl");

        try
        {
            await Handle(Command(outputPath: path));

            _repository.ReplacedDocuments.Should().BeNull();
            var lines = await File.ReadAllLinesAsync(path, TestContext.Current.CancellationToken);
            lines.Should().ContainSingle();
            using var document = JsonDocument.Parse(lines[0]);
            document.RootElement.GetProperty("language").GetString().Should().Be("nb");
            document.RootElement.GetProperty("generatedAt").GetDateTimeOffset().Should().Be(FixedNow);
            document.RootElement.GetProperty("words").EnumerateArray()
                .Select(w => w.GetProperty("w").GetString())
                .Should().Equal("skattemelding");
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static GenerateSearchTermsCommand Command(
        string[]? languages = null,
        string[]? excludedOrgs = null,
        string? outputPath = null) => new()
        {
            SampleSize = SampleSize,
            PoolRows = 100,
            MinLength = MinLength,
            Languages = languages ?? ["nb"],
            ExcludedOrgs = excludedOrgs ?? [],
            OutputPath = outputPath
        };

    private async Task Handle(GenerateSearchTermsCommand command)
    {
        var handler = new GenerateSearchTermsCommandHandler(
            _repository,
            new SearchTermsTokenizer(),
            new SearchTermsFilter(),
            new FixedClock(FixedNow),
            new TestLogger<GenerateSearchTermsCommandHandler>());

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);
        result.IsT0.Should().BeTrue("generation should succeed: {0}", result.Value);
    }

    private void AddDialog(string unprefixedResource, params string[] norwegianTitles)
    {
        foreach (var title in norwegianTitles)
        {
            AddDialogLocalized(unprefixedResource, ("nb", title));
        }
    }

    private void AddDialogLocalized(string unprefixedResource, params (string Language, string Value)[] localizations)
        => _repository.Dialogs.Add(new SampledDialogContent(
            Guid.NewGuid(),
            ResourcePrefix + unprefixedResource,
            localizations.Select(l => new SampledDialogLocalization(l.Language, l.Value)).ToList()));

    private List<SearchTermEntry> NbWords() => Words("nb");

    private List<SearchTermEntry> Words(string language)
    {
        _repository.ReplacedDocuments.Should().NotBeNull();
        var document = _repository.ReplacedDocuments!.Single(d => d.Language == language);
        return JsonSerializer.Deserialize<List<SearchTermEntry>>(document.WordsJson)!;
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNowOffset => now;
        public DateTimeOffset NowOffset => now;
        public DateTime UtcNow => now.UtcDateTime;
        public DateTime Now => now.LocalDateTime;
    }

    // In-memory stand-in for the SQL sampling queries. Sampling is exhaustive rather than random:
    // Stage A returns every dialog (minus excluded orgs, which the real TABLESAMPLE query also
    // filters SQL-side), so a resource with exactly SampleSize dialogs is sampled deterministically.
    private sealed class FakeSamplingRepository : ISearchTermsSamplingRepository
    {
        public List<SampledDialogContent> Dialogs { get; } = [];
        public Dictionary<string, string> OrgByResource { get; } = new(StringComparer.Ordinal);
        public IReadOnlyList<SearchTermListDocument>? ReplacedDocuments { get; private set; }
        public DateTimeOffset? ReplacedGeneratedAt { get; private set; }

        private readonly Dictionary<string, Dictionary<string, string>> _stemsByDictionary = new(StringComparer.Ordinal);

        public void SetStems(string dictionary, params (string Word, string Stem)[] stems)
            => _stemsByDictionary[dictionary] = stems.ToDictionary(s => s.Word, s => s.Stem, StringComparer.Ordinal);

        private string OrgOf(string serviceResource) => OrgByResource.GetValueOrDefault(serviceResource, "digdir");

        public Task<long> EstimateTotalRowCountAsync(CancellationToken ct)
            => Task.FromResult((long)Dialogs.Count);

        public Task<IReadOnlyList<string>> EnumerateServiceResourcesAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyList<string>>(Dialogs
                .Select(d => d.ServiceResource)
                .Distinct(StringComparer.Ordinal)
                .ToList());

        public Task<IReadOnlyList<SampledDialogIdentity>> SampleViaTableSampleAsync(
            double percent, IReadOnlyCollection<string> excludedOrgs, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<SampledDialogIdentity>>(Dialogs
                .Where(d => !excludedOrgs.Contains(OrgOf(d.ServiceResource)))
                .Select(d => new SampledDialogIdentity(d.Id, d.ServiceResource))
                .ToList());

        public Task<IReadOnlyDictionary<string, string>> GetResourceOrgsAsync(
            IReadOnlyCollection<string> serviceResources, CancellationToken ct)
            => Task.FromResult<IReadOnlyDictionary<string, string>>(
                serviceResources.ToDictionary(r => r, OrgOf, StringComparer.Ordinal));

        public Task<IReadOnlyDictionary<string, IReadOnlyList<Guid>>> SampleByResourcesAsync(
            IReadOnlyCollection<string> serviceResources, int n, IReadOnlyCollection<Guid> excludeDialogIds, CancellationToken ct)
            => Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<Guid>>>(serviceResources.ToDictionary(
                r => r,
                IReadOnlyList<Guid> (r) => Dialogs
                    .Where(d => d.ServiceResource == r && !excludeDialogIds.Contains(d.Id))
                    .Take(n)
                    .Select(d => d.Id)
                    .ToList(),
                StringComparer.Ordinal));

        public Task<IReadOnlyList<SampledDialogContent>> FetchContentAsync(
            IReadOnlyCollection<Guid> dialogIds, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<SampledDialogContent>>(Dialogs
                .Where(d => dialogIds.Contains(d.Id))
                .ToList());

        public Task<IReadOnlyDictionary<string, string>> StemAsync(
            string dictionary, IReadOnlyCollection<string> words, CancellationToken ct)
        {
            var known = _stemsByDictionary.GetValueOrDefault(dictionary);
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var word in words)
            {
                if (known?.TryGetValue(word, out var stem) == true)
                {
                    result[word] = stem;
                }
            }
            return Task.FromResult<IReadOnlyDictionary<string, string>>(result);
        }

        public Task ReplaceAsync(IReadOnlyList<SearchTermListDocument> documents, DateTimeOffset generatedAt, CancellationToken ct)
        {
            ReplacedDocuments = documents;
            ReplacedGeneratedAt = generatedAt;
            return Task.CompletedTask;
        }
    }
}
