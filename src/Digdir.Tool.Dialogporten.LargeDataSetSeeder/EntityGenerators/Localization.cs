using Digdir.Tool.Dialogporten.LargeDataSetSeeder.FileImport;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

public sealed record Localization(
    Guid LocalizationSetId,
    string LanguageCode,
    string Value
) : IEntityGenerator<Localization>
{
    public static IEnumerable<Localization> GenerateEntities(IEnumerable<DialogTimestamp> timestamps)
    {
        foreach (var timestamp in timestamps)
        {
            foreach (var localizationSet in LocalizationSet.GenerateEntities([timestamp]))
            {
                var english = $"{Words.English.GetRandomWord()} {Words.English.GetRandomWord()}";
                var norwegian = $"{Words.Norwegian.GetRandomWord()} {Words.Norwegian.GetRandomWord()}";

                yield return new(
                    LocalizationSetId: localizationSet.Id,
                    LanguageCode: "nb",
                    Value: norwegian
                );

                yield return new(
                    LocalizationSetId: localizationSet.Id,
                    LanguageCode: "en",
                    Value: english
                );
            }
        }
    }
}
