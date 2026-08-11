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

    // Letters only, including Norwegian æøå and other accented letters. The lookarounds require
    // a non-alphanumeric boundary on both sides so letter runs inside alphanumeric identifiers
    // ('Hansen123', 'REF2024ABC') are rejected outright rather than yielding a fragment like
    // 'hansen' — such fragments are identifier/PII noise, and nothing downstream re-checks for
    // digit adjacency. Both lookarounds must exclude letters as well as digits: a digit-only
    // lookahead ((?!\p{N})) lets the engine backtrack \p{L}+ one char and match 'Hanse' anyway.
    [GeneratedRegex(@"(?<![\p{L}\p{N}])\p{L}+(?![\p{L}\p{N}])", RegexOptions.CultureInvariant)]
    private static partial Regex WordRegex();
}
