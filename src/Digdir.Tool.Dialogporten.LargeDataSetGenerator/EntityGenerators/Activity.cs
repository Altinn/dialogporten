using System.Text;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators.CopyCommand;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class Activity
{
    public static readonly string CopyCommand = Create(nameof(Activity),
        "Id", "CreatedAt", "ExtendedType", "TypeId", "TransmissionId", "DialogId");

    public const int DialogCreatedType = 1;
    public const int InformationType = 2;

    public static List<ActivityDto> GetDtos(DialogTimestamp dialogDto)
    {
        var activityId1 = DeterministicUuidV7.Generate(dialogDto.Timestamp, nameof(DialogActivity), DialogCreatedType);
        var activityId2 = DeterministicUuidV7.Generate(dialogDto.Timestamp, nameof(DialogActivity), InformationType);

        return
        [
            new(activityId1, DialogCreatedType),
            new(activityId2, InformationType)
        ];
    }

    public static string Generate(DialogTimestamp dto)
    {
        var activityCsvData = new StringBuilder();

        foreach (var activity in GetDtos(dto))
        {
            activityCsvData.AppendLine($"{activity.Id},{dto.FormattedTimestamp},,{activity.TypeId},,{dto.DialogId}");
        }

        return activityCsvData.ToString();
    }

    public record ActivityDto(Guid Id, int TypeId);
}
