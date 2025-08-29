using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Tool.Dialogporten.LargeDataSetSeeder.Common;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

public sealed record LocalizationSet(
    Guid Id,
    string Discriminator,
    Guid? AttachmentId,
    Guid? GuiActionId,
    Guid? ActivityId,
    Guid? DialogContentId,
    Guid? TransmissionContentId
) : IEntityGenerator<LocalizationSet>
{
    public static IEnumerable<LocalizationSet> GenerateEntities(IEnumerable<DialogTimestamp> timestamps)
    {
        foreach (var timestamp in timestamps)
        {
            // Attachments
            foreach (var attachment in Attachment.GenerateEntities([timestamp]))
            {
                yield return CreateLocalizationSet(
                    id: timestamp.ToUuidV7<LocalizationSet>(attachment.Id),
                    discriminator: "AttachmentDisplayName",
                    attachmentId: attachment.Id
                );
            }

            // GuiAction
            foreach (var guiAction in DialogGuiAction.GenerateEntities([timestamp]))
            {
                yield return CreateLocalizationSet(
                    id: timestamp.ToUuidV7<LocalizationSet>(guiAction.Id),
                    discriminator: "DialogGuiActionTitle",
                    guiActionId: guiAction.Id
                );
            }

            // DialogActivity
            var informationActivities = DialogActivity
                .GenerateEntities([timestamp])
                .Where(x => x.TypeId == DialogActivityType.Values.Information)
                .ToList();

            foreach (var activity in informationActivities)
            {
                yield return CreateLocalizationSet(
                    id: timestamp.ToUuidV7<LocalizationSet>(activity.Id),
                    discriminator: "DialogActivityDescription",
                    activityId: activity.Id
                );
            }

            // DialogContent
            foreach (var content in DialogContent.GenerateEntities([timestamp]))
            {
                yield return CreateLocalizationSet(
                    id: timestamp.ToUuidV7<LocalizationSet>(content.Id),
                    discriminator: "DialogContentValue",
                    dialogContentId: content.Id
                );
            }

            // DialogTransmissionContent
            foreach (var tc in DialogTransmissionContent.GenerateEntities([timestamp]))
            {
                yield return CreateLocalizationSet(
                    id: timestamp.ToUuidV7<LocalizationSet>(tc.Id),
                    discriminator: "DialogTransmissionContentValue",
                    transmissionContentId: tc.Id
                );
            }
        }
    }

    private static LocalizationSet CreateLocalizationSet(
        Guid id,
        string discriminator,
        Guid? attachmentId = null,
        Guid? guiActionId = null,
        Guid? activityId = null,
        Guid? dialogContentId = null,
        Guid? transmissionContentId = null
    ) => new(
        Id: id,
        Discriminator: discriminator,
        AttachmentId: attachmentId,
        GuiActionId: guiActionId,
        ActivityId: activityId,
        DialogContentId: dialogContentId,
        TransmissionContentId: transmissionContentId
    );
}
