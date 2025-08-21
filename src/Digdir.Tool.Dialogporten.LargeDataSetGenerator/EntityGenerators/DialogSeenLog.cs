using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.CopyCommand;
using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.CsvBuilder;
using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.ListBuilder;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class DialogSeenLog
{
    public static readonly string CopyCommand = Create(nameof(DialogSeenLog),
        "Id", "CreatedAt", "IsViaServiceOwner", "DialogId", "EndUserTypeId");

    public sealed record DialogSeenLogDto(Guid Id, DialogUserType.Values EndUserTypeId);

    public static List<DialogSeenLogDto> GetDtos(DialogTimestamp dialogDto)
        => BuildList<DialogSeenLogDto>(dtos =>
            dtos.Add(new(dialogDto.DialogId, DialogUserType.Values.Person)));

    public static string Generate(DialogTimestamp dto) => BuildCsv(sb =>
    {
        foreach (var dialogSeenLog in GetDtos(dto))
        {
            sb.AppendLine($"{dialogSeenLog.Id},{dto.FormattedTimestamp},FALSE,{dto.DialogId},{(int)dialogSeenLog.EndUserTypeId}");
        }
    });
}
