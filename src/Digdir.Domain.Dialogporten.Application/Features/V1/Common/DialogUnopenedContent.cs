using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Constants = Digdir.Domain.Dialogporten.Application.Common.ResourceRegistry.Constants;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common;

public static class DialogUnopenedContent
{
    public static bool HasUnopenedContent(DialogEntity dialog, ServiceResourceInformation? serviceResourceInformation)
    {
        // Checks if transmissions of all types except Correction or Submission have any activities with TransmissionOpened
        var transmissions = dialog.Transmissions
            .Where(transmission => transmission.TypeId is not (DialogTransmissionType.Values.Correction or DialogTransmissionType.Values.Submission));

        if (transmissions
            .Any(transmission =>
                transmission.Activities
                    .All(x => x.TypeId != DialogActivityType.Values.TransmissionOpened)))
        {
            return true;
        }

        return serviceResourceInformation?.ResourceType == Constants.CorrespondenceService &&
            dialog.Activities.All(a => a.TypeId != DialogActivityType.Values.CorrespondenceOpened);
    }

    public static bool IsOpened(DialogTransmission transmission) => transmission.TypeId
        is not (DialogTransmissionType.Values.Correction or DialogTransmissionType.Values.Submission)
        && transmission.Activities.Any(x => x.TypeId == DialogActivityType.Values.TransmissionOpened);
}
