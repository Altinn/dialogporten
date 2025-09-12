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
        if (headerString is null)
        {
            return new(false, new());
        }

        List<AcceptedLanguage> acceptedLanguages = [];
        foreach (var lang in headerString.Split(','))
        {
            var valueParts = lang.Trim().Split(";");
            if (valueParts.Length != 2)
            {
                acceptedLanguages.Add(new AcceptedLanguage(lang, 100));
                continue;
            }

            var langCode = valueParts[0];
            var weightString = valueParts[1];
            if (string.IsNullOrWhiteSpace(weightString))
            {
                acceptedLanguages.Add(new AcceptedLanguage(langCode, 1));
                continue;
            }

            var weight = 100;
            if (float.TryParse(valueParts[1].Split("=")[1], CultureInfo.InvariantCulture, out var f1))
            {
                // Amund: 0.20000000000003, nei takk. 200 istedet :D
                weight = (int)(f1 * 100);
            }

            acceptedLanguages.Add(new AcceptedLanguage(langCode, weight));
        }

        return new(true, acceptedLanguages);

    }
}
