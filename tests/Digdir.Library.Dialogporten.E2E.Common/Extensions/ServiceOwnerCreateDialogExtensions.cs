using Altinn.ApiClients.Dialogporten.Features.V1;

namespace Digdir.Library.Dialogporten.E2E.Common.Extensions;

public static class ServiceOwnerCreateDialogExtensions
{
    extension(V1ServiceOwnerDialogsCommandsCreate_Dialog dialog)
    {
        public V1ServiceOwnerDialogsCommandsCreate_Dialog AddAttachment(Action<V1ServiceOwnerDialogsCommandsCreate_Attachment>? modify = null)
        {
            ArgumentNullException.ThrowIfNull(dialog);
            dialog.Attachments ??= [];

            var attachment = new V1ServiceOwnerDialogsCommandsCreate_Attachment
            {
                DisplayName = [DialogTestData.CreateLocalization("Dialogvedlegg")],
                Urls =
                [
                    new V1ServiceOwnerDialogsCommandsCreate_AttachmentUrl
                    {
                        Url = new Uri("https://example.com/dialog-attachment.pdf"),
                        ConsumerType = Attachments_AttachmentUrlConsumerType.Gui
                    }
                ]
            };

            modify?.Invoke(attachment);
            dialog.Attachments.Add(attachment);
            return dialog;
        }

        public V1ServiceOwnerDialogsCommandsCreate_Dialog AddTransmission(Action<V1ServiceOwnerDialogsCommandsCreate_Transmission>? modify = null)
        {
            ArgumentNullException.ThrowIfNull(dialog);
            dialog.Transmissions ??= [];

            var transmission = new V1ServiceOwnerDialogsCommandsCreate_Transmission
            {
                Id = Guid.CreateVersion7(),
                CreatedAt = DateTimeOffset.UtcNow,
                Type = DialogsEntitiesTransmissions_DialogTransmissionType.Information,
                Sender = new V1ServiceOwnerCommonActors_Actor
                {
                    ActorType = Actors_ActorType.ServiceOwner
                },
                Content = new V1ServiceOwnerDialogsCommandsCreate_TransmissionContent
                {
                    Title = DialogTestData.CreateContentValue(
                        value: "Melding med vedlegg",
                        languageCode: "nb")
                }
            };

            modify?.Invoke(transmission);
            dialog.Transmissions.Add(transmission);
            return dialog;
        }

        public V1ServiceOwnerDialogsCommandsCreate_Dialog AddApiAction(Action<V1ServiceOwnerDialogsCommandsCreate_ApiAction>? modify = null)
        {
            ArgumentNullException.ThrowIfNull(dialog);
            dialog.ApiActions ??= [];

            var apiAction = new V1ServiceOwnerDialogsCommandsCreate_ApiAction
            {
                Action = "some_unauthorized_action",
                Name = "confirm",
                Endpoints =
                [
                    new V1ServiceOwnerDialogsCommandsCreate_ApiActionEndpoint
                    {
                        Url = new Uri("https://digdir.no"),
                        HttpMethod = Http_HttpVerb.GET
                    },
                    new V1ServiceOwnerDialogsCommandsCreate_ApiActionEndpoint
                    {
                        Url = new Uri("https://digdir.no/deprecated"),
                        HttpMethod = Http_HttpVerb.GET,
                        Deprecated = true
                    }
                ]
            };

            modify?.Invoke(apiAction);
            dialog.ApiActions.Add(apiAction);
            return dialog;
        }

        public V1ServiceOwnerDialogsCommandsCreate_Dialog AddActivity(Action<V1ServiceOwnerDialogsCommandsCreate_Activity>? modify = null)
        {
            ArgumentNullException.ThrowIfNull(dialog);
            dialog.Activities ??= [];

            var activity = new V1ServiceOwnerDialogsCommandsCreate_Activity
            {
                Type = DialogsEntitiesActivities_DialogActivityType.DialogCreated,
                PerformedBy = new V1ServiceOwnerCommonActors_Actor
                {
                    ActorType = Actors_ActorType.PartyRepresentative,
                    ActorName = "Some custom name"
                }
            };

            modify?.Invoke(activity);
            dialog.Activities.Add(activity);
            return dialog;
        }
    }
}
