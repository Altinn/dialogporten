using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.Utils;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class Activity
{
    public static readonly string CopyCommand = CreateCopyCommand(nameof(Activity),
        "Id", "CreatedAt", "ExtendedType", "TypeId", "TransmissionId", "DialogId");

    private const DialogActivityType.Values DialogCreated = DialogActivityType.Values.DialogCreated;
    private const DialogActivityType.Values DialogOpened = DialogActivityType.Values.DialogOpened;
    private const DialogActivityType.Values Information = DialogActivityType.Values.Information;

    public sealed record ActivityDto(Guid Id, DialogActivityType.Values TypeId);

    public static List<ActivityDto> GetDtos(DialogTimestamp dto) => BuildDtoList<ActivityDto>(dtos =>
    {
        // All dialogs have a DialogCreated activity.
        var dialogCreatedActivityId = dto.ToUuidV7(nameof(DialogActivity), (int)DialogCreated);
        dtos.Add(new(dialogCreatedActivityId, DialogCreated));

        var rng = dto.GetRng();

        // Approx. 1/2 of dialogs have a DialogOpened activity.
        if (rng.Next(0, 2) == 0)
        {
            var dialogOpenedActivityId = dto.ToUuidV7(nameof(DialogActivity), (int)DialogOpened);
            dtos.Add(new(dialogOpenedActivityId, DialogOpened));
        }

        // Approx. 1/3 of dialogs have an Information activity.
        if (rng.Next(0, 3) == 0)
        {
            var informationActivityId = dto.ToUuidV7(nameof(DialogActivity), (int)Information);
            dtos.Add(new(informationActivityId, Information));
        }
    });

    public static string Generate(DialogTimestamp dto) => BuildCsv(sb =>
    {
        foreach (var activity in GetDtos(dto))
        {
            sb.AppendLine(
                $"{activity.Id}," +
                $"{dto.FormattedTimestamp}," +
                $"{Null}," +
                $"{(int)activity.TypeId}," +
                $"{Null}," +
                $"{dto.DialogId}");
        }
    });
}
