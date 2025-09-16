using System.Globalization;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using FastEndpoints;
using Microsoft.Extensions.Primitives;

namespace Digdir.Domain.Dialogporten.WebApi.Common;

public static class AcceptedLanguageParser
{
    public static ParseResult Parse(StringValues input)
    {
        var headerString = input.First();
        return headerString is null ? new(false, new()) : ParseFromSpan(headerString);
    }

    private static ParseResult ParseFromSpan(ReadOnlySpan<char> headerSpan)
    {
        List<AcceptedLanguage> acceptedLanguages = [];
        var range = headerSpan.Split(',');
        while (range.MoveNext())
        {

            var langParts = headerSpan[range.Current]; // 0
            var langPartsEnumerator = langParts.Split(";");

            if (!langPartsEnumerator.MoveNext())
            {
                return new(false, new());
            }

            var langCode = langParts[langPartsEnumerator.Current];

            var weight = 100;
            if (langPartsEnumerator.MoveNext())
            {
                var weightSpan = langParts[langPartsEnumerator.Current];
                var weightSpanEnumerator = weightSpan.Split("=");

                if (!weightSpanEnumerator.MoveNext() || !weightSpanEnumerator.MoveNext() || weightSpan[weightSpanEnumerator.Current] is "q")
                {
                    return new(false, new());
                }

                if (float.TryParse(weightSpan[weightSpanEnumerator.Current].Trim(), CultureInfo.InvariantCulture, out var weightFloat))
                {
                    // 0.20000000000003 => 20
                    weight = (int)(weightFloat * 100);
                }
            }

            acceptedLanguages.Add(new AcceptedLanguage(langCode.Trim().ToString(), weight));
        }
        return new(true, acceptedLanguages);
    }
}
