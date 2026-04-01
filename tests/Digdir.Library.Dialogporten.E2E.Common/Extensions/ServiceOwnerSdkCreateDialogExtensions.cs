using Altinn.ApiClients.Dialogporten.ServiceOwner.V1;

namespace Digdir.Library.Dialogporten.E2E.Common.Extensions;

public static class ServiceOwnerSdkCreateDialogExtensions
{
    extension(CreateDialogRequest dialog)
    {
        public CreateDialogRequest AddAttachment(Action<CreateDialogAttachment>? modify = null)
        {
            ArgumentNullException.ThrowIfNull(dialog);
            dialog.Attachments ??= [];

            var attachment = new CreateDialogAttachment
            {
                DisplayName = [ServiceOwnerSdkDialogTestData.CreateLocalization("Dialogvedlegg")],
                Urls =
                [
                    new CreateDialogAttachmentUrl
                    {
                        Url = new Uri("https://example.com/dialog-attachment.pdf"),
                        ConsumerType = AttachmentUrlConsumerType.Gui
                    }
                ]
            };

            modify?.Invoke(attachment);
            dialog.Attachments.Add(attachment);
            return dialog;
        }

        public CreateDialogRequest AddTransmission(Action<CreateDialogTransmission>? modify = null)
        {
            ArgumentNullException.ThrowIfNull(dialog);
            dialog.Transmissions ??= [];

            var transmission = new CreateDialogTransmission
            {
                Id = Guid.CreateVersion7(),
                CreatedAt = DateTimeOffset.UtcNow,
                Type = DialogTransmissionType.Information,
                Sender = new Actor
                {
                    ActorType = ActorType.ServiceOwner
                },
                Content = new CreateDialogTransmissionContent
                {
                    Title = ServiceOwnerSdkDialogTestData.CreateContentValue(
                        value: "Melding med vedlegg",
                        languageCode: "nb")
                }
            };

            modify?.Invoke(transmission);
            dialog.Transmissions.Add(transmission);
            return dialog;
        }

        public CreateDialogRequest AddApiAction(Action<CreateDialogApiAction>? modify = null)
        {
            ArgumentNullException.ThrowIfNull(dialog);
            dialog.ApiActions ??= [];

            var apiAction = new CreateDialogApiAction
            {
                Action = "some_unauthorized_action",
                Name = "confirm",
                Endpoints =
                [
                    new CreateDialogApiActionEndpoint
                    {
                        Url = new Uri("https://digdir.no"),
                        HttpMethod = HttpVerb.GET
                    },
                    new CreateDialogApiActionEndpoint
                    {
                        Url = new Uri("https://digdir.no/deprecated"),
                        HttpMethod = HttpVerb.GET,
                        Deprecated = true
                    }
                ]
            };

            modify?.Invoke(apiAction);
            dialog.ApiActions.Add(apiAction);
            return dialog;
        }

        public CreateDialogRequest AddActivity(Action<CreateDialogActivity>? modify = null)
        {
            ArgumentNullException.ThrowIfNull(dialog);
            dialog.Activities ??= [];

            var activity = new CreateDialogActivity
            {
                Type = DialogActivityType.DialogCreated,
                PerformedBy = new Actor
                {
                    ActorType = ActorType.PartyRepresentative,
                    ActorName = "Some custom name"
                }
            };

            modify?.Invoke(activity);
            dialog.Activities.Add(activity);
            return dialog;
        }

        public CreateDialogRequest AddGuiAction(Action<CreateDialogGuiAction>? modify = null)
        {
            ArgumentNullException.ThrowIfNull(dialog);
            dialog.GuiActions ??= [];

            var guiAction = new CreateDialogGuiAction
            {
                Action = "read",
                Url = new Uri("https://digdir.no"),
                Priority = DialogGuiActionPriority.Primary,
                Title = [ServiceOwnerSdkDialogTestData.CreateLocalization("Gui-handling")]
            };

            modify?.Invoke(guiAction);
            dialog.GuiActions.Add(guiAction);
            return dialog;
        }
    }
}

public static class ServiceOwnerSdkCreateDialogTransmissionExtensions
{
    extension(CreateDialogTransmission transmission)
    {
        public CreateDialogTransmission AddAttachment(Action<CreateDialogTransmissionAttachment>? modify = null)
        {
            ArgumentNullException.ThrowIfNull(transmission);
            transmission.Attachments ??= [];

            var attachment = new CreateDialogTransmissionAttachment
            {
                DisplayName = [ServiceOwnerSdkDialogTestData.CreateLocalization("Forsendelsevedlegg")],
                Urls =
                [
                    new CreateDialogTransmissionAttachmentUrl
                    {
                        Url = new Uri("https://example.com/transmission-attachment.pdf"),
                        ConsumerType = AttachmentUrlConsumerType.Gui
                    }
                ]
            };

            modify?.Invoke(attachment);
            transmission.Attachments.Add(attachment);

            return transmission;
        }
    }
}

public static class ServiceOwnerSdkCreateTransmissionExtensions
{
    extension(CreateTransmissionRequest transmission)
    {
        public CreateTransmissionRequest AddAttachment(Action<CreateTransmissionAttachment>? modify = null)
        {
            ArgumentNullException.ThrowIfNull(transmission);
            transmission.Attachments ??= [];

            var attachment = new CreateTransmissionAttachment
            {
                DisplayName = [ServiceOwnerSdkDialogTestData.CreateLocalization("Forsendelsevedlegg")],
                Urls =
                [
                    new CreateTransmissionAttachmentUrl
                    {
                        Url = new Uri("https://example.com/transmission-attachment.pdf"),
                        ConsumerType = AttachmentUrlConsumerType.Gui
                    }
                ]
            };

            modify?.Invoke(attachment);
            transmission.Attachments.Add(attachment);
            return transmission;
        }
    }
}
