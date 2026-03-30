using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateActivity;

internal static class CreateActivityDtoMapExtensions
{
    extension(CreateActivityDto source)
    {
        internal DialogActivity ToDialogActivity() => new()
        {
            Id = source.Id ?? Guid.Empty,
            CreatedAt = source.CreatedAt ?? default,
            ExtendedType = source.ExtendedType,
            TypeId = source.Type,
            TransmissionId = source.TransmissionId,
            PerformedBy = source.PerformedBy.ToActor<DialogActivityPerformedByActor>(),
            Description = source.Description.Count == 0
                ? null
                : new DialogActivityDescription
                {
                    Localizations = source.Description.Select(x => x.ToLocalization()).ToList()
                }
        };
    }
}
