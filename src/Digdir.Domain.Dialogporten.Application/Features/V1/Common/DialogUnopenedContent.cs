using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common;

public static class DialogUnopenedContent
{
    public static bool HasUnopenedContent(DialogEntity dialog, ServiceResourceInformation? serviceResourceInformation)
    {

        // Checks if transmissions of all types except Correction or Acceptance have any activities with TransmissionOpened
        var hasUnopenedContent = dialog.Transmissions
                                       .Where(transmission => transmission.TypeId is not
                                           (DialogTransmissionType.Values.Correction or DialogTransmissionType.Values.Acceptance))
                                       .SelectMany(transmission => transmission.Activities)
                                       .All(activity => activity.TypeId != DialogActivityType.Values.TransmissionOpened);


        if (hasUnopenedContent) return true;

        if (serviceResourceInformation?.ResourceType == "CorrespondenceService")
        {
            return dialog.Activities.All(x => x.TypeId != DialogActivityType.Values.CorrespondenceOpened);
        }

        return false;
    }
}
