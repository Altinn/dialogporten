using Digdir.Tool.Dialogporten.LargeDataSetSeeder.Common;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

public sealed record DialogSearchTag(
    Guid Id,
    string Value,
    DateTimeOffset CreatedAt,
    Guid DialogId
) : IEntityGenerator<DialogSearchTag>
{
    public static IEnumerable<DialogSearchTag> GenerateEntities(IEnumerable<DialogTimestamp> timestamps)
    {
        foreach (var timestamp in timestamps)
        {
            var searchTags = LanguageLorem
                .GetRandomWords(Random.Shared.Next(0, 7))
                .Distinct()
                .Select((x, i) => (x, i));

            foreach (var (searchTag, tieBreaker) in searchTags)
            {
                yield return new(
                    Id: timestamp.ToUuidV7<DialogSearchTag>(timestamp.DialogId, tieBreaker),
                    Value: searchTag,
                    CreatedAt: timestamp.Timestamp,
                    DialogId: timestamp.DialogId
                );
            }
        }
    }
}
