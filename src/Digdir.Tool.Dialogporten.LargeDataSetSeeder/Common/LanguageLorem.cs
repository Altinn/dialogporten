using Bogus.DataSets;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.Common;

internal static class LanguageLorem
{
    internal static readonly Lorem NorwegianLorem = new(locale: "nb_NO");
    internal static readonly Lorem EnglishLorem = new(locale: "en");
}
