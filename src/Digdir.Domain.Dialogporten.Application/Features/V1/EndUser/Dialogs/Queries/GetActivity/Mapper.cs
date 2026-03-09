#pragma warning disable CS8601

using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.GetActivity;

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
            TransmissionId = source.TransmissionId,
            PerformedBy = source.PerformedBy.ToDto(),
            Description = source.Description.ToDto()
        };
    }
}

#pragma warning restore CS8601
