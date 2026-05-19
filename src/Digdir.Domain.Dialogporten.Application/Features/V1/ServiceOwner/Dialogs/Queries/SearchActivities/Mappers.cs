using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.SearchActivities;

internal static class DialogActivityMapExtensions
{
    extension(DialogActivity activity)
    {
        internal ActivityDto ToDto() => new()
        {
            Id = activity.Id,
            CreatedAt = activity.CreatedAt,
            ExtendedType = activity.ExtendedType,
            Type = activity.TypeId,
            TransmissionId = activity.TransmissionId
        };
    }
}
