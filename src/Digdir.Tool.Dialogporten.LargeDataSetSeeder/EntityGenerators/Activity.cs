using System;
using System.Collections.Generic;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using static Digdir.Tool.Dialogporten.LargeDataSetSeeder.Utils;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

internal static class Activity
{
    private const DialogActivityType.Values DialogCreated = DialogActivityType.Values.DialogCreated;
    private const DialogActivityType.Values DialogOpened = DialogActivityType.Values.DialogOpened;
    private const DialogActivityType.Values Information = DialogActivityType.Values.Information;

    private const string DomainName = nameof(DialogActivity);

    public sealed record ActivityDto(Guid Id, DialogActivityType.Values TypeId);

    public static List<ActivityDto> GetDtos(DialogTimestamp dto) => BuildDtoList<ActivityDto>(dtos =>
    {
        // All dialogs have a DialogCreated activity.
        var dialogCreatedActivityId = dto.ToUuidV7(DomainName, (int)DialogCreated);
        dtos.Add(new(dialogCreatedActivityId, DialogCreated));

        var rng = dto.GetRng();

        // Approx. 1/2 of dialogs have a DialogOpened activity.
        if (rng.Next(0, 2) == 0)
        {
            var dialogOpenedActivityId = dto.ToUuidV7(DomainName, (int)DialogOpened);
            dtos.Add(new(dialogOpenedActivityId, DialogOpened));
        }

        // Approx. 1/3 of dialogs have an Information activity.
        if (rng.Next(0, 3) == 0)
        {
            var informationActivityId = dto.ToUuidV7(DomainName, (int)Information);
            dtos.Add(new(informationActivityId, Information));
        }
    });

    public static string Generate(DialogTimestamp _) => BuildCsv(sb =>
    {
        // var magic = new Magic();
        // return magic.CreateCsv<Activity>(dto, GetDtos(dto));

        // foreach (var activity in GetDtos(dto))
        // {
        //     sb.AppendDialogActivityLine(
        //         Id: activity.Id,
        //         CreatedAt: dto.Timestamp,
        //         ExtendedType: null,
        //         TypeId: activity.TypeId,
        //         DialogId: dto.DialogId,
        //         FooId: null);
        // }
    });
}
