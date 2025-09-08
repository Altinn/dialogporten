using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using FastEndpoints;
using Microsoft.Extensions.Primitives;

namespace Digdir.Domain.Dialogporten.WebApi.Common;

public static class AcceptedLanguageParser
{
    public static ParseResult Parse(StringValues input)
    {
        var headerString = input.First();
        List<AcceptedLanguage> acceptedLanguages = [];
        if (headerString is null)
        {
            return new(false, acceptedLanguages);
        }

        var langs = headerString.Split(',');
        foreach (var lang in langs)
        {
            var temp = lang.Split(";");
            if (temp.Length == 1)
            {
                acceptedLanguages.Add(new AcceptedLanguage(temp[0], 1));
                continue;
            }

            var temp2 = temp[1].Split("=")[1];
            float.TryParse(temp2, out var f1);

            acceptedLanguages.Add(new AcceptedLanguage(temp[0], f1));
        }

        return new(true, acceptedLanguages);

    }
}
