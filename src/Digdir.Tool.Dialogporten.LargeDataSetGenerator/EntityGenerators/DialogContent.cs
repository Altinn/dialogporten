using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.CopyCommand;
using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.CsvBuilder;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class DialogContent
{
    public static readonly string CopyCommand = Create(nameof(DialogContent),
        "Id", "CreatedAt", "UpdatedAt", "MediaType", "DialogId", "TypeId");

    public const string DomainName = nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Contents.DialogContent);

    public sealed record DialogContentDto(Guid Id, DialogContentType.Values TypeId);

    public static List<DialogContentDto> GetDtos(DialogTimestamp dto)
    {
        var dialogTitleId = DeterministicUuidV7.Generate(dto.Timestamp, DomainName, (int)DialogContentType.Values.Title);
        var dialogTitle = new DialogContentDto(dialogTitleId, DialogContentType.Values.Title);

        var dialogSummaryId = DeterministicUuidV7.Generate(dto.Timestamp, DomainName, (int)DialogContentType.Values.Summary);
        var dialogSummary = new DialogContentDto(dialogSummaryId, DialogContentType.Values.Summary);

        return [dialogTitle, dialogSummary];
    }

    public static string Generate(DialogTimestamp dto) => BuildCsv(sb =>
    {
        foreach (var dialogContent in GetDtos(dto))
        {
            sb.AppendLine($"{dialogContent.Id},{dto.FormattedTimestamp},{dto.FormattedTimestamp},text/plain,{dto.DialogId},{(int)dialogContent.TypeId}");
        }
    });
}
