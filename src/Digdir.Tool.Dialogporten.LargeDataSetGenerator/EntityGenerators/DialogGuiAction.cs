using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Http;
using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.Utils;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class DialogGuiAction
{
    private static readonly string[] Properties = [
        "Id", "CreatedAt", "UpdatedAt", "Action",
        "Url", "AuthorizationAttribute", "IsDeleteDialogAction",
        "PriorityId", "HttpMethodId", "DialogId"
    ];

    public static readonly string CopyCommand = CreateCopyCommand(nameof(DialogGuiAction), Properties);

    public const string DomainName = nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Actions.DialogGuiAction);
    private const string Action = "submit";
    private const string Url = "https://digdir.apps.tt02.altinn.no";
    private const int HttpMethodId = (int)HttpVerb.Values.POST;

    public sealed record DialogGuiActionDto(Guid Id, DialogGuiActionPriority.Values PriorityId);

    public static List<DialogGuiActionDto> GetDtos(DialogTimestamp dto) => BuildDtoList<DialogGuiActionDto>(dtos =>
    {
        foreach (var priority in Enum.GetValues<DialogGuiActionPriority.Values>())
        {
            var guiActionId = dto.ToUuidV7(DomainName, (int)priority);
            dtos.Add(new DialogGuiActionDto(guiActionId, priority));
        }
    });

    public static string Generate(DialogTimestamp dto) => BuildCsv(sb =>
    {
        foreach (var guiAction in GetDtos(dto))
        {
            sb.AppendLine(
                $"{guiAction.Id}," +
                $"{dto.FormattedTimestamp}," +
                $"{dto.FormattedTimestamp}," +
                $"{Action}," +
                $"{Url}," +
                $"{Null}," +
                $"FALSE," +
                $"{(int)guiAction.PriorityId}," +
                $"{HttpMethodId}," +
                $"{dto.DialogId}");
        }
    });
}
