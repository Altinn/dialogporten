using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using static Digdir.Tool.Dialogporten.LargeDataSetSeeder.Utils;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

internal static class DialogSeenLog
{
    // public static readonly string CopyCommand = CreateCopyCommand(nameof(DialogSeenLog),
    //     "Id", "CreatedAt", "IsViaServiceOwner", "DialogId", "EndUserTypeId");

    public sealed record DialogSeenLogDto(Guid Id, DialogUserType.Values EndUserTypeId);

    public static List<DialogSeenLogDto> GetDtos(DialogTimestamp dto)
        => BuildDtoList<DialogSeenLogDto>(dtos =>
            dtos.Add(new(dto.DialogId, DialogUserType.Values.Person)));

    public static string Generate(DialogTimestamp dto) => BuildCsv(sb =>
    {
        foreach (var dialogSeenLog in GetDtos(dto))
        {
            sb.AppendLine($"{dialogSeenLog.Id},{dto.FormattedTimestamp},FALSE,{dto.DialogId},{(int)dialogSeenLog.EndUserTypeId}");
        }
    });
}
