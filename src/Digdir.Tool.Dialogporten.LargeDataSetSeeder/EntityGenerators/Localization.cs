using Digdir.Tool.Dialogporten.LargeDataSetSeeder.Common;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

public sealed record Localization(
    Guid LocalizationSetId,
    string LanguageCode,
    string Value
) : IEntityGenerator<Localization>
{
    public static IEnumerable<Localization> GenerateEntities(IEnumerable<DialogTimestamp> timestamps)
    {
        var norwegianLorem = LanguageLorem.NorwegianLorem;
        var englishLorem = LanguageLorem.EnglishLorem;

        foreach (var timestamp in timestamps)
        {
            foreach (var localizationSet in LocalizationSet.GenerateEntities([timestamp]))
            {
                yield return new(
                    LocalizationSetId: localizationSet.Id,
                    LanguageCode: "nb",
                    Value: norwegianLorem.Sentence()
                );

                yield return new(
                    LocalizationSetId: localizationSet.Id,
                    LanguageCode: "en",
                    Value: englishLorem.Sentence()
                );
            }
        }
    }
}
