using System.Diagnostics.CodeAnalysis;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Domain.Dialogporten.Domain.SubjectResources;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using Digdir.Tool.Dialogporten.LargeDataSetSeeder.Common;
using Digdir.Tool.Dialogporten.LargeDataSetSeeder.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Refit;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder;

internal static class StaticStore
{
    private static Resource[]? _privResources;
    private static Resource[]? _daglResources;
    private static string[]? _norwegianWords;
    private static string[]? _englishWords;
    private static bool _isInitialized;

    public static readonly string[] FrequentWords =
    [
        "og", "i", "det", "på", "som", "er", "en", "til", "å", "han", "av",
        "for", "med", "at", "var", "de", "ikke", "den", "har", "jeg", "om",
        "et", "men", "så", "seg", "hun", "hadde", "fra", "vi", "du", "kan",
        "da", "ble", "ut", "skal", "vil", "ham", "etter", "over", "ved", "også",
        "bare", "eller", "sa", "nå", "dette", "noe", "være", "meg", "mot", "opp",
        "der", "når", "inn", "dem", "kunne", "andre", "blir", "alle", "noen", "sin",
        "ha", "år", "henne", "må", "selv", "sier", "få", "kom", "denne", "enn",
        "to", "hans", "bli", "ville", "før", "vært", "skulle", "går", "her", "slik",
        "gikk", "mer", "hva", "igjen", "fikk", "man", "alt", "mange", "ingen", "får",
        "oss", "hvor", "under", "siden", "hele", "dag", "gang", "sammen", "ned"
    ];

    public static int? DialogAmount { get; private set; }

    [MemberNotNull(nameof(_daglResources), nameof(_privResources), nameof(_norwegianWords), nameof(_englishWords), nameof(DialogAmount))]
    private static void ValidateInitialized()
    {
        if (!_isInitialized || _privResources is null || _daglResources is null || _norwegianWords is null || _englishWords is null || DialogAmount is null)
        {
            throw new InvalidOperationException("StaticStore is not initialized. Call Init() before using this method.");
        }
    }

    public static Resource GetRandomResource(string party, Random? rng = null)
    {
        ValidateInitialized();
        rng ??= Random.Shared;

        if (party.StartsWith(NorwegianPersonIdentifier.PrefixWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            return _privResources[rng.Next(0, _privResources.Length)];
        }

        if (party.StartsWith(NorwegianOrganizationIdentifier.PrefixWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            return _daglResources[rng.Next(0, _daglResources.Length)];
        }

        throw new ArgumentException($"Party must be a valid Norwegian identifier. Got {party}.", nameof(party));
    }

    public static string[] GetRandomWords(int count, Random? rng = null)
    {
        ValidateInitialized();
        rng ??= Random.Shared;
        var distinctWords = new HashSet<string>();
        while (distinctWords.Count < count)
        {
            var word = distinctWords.Count % 2 == 0
                ? _norwegianWords[rng.Next(0, _norwegianWords.Length)]
                : _englishWords[rng.Next(0, _englishWords.Length)];
            distinctWords.Add(word);
        }
        return distinctWords.ToArray();
    }

    public static string GetRandomSentence(int minLength, int maxLength, Random? rng = null)
    {
        ValidateInitialized();
        rng ??= Random.Shared;
        ReadOnlySpan<char> giveMeSomeSpace = " ";
        var length = rng.Next(minLength, maxLength);
        Span<char> result = stackalloc char[length];
        var remaining = length;
        while (remaining > 0)
        {
            var word = rng.Next(0,5) == 0
                ? FrequentWords[rng.Next(0, FrequentWords.Length)].AsSpan()
                : _norwegianWords[rng.Next(0, _norwegianWords.Length)].AsSpan();
            if (word.Length > remaining) break;
            word.CopyTo(result[^remaining..]);
            remaining -= word.Length;
            if (giveMeSomeSpace.Length > remaining) break;
            giveMeSomeSpace.CopyTo(result[^remaining..]);
            remaining -= giveMeSomeSpace.Length;
        }
        return result[..(length - remaining)].ToString();
    }

    public static async Task Init(string connectionString, string altinnPlatformBaseUrl, int dialogAmount)
    {
        var (dagls, privs) = await GetDaglsAndPrivs(connectionString, altinnPlatformBaseUrl);
        var (norwegianWords, englishWords) = await FetchWordLists();
        _daglResources = dagls.ToArray();
        _privResources = privs.ToArray();
        _norwegianWords = norwegianWords
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length > 3) // Filter out short words
            .ToArray();
        _englishWords = englishWords
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length > 3) // Filter out short words
            .ToArray();
        DialogAmount = dialogAmount;
        _isInitialized = true;
    }

    private static async Task<(List<Resource> dagls, List<Resource> privs)> GetDaglsAndPrivs(string connectionString, string altinnPlatformBaseUrl1)
    {
        await using var db = new DialogDbContext(new DbContextOptionsBuilder<DialogDbContext>()
            .UseNpgsql(connectionString).Options);
        var subjectResources = await db.SubjectResources
            .AsNoTracking()
            .Where(x => x.Subject == "urn:altinn:rolecode:dagl" || x.Subject == "urn:altinn:rolecode:priv")
            .ToListAsync();

        if (subjectResources.Count == 0)
        {
            throw new InvalidOperationException(
                $"No {nameof(SubjectResource)} found in db. " +
                $"Seed the {nameof(SubjectResource)} table before running this tool. " +
                $"There needs to be at least one entry for each of " +
                $"the subjects 'urn:altinn:rolecode:dagl' and 'urn:altinn:rolecode:priv'.");
        }

        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(altinnPlatformBaseUrl1);
        var refitClient = RestService.For<IResourceRegistry>(httpClient);

        var resources = await refitClient.GetResources();
        return resources
            .GroupJoin(subjectResources,
                x => x.Identifier, x => x.Resource,
                (dto, resources) => new Resource(
                    dto.Identifier,
                    dto.ResourceType,
                    dto.HasCompetentAuthority.Orgcode,
                    resources.Select(r => r.Subject)))
            .Aggregate((Dagls: new List<Resource>(), Privs: new List<Resource>()), (acc, resource) =>
            {
                if (resource.HasDagl) acc.Dagls.Add(resource);
                if (resource.HasPriv) acc.Privs.Add(resource);
                return acc;
            });
    }

    private static async Task<(string norwegianWords, string englishWords)> FetchWordLists()
    {
        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("https://github.com");
        var refitClient = RestService.For<IGithubClient>(httpClient);
        var wordLists = await Task.WhenAll(
            refitClient.GetNorwegianWordList(),
            refitClient.GetEnglishWordList());
        return (wordLists[0], wordLists[1]);
    }
}
