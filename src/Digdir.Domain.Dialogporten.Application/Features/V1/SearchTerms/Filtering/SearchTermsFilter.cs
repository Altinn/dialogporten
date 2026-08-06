using System.Reflection;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.SearchTerms.Filtering;

internal sealed class SearchTermsFilter : ISearchTermsFilter
{
    private readonly HashSet<string> _stopwords;

    public SearchTermsFilter()
    {
        _stopwords = LoadStopwords();
    }

    public IReadOnlyCollection<string> Stopwords => _stopwords;

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

    private static HashSet<string> LoadStopwords()
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        var assembly = typeof(SearchTermsFilter).Assembly;
        foreach (var name in assembly.GetManifestResourceNames())
        {
            if (!name.Contains(".Stopwords.", StringComparison.Ordinal))
            {
                continue;
            }
            using var stream = assembly.GetManifestResourceStream(name);
            if (stream is null)
            {
                continue;
            }
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
        }
        return set;
    }
}
