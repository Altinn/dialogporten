using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Microsoft.EntityFrameworkCore;
using Constants = Digdir.Domain.Dialogporten.Application.Common.ResourceRegistry.Constants;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common;

public static class DialogUnopenedContent
{
    public static bool IsRelevantTransmissionType(DialogTransmissionType.Values transmissionType) =>
        transmissionType is not (DialogTransmissionType.Values.Correction or DialogTransmissionType.Values.Submission);

    public static bool HasUnopenedContent(DialogEntity dialog, ServiceResourceInformation? serviceResourceInformation)
    {
        // Checks if transmissions of all types except Correction or Submission have any activities with TransmissionOpened
        var transmissions = dialog.Transmissions
            .Where(transmission => IsRelevantTransmissionType(transmission.TypeId));

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

    /// <summary>
    /// Same as <see cref="HasUnopenedContent"/>, for a dialog whose stored transmissions are not loaded.
    /// The dialog's activities must be loaded, and <paramref name="newActivities"/> are the ones added but not yet saved.
    /// </summary>
    public static async Task<bool> HasUnopenedContentAsync(
        IDialogDbContext db,
        DialogEntity dialog,
        IReadOnlyCollection<DialogActivity> newActivities,
        ServiceResourceInformation? serviceResourceInformation,
        CancellationToken cancellationToken)
    {
        var openedTransmissionIds = dialog.Activities
            .Where(x => x.TypeId == DialogActivityType.Values.TransmissionOpened)
            .Select(x => x.TransmissionId)
            .OfType<Guid>()
            .ToHashSet();

        if (dialog.Transmissions.Any(transmission =>
                IsRelevantTransmissionType(transmission.TypeId) &&
                !openedTransmissionIds.Contains(transmission.Id)))
        {
            return true;
        }

        var newlyOpenedTransmissionIds = newActivities
            .Where(x => x.TypeId == DialogActivityType.Values.TransmissionOpened)
            .Select(x => x.TransmissionId)
            .OfType<Guid>()
            .ToList();

        var hasUnopenedStoredTransmission = await db.DialogTransmissions
            .Where(x => x.DialogId == dialog.Id)
            .Where(x => x.TypeId != DialogTransmissionType.Values.Correction &&
                x.TypeId != DialogTransmissionType.Values.Submission)
            .Where(x => !newlyOpenedTransmissionIds.Contains(x.Id))
            .AnyAsync(x => x.Activities
                .All(a => a.TypeId != DialogActivityType.Values.TransmissionOpened), cancellationToken);

        if (hasUnopenedStoredTransmission)
        {
            return true;
        }

        return serviceResourceInformation?.ResourceType == Constants.CorrespondenceService &&
            dialog.Activities.All(a => a.TypeId != DialogActivityType.Values.CorrespondenceOpened);
    }

    public static bool IsOpened(DialogTransmission transmission) => IsRelevantTransmissionType(transmission.TypeId)
        && transmission.Activities.Any(x => x.TypeId == DialogActivityType.Values.TransmissionOpened);
}
