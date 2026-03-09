using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateActivity;

internal static class ActivityMapper
{
    extension(CreateActivityDto source)
    {
        public DialogActivity ToEntity() => new()
        {
            Id = source.Id ?? Guid.Empty,
            CreatedAt = source.CreatedAt ?? default,
            ExtendedType = source.ExtendedType,
            TypeId = source.Type,
            TransmissionId = source.TransmissionId,
            PerformedBy = source.PerformedBy.ToDialogActivityPerformedByActor(),
            Description = source.Description.MergeInto<DialogActivityDescription>(null)
        };
    }
}
