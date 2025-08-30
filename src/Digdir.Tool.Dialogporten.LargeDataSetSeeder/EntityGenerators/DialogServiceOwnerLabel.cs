using Digdir.Tool.Dialogporten.LargeDataSetSeeder.Common;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

public sealed record DialogServiceOwnerLabel(
    Guid DialogServiceOwnerContextId,
    DateTimeOffset CreatedAt,
    string Value
) : IEntityGenerator<DialogServiceOwnerLabel>
{
    public static IEnumerable<DialogServiceOwnerLabel> GenerateEntities(IEnumerable<DialogTimestamp> timestamps)
    {
        foreach (var context in DialogServiceOwnerContext.GenerateEntities(timestamps))
        {
            foreach (var serviceLabel in LanguageLorem.GetRandomWords(5).Distinct())
            {
                yield return new(
                    DialogServiceOwnerContextId: context.DialogId,
                    CreatedAt: context.CreatedAt,
                    Value: serviceLabel
                );
            }
        }
    }
}

