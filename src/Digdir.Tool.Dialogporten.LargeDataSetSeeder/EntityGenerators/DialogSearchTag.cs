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
            var searchTags = Words.GetBetweenZeroAndCountWords(count: 6);

            foreach (var (searchTag, tieBreaker) in searchTags)
            {
                yield return CreateSearchTag(timestamp, tieBreaker, searchTag);
            }
        }
    }

    private static DialogSearchTag CreateSearchTag(DialogTimestamp timestamp, int tieBreaker, string searchTag) =>
        new(
            Id: timestamp.ToUuidV7(timestamp.DialogId, tieBreaker),
            Value: searchTag,
            CreatedAt: timestamp.Timestamp,
            DialogId: timestamp.DialogId
        );
}
