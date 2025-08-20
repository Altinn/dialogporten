using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.CopyCommand;
using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.CsvBuilder;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class Activity
{
    public static readonly string CopyCommand = Create(nameof(Activity),
        "Id", "CreatedAt", "ExtendedType", "TypeId", "TransmissionId", "DialogId");

    public static List<ActivityDto> GetDtos(DialogTimestamp dialogDto)
    {
        // All dialogs have a DialogCreated activity.
        var dialogCreatedActivityId = DeterministicUuidV7.Generate(dialogDto.Timestamp, nameof(DialogActivity), (int)DialogActivityType.Values.DialogCreated);
        List<ActivityDto> dtos = [new(dialogCreatedActivityId, DialogActivityType.Values.DialogCreated)];

        var rng = dialogDto.GetRng();

        // Approx. 1/2 of dialogs have a DialogOpened activity.
        if (rng.Next(0, 2) == 0)
        {
            var dialogOpenedActivityId = DeterministicUuidV7.Generate(dialogDto.Timestamp, nameof(DialogActivity), (int)DialogActivityType.Values.DialogOpened);
            dtos.Add(new(dialogOpenedActivityId, DialogActivityType.Values.DialogOpened));
        }

        // Approx. 1/3 of dialogs have an Information activity.
        if (rng.Next(0, 3) == 0)
        {
            var informationActivityId = DeterministicUuidV7.Generate(dialogDto.Timestamp, nameof(DialogActivity), (int)DialogActivityType.Values.Information);
            dtos.Add(new(informationActivityId, DialogActivityType.Values.Information));
        }

        return dtos;
    }

    public static string Generate(DialogTimestamp dto) => BuildCsv(sb =>
    {
        foreach (var activity in GetDtos(dto))
        {
            sb.AppendLine($"{activity.Id},{dto.FormattedTimestamp},,{(int)activity.TypeId},,{dto.DialogId}");
        }
    });

    public sealed record ActivityDto(Guid Id, DialogActivityType.Values TypeId);
}
