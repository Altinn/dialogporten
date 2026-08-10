using System.Reflection;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.SearchTerms.Filtering;

internal sealed class SearchTermsFilter : ISearchTermsFilter
{
    private readonly HashSet<string> _stopwords;
    private readonly Dictionary<string, HashSet<string>> _stopwordsByList;

    public SearchTermsFilter()
    {
        _stopwordsByList = LoadStopwordLists();
        _stopwords = _stopwordsByList.Values
            .SelectMany(x => x)
            .ToHashSet(StringComparer.Ordinal);
    }

    public IReadOnlyCollection<string> Stopwords => _stopwords;

    public IReadOnlyCollection<string> StopwordsForLanguage(string language)
    {
        // Bundled list files are named by language: no.txt covers Norwegian (nb/nn), en.txt English.
        // Only the language's own stopwords may be used for stem-based matching — stemming another
        // language's stopwords with this language's dictionary produces bogus stems that collide
        // with (and would delete) legitimate words (e.g. en 'after' -> norwegian_stem 'aft', which
        // also matches nb 'aften').
        var listKey = language switch
        {
            "nb" or "nn" or "no" => "no",
            _ => language
        };
        return _stopwordsByList.TryGetValue(listKey, out var stopwords)
            ? stopwords
            : [];
    }

    public bool ShouldKeep(string word, int minLength)
    {
        if (word.Length < minLength)
        {
            return false;
        }
        if (_stopwords.Contains(word))
        {
            return false;
        }
        return true;
    }

    private static Dictionary<string, HashSet<string>> LoadStopwordLists()
    {
        var lists = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var assembly = typeof(SearchTermsFilter).Assembly;
        foreach (var name in assembly.GetManifestResourceNames())
        {
            const string marker = ".Stopwords.";
            var markerIndex = name.IndexOf(marker, StringComparison.Ordinal);
            if (markerIndex < 0)
            {
                continue;
            }

            // "….Stopwords.no.txt" -> "no"
            var listKey = Path.GetFileNameWithoutExtension(name[(markerIndex + marker.Length)..]);

            using var stream = assembly.GetManifestResourceStream(name);
            if (stream is null)
            {
                continue;
            }
            var set = new HashSet<string>(StringComparer.Ordinal);
            using var reader = new StreamReader(stream);
            while (reader.ReadLine() is { } line)
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                {
                    continue;
                }
                set.Add(trimmed.ToLowerInvariant());
            }
            lists[listKey] = set;
        }
        return lists;
    }
}
