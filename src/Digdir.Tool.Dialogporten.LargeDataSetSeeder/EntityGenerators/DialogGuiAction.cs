using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Http;
using Digdir.Tool.Dialogporten.LargeDataSetSeeder.Common;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

public sealed record DialogGuiAction(
    Guid Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string Action,
    string Url,
    string? AuthorizationAttribute,
    bool IsDeleteDialogAction,
    DialogGuiActionPriority.Values PriorityId,
    HttpVerb.Values HttpMethodId,
    Guid DialogId
) : IEntityGenerator<DialogGuiAction>
{
    public static IEnumerable<DialogGuiAction> GenerateEntities(IEnumerable<DialogTimestamp> timestamps)
    {
        foreach (var timestamp in timestamps)
        {
            foreach (var priority in Enum.GetValues<DialogGuiActionPriority.Values>())
            {
                yield return new DialogGuiAction(
                    Id: timestamp.ToUuidV7<DialogGuiAction>(timestamp.DialogId, (int)priority),
                    CreatedAt: timestamp.Timestamp,
                    UpdatedAt: timestamp.Timestamp,
                    Action: "submit", // TODO: ?
                    Url: "https://digdir.apps.tt02.altinn.no", // TODO:?
                    AuthorizationAttribute: null,
                    IsDeleteDialogAction: false,
                    PriorityId: priority,
                    HttpMethodId: HttpVerb.Values.POST,
                    DialogId: timestamp.DialogId
                );
            }
        }
    }
}
