using System;
using System.Collections.Generic;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using static Digdir.Tool.Dialogporten.LargeDataSetSeeder.Utils;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

internal static class DialogContent
{
    // public static readonly string CopyCommand = CreateCopyCommand(nameof(DialogContent),
    //     "Id", "CreatedAt", "UpdatedAt", "MediaType", "DialogId", "TypeId");

    public const string DomainName = nameof(Domain.Dialogporten.Domain.Dialogs.Entities.Contents.DialogContent);

    public sealed record DialogContentDto(Guid Id, DialogContentType.Values TypeId);

    private const int TitleTypeId = (int)DialogContentType.Values.Title;
    private const int SummaryTypeId = (int)DialogContentType.Values.Summary;

    public static List<DialogContentDto> GetDtos(DialogTimestamp dto) => BuildDtoList<DialogContentDto>(dtos =>
    {
        var dialogTitleId = dto.ToUuidV7(DomainName, TitleTypeId);
        var dialogTitle = new DialogContentDto(dialogTitleId, DialogContentType.Values.Title);

        var dialogSummaryId = dto.ToUuidV7(DomainName, SummaryTypeId);
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
