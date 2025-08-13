using System.Text;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators.CopyCommand;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class Activity
{
    public static readonly string CopyCommand = Create(nameof(Activity),
        "Id", "CreatedAt", "ExtendedType", "TypeId", "TransmissionId", "DialogId");

    public static List<ActivityDto> GetDtos(DialogTimestamp dialogDto)
    {
        // All dialogs have a DialogCreated activity.
        var dialogCreatedActivityId = DeterministicUuidV7.Generate(dialogDto.Timestamp, nameof(DialogActivity), (int)DialogActivityType.Values.DialogCreated);
        List<ActivityDto> dtos = [new(dialogCreatedActivityId,(int)DialogActivityType.Values.DialogCreated)];

        var rng = dialogDto.GetRng();

        // Approx. 1/2 of dialogs have a DialogOpened activity.
        if (rng.Next(0, 2) == 0)
        {
            var dialogOpenedActivityId = DeterministicUuidV7.Generate(dialogDto.Timestamp, nameof(DialogActivity), (int)DialogActivityType.Values.DialogOpened);
            dtos.Add(new(dialogOpenedActivityId, (int)DialogActivityType.Values.DialogOpened));
        }

        // Approx. 1/3 of dialogs have an Information activity.
        if (rng.Next(0, 3) == 0)
        {
            var informationActivityId = DeterministicUuidV7.Generate(dialogDto.Timestamp, nameof(DialogActivity), (int)DialogActivityType.Values.Information);
            dtos.Add(new(informationActivityId, (int)DialogActivityType.Values.Information));
        }

        return dtos;
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
