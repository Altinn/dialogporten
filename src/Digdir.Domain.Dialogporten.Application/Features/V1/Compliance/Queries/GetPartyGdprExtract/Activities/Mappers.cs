using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.Compliance.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Compliance.Queries.GetPartyGdprExtract.Activities;

internal static class DialogActivityMapExtensions
{
    extension(DialogActivity activity)
    {
        internal ActivityGdprExtractDto ToGdprExtractDto() => new()
        {
            Id = activity.Id,
            CreatedAt = activity.CreatedAt,
            ExtendedType = activity.ExtendedType,
            Type = activity.TypeId,
            TransmissionId = activity.TransmissionId,
            PerformedBy = activity.PerformedBy.ToDto(),
            Description = activity.Description.ToDtoList()!,
            DialogId = activity.DialogId
        };
    }
}
