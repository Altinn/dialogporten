using Digdir.Tool.Dialogporten.LargeDataSetSeeder.Common;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

public record DialogSearch(Guid DialogId, string SearchValue) : IEntityGenerator<DialogSearch>
{
    public static IEnumerable<DialogSearch> GenerateEntities(IEnumerable<DialogTimestamp> timestamps)
    {
        foreach (var dialogTimestamp in timestamps)
        {
            yield return new DialogSearch(
                dialogTimestamp.DialogId,
                StaticStore.GetRandomSentence(2000, 5000, dialogTimestamp.GetRng()));
        }
    }
}
