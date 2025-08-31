using Bogus.DataSets;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.Common;

internal static class LanguageLorem
{
    internal static readonly Lorem NorwegianLorem = new(locale: "nb_NO");
    internal static readonly Lorem EnglishLorem = new(locale: "en");

    internal static string[] GetRandomWords(int count) => Enumerable
        .Range(0, count)
        .Select(x => x % 2 == 0 ? NorwegianLorem.Word() : EnglishLorem.Word())
        .ToArray();
}
