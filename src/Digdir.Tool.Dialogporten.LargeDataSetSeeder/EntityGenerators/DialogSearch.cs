using Digdir.Tool.Dialogporten.LargeDataSetSeeder.Common;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

public record DialogSearch(Guid DialogId, string Norwegian, string English, string Spanish) : IEntityGenerator<DialogSearch>
{
    public static IEnumerable<DialogSearch> GenerateEntities(IEnumerable<DialogTimestamp> timestamps)
    {
        foreach (var dialogTimestamp in timestamps)
        {
            var rng = dialogTimestamp.GetRng();
            var length = rng.Next(500, 2000);
            yield return new DialogSearch(
                dialogTimestamp.DialogId,
                StaticStore.GetRandomNorwegianSentence(length, rng),
                StaticStore.GetRandomEnglishSentence(length, rng),
                StaticStore.GetRandomSpanishSentence(length, rng));
        }
    }
}
