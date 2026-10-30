using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common;

public static class ActivitiesExtensions
{
    extension(List<DialogActivity> activities)
    {
        internal void RemoveExtraTransmissionOpenedActivities()
        {
            var seenTransmissionIds = new HashSet<Guid>();
            activities.RemoveAll(x =>
                x is { TypeId: DialogActivityType.Values.TransmissionOpened, TransmissionId: not null }
                && !seenTransmissionIds.Add(x.TransmissionId.Value));
        }
    }
}
