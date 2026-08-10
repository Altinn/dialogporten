using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.SearchTerms.Tokenizer;

internal sealed partial class SearchTermsTokenizer : ISearchTermsTokenizer
{
    public HashSet<string> Tokenize(string text)
    {
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(text))
        {
            return tokens;
        }

        // Compose to NFC first: \p{L}+ does not match combining marks, so decomposed input
        // (e.g. 'å' as 'a' + U+030A) would otherwise split into bogus fragments.
        foreach (Match match in WordRegex().Matches(text.Normalize(NormalizationForm.FormC)))
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
