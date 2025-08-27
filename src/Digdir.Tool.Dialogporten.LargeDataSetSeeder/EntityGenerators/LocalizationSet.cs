using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using static Digdir.Tool.Dialogporten.LargeDataSetSeeder.Utils;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

public sealed record LocalizationSet(
    Guid Id,
    string Discriminator,
    Guid? AttachmentId,
    Guid? GuiActionId,
    Guid? ActivityId,
    Guid? DialogContentId,
    Guid? TransmissionContentId
) : IEntityGenerator<LocalizationSet>
{
    public static IEnumerable<LocalizationSet> GenerateEntities(IEnumerable<DialogTimestamp> timestamps)
    {
        return [];
    }
}

// internal static class LocalizationSet
// {
//     // public static readonly string CopyCommand = CreateCopyCommand(nameof(LocalizationSet),
//     //     "Id", "CreatedAt", "Discriminator", "AttachmentId", "GuiActionId",
//     //     "ActivityId", "DialogContentId", "TransmissionContentId");
//
//     private const string AttachmentDiscriminator = "AttachmentDisplayName";
//     private const string DialogGuiActionDiscriminator = "DialogGuiActionTitle";
//     private const string DialogActivityDiscriminator = "DialogActivityDescription";
//     private const string DialogContentDiscriminator = "DialogContentValue";
//     private const string DialogTransmissionContentDiscriminator = "DialogTransmissionContentValue";
//
//     public sealed record LocalizationSetDto(Guid Id, string Discriminator);
//
//     public static List<LocalizationSetDto> GetDtos(DialogTimestamp dto) => BuildDtoList<LocalizationSetDto>(dtos =>
//     {
//         // Attachments
//         dtos.AddRange(Attachment.GetDtos(dto)
//             .Select(attachment =>
//                 new LocalizationSetDto(attachment.Id, AttachmentDiscriminator)));
//
//         // GuiAction
//         dtos.AddRange(DialogGuiAction.GetDtos(dto)
//             .Select(guiAction =>
//                 new LocalizationSetDto(guiAction.Id, DialogGuiActionDiscriminator)));
//
//         // DialogActivity
//         // var informationActivities = ActivityFoo
//         //     .GetDtos(dto)
//         //     // Only information activities have localization entries.
//         //     .Where(x => x.TypeId == DialogActivityType.Values.Information)
//         //     .ToList();
//         //
//         // dtos.AddRange(informationActivities
//         //     .Select(activity =>
//         //         new LocalizationSetDto(activity.Id, DialogActivityDiscriminator)));
//
//         // DialogContent
//         dtos.AddRange(DialogContent.GetDtos(dto)
//             .Select(content =>
//                 new LocalizationSetDto(content.Id, DialogContentDiscriminator)));
//
//         // DialogTransmissionContent
//         dtos.AddRange(DialogTransmissionContent.GetDtos(dto)
//             .Select(tc =>
//                 new LocalizationSetDto(tc.Id, DialogTransmissionContentDiscriminator)));
//     });
//
//     public static string Generate(DialogTimestamp dto) => BuildCsv(sb =>
//     {
//         foreach (var localizationSet in GetDtos(dto))
//         {
//             sb.AppendLine(
//                 $"{localizationSet.Id},{dto.FormattedTimestamp}," +
//                 $"{localizationSet.Discriminator}," +
//                 $"{(localizationSet.Discriminator == AttachmentDiscriminator ? localizationSet.Id.ToString() : string.Empty)}," +
//                 $"{(localizationSet.Discriminator == DialogGuiActionDiscriminator ? localizationSet.Id.ToString() : string.Empty)}," +
//                 $"{(localizationSet.Discriminator == DialogActivityDiscriminator ? localizationSet.Id.ToString() : string.Empty)}," +
//                 $"{(localizationSet.Discriminator == DialogContentDiscriminator ? localizationSet.Id.ToString() : string.Empty)}," +
//                 $"{(localizationSet.Discriminator == DialogTransmissionContentDiscriminator ? localizationSet.Id.ToString() : string.Empty)}");
//         }
//     });
// }
