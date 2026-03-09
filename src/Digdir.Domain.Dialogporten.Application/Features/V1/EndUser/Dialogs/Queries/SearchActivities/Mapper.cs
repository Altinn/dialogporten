using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.SearchActivities;

internal static class ActivityMapper
{
    extension(DialogActivity source)
    {
        public ActivityDto ToDto() => new()
        {
            Id = source.Id,
            CreatedAt = source.CreatedAt,
            ExtendedType = source.ExtendedType,
            Type = source.TypeId,
            TransmissionId = source.TransmissionId
        };
    }
}
