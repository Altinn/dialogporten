using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

public sealed record DialogContent(
    Guid Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string MediaType,
    Guid DialogId,
    DialogContentType.Values TypeId
) : IEntityGenerator<DialogContent>
{
    public static IEnumerable<DialogContent> GenerateEntities(IEnumerable<DialogTimestamp> timestamps)
    {
        foreach (var timestamp in timestamps)
        {
            yield return CreateDialogContent(timestamp, DialogContentType.Values.Title);
            yield return CreateDialogContent(timestamp, DialogContentType.Values.Summary);
        }
    }

    private static DialogContent CreateDialogContent(DialogTimestamp timestamp, DialogContentType.Values typeId) =>
        new(
            Id: timestamp.ToUuidV7(timestamp.DialogId, (int)typeId),
            CreatedAt: timestamp.Timestamp,
            UpdatedAt: timestamp.Timestamp,
            MediaType: "text/plain",
            DialogId: timestamp.DialogId,
            TypeId: typeId
        );
}
