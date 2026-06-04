using System.Globalization;
using System.Text.RegularExpressions;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Wordlist.Tokenizer;

internal sealed partial class WordlistTokenizer : IWordlistTokenizer
{
    public HashSet<string> Tokenize(string text)
    {
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(text))
        {
            return tokens;
        }

        foreach (Match match in WordRegex().Matches(text))
        {
            tokens.Add(match.Value.ToLower(CultureInfo.InvariantCulture));
        }
        return tokens;
    }

    // Letters only, including Norwegian æøå and other accented letters. No digits — those are
    // dropped upstream because PII heuristics reject anything containing digits anyway.
    [GeneratedRegex(@"\p{L}+", RegexOptions.CultureInvariant)]
    private static partial Regex WordRegex();
}
