using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Tool.Dialogporten.LargeDataSetSeeder.Common;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

public sealed record DialogSeenLog(
    Guid Id,
    DateTimeOffset CreatedAt,
    bool IsViaServiceOwner,
    Guid DialogId,
    DialogUserType.Values EndUserTypeId
) : IEntityGenerator<DialogSeenLog>
{
    public static IEnumerable<DialogSeenLog> GenerateEntities(IEnumerable<DialogTimestamp> timestamps)
    {
        foreach (var timestamp in timestamps)
        {
            yield return new(
                Id: timestamp.ToUuidV7<DialogSeenLog>(timestamp.DialogId),
                CreatedAt: timestamp.Timestamp,
                IsViaServiceOwner: false,
                DialogId: timestamp.DialogId,
                EndUserTypeId: DialogUserType.Values.Person
            );
        }

    }
}
