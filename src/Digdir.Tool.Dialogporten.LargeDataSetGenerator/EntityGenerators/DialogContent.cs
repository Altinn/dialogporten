using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.CopyCommand;
using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.CsvBuilder;
using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.DeterministicUuidV7;
using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.ListBuilder;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class DialogContent
{
    public static readonly string CopyCommand = Create(nameof(DialogContent),
        "Id", "CreatedAt", "UpdatedAt", "MediaType", "DialogId", "TypeId");

    public const string DomainName = nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Contents.DialogContent);

    public sealed record DialogContentDto(Guid Id, DialogContentType.Values TypeId);

    private const int TitleTypeId = (int)DialogContentType.Values.Title;
    private const int SummaryTypeId = (int)DialogContentType.Values.Summary;

    public static List<DialogContentDto> GetDtos(DialogTimestamp dto) => BuildList<DialogContentDto>(dtos =>
    {
        var dialogTitleId = CreateUuidV7(dto.Timestamp, DomainName, TitleTypeId);
        var dialogTitle = new DialogContentDto(dialogTitleId, DialogContentType.Values.Title);

        var dialogSummaryId = CreateUuidV7(dto.Timestamp, DomainName, SummaryTypeId);
        var dialogSummary = new DialogContentDto(dialogSummaryId, DialogContentType.Values.Summary);

        dtos.Add(dialogTitle);
        dtos.Add(dialogSummary);
    });

    public static string Generate(DialogTimestamp dto) => BuildCsv(sb =>
    {
        foreach (var dialogContent in GetDtos(dto))
        {
            sb.AppendLine($"{dialogContent.Id},{dto.FormattedTimestamp},{dto.FormattedTimestamp},text/plain,{dto.DialogId},{(int)dialogContent.TypeId}");
        }
    });
}
