using Altinn.ApiClients.Dialogporten.Features.V1;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;

namespace Digdir.Library.Dialogporten.E2E.Common;

public static class DialogTestData
{
    public static V1ServiceOwnerDialogsCommandsCreate_Dialog CreateSimpleDialog(
        Action<V1ServiceOwnerDialogsCommandsCreate_Dialog>? modify = null)
    {
        var dialog = CreateDialog(
            serviceResource: "urn:altinn:resource:ttd-dialogporten-automated-tests",
            party: $"urn:altinn:person:identifier-no:{E2EConstants.DefaultEndUserSsn}",
            content: CreateContent(
                title: CreateContentValue(
                    value: "Skjema for rapportering av et eller annet",
                    languageCode: "nb"),
                summary: CreateContentValue(
                    value: "Et sammendrag her. Maks 200 tegn, ingen HTML-støtte. Påkrevd. Vises i liste.",
                    languageCode: "nb"),
                senderName: CreateContentValue(
                    value: "Avsendernavn",
                    languageCode: "nb"),
                additionalInfo: CreateContentValue(
                    value: "Utvidet forklaring (enkel HTML-støtte, inntil 1023 tegn). Ikke påkrevd. Vises kun i detaljvisning.",
                    languageCode: "nb",
                    mediaType: "text/plain"),
                extendedStatus: CreateContentValue(
                    value: "Utvidet status",
                    languageCode: "nb",
                    mediaType: "text/plain")));

        modify?.Invoke(dialog);
        return dialog;
    }

    public static V1ServiceOwnerDialogsCommandsCreate_Dialog CreateDialog(
        string serviceResource,
        string party,
        V1ServiceOwnerDialogsCommandsCreate_Content content) =>
        new()
        {
            ServiceResource = serviceResource,
            Party = party,
            Content = content
        };

    public static V1ServiceOwnerDialogsCommandsCreate_Content CreateContent(
        V1CommonContent_ContentValue title,
        V1CommonContent_ContentValue? summary = null,
        V1CommonContent_ContentValue? senderName = null,
        V1CommonContent_ContentValue? additionalInfo = null,
        V1CommonContent_ContentValue? extendedStatus = null)
    {
        var content = new V1ServiceOwnerDialogsCommandsCreate_Content
        {
            Title = title
        };

        if (summary is not null)
            content.Summary = summary;

        if (senderName is not null)
            content.SenderName = senderName;

        if (additionalInfo is not null)
            content.AdditionalInfo = additionalInfo;

        if (extendedStatus is not null)
            content.ExtendedStatus = extendedStatus;

        return content;
    }

    public static V1CommonContent_ContentValue CreateContentValue(
        string value,
        string languageCode,
        string? mediaType = null) =>
        CreateContentValue(
            mediaType: mediaType,
            value: [CreateLocalization(value, languageCode)]);

    public static V1CommonContent_ContentValue CreateContentValue(
        List<V1CommonLocalizations_Localization> value,
        string? mediaType = null)
    {
        var contentValue = new V1CommonContent_ContentValue
        {
            Value = value
        };

        if (mediaType is not null)
        {
            contentValue.MediaType = mediaType;
        }

        return contentValue;
    }

    public static V1CommonLocalizations_Localization CreateLocalization(
        string value,
        string languageCode = "nb") =>
        new()
        {
            Value = value,
            LanguageCode = languageCode,
        };

    public static V1ServiceOwnerDialogsCommandsCreateTransmission_TransmissionRequest CreateSimpleTransmission(
        Action<V1ServiceOwnerDialogsCommandsCreateTransmission_TransmissionRequest>? modify = null)
    {
        var transmission = new V1ServiceOwnerDialogsCommandsCreateTransmission_TransmissionRequest
        {
            Type = DialogsEntitiesTransmissions_DialogTransmissionType.Information,
            Sender = new V1ServiceOwnerCommonActors_Actor
            {
                ActorType = Actors_ActorType.ServiceOwner
            },
            Content = new V1ServiceOwnerDialogsCommandsCreateTransmission_TransmissionContent
            {
                Title = CreateContentValue(
                    value: "Melding med vedlegg",
                    languageCode: "nb")
            }
        };

        modify?.Invoke(transmission);
        return transmission;
    }

    public static V1ServiceOwnerDialogsCommandsCreateActivity_ActivityRequest CreateSimpleActivity(
        Action<V1ServiceOwnerDialogsCommandsCreateActivity_ActivityRequest>? modify = null)
    {
        var activity = new V1ServiceOwnerDialogsCommandsCreateActivity_ActivityRequest
        {
            Type = DialogsEntitiesActivities_DialogActivityType.DialogCreated,
            ExtendedType = new Uri("http://localhost"),
            PerformedBy = new V1ServiceOwnerCommonActors_Actor
            {
                ActorType = Actors_ActorType.PartyRepresentative,
                ActorId = "urn:altinn:person:legacy-selfidentified:Leif"
            },
            Description = []
        };

        modify?.Invoke(activity);
        return activity;
    }

    public static V1ServiceOwnerDialogsCommandsCreate_Transmission AddAttachment(this V1ServiceOwnerDialogsCommandsCreate_Transmission transmission,
        Action<V1ServiceOwnerDialogsCommandsCreate_TransmissionAttachment>? modify = null)
    {
        ArgumentNullException.ThrowIfNull(transmission);
        transmission.Attachments ??= [];

        var attachment = new V1ServiceOwnerDialogsCommandsCreate_TransmissionAttachment
        {
            DisplayName = [CreateLocalization("Forsendelsevedlegg")],
            Urls =
            [
                new V1ServiceOwnerDialogsCommandsCreate_TransmissionAttachmentUrl
                {
                    Url = new Uri("https://example.com/transmission-attachment.pdf"),
                    ConsumerType = Attachments_AttachmentUrlConsumerType.Gui
                }
            ]
        };

        modify?.Invoke(attachment);
        transmission.Attachments.Add(attachment);

        return transmission;
    }

    public static V1ServiceOwnerDialogsCommandsCreateTransmission_TransmissionRequest AddAttachment(
        this V1ServiceOwnerDialogsCommandsCreateTransmission_TransmissionRequest transmission,
        Action<V1ServiceOwnerDialogsCommandsCreateTransmission_TransmissionAttachment>? modify = null)
    {
        ArgumentNullException.ThrowIfNull(transmission);
        transmission.Attachments ??= [];

        var attachment = new V1ServiceOwnerDialogsCommandsCreateTransmission_TransmissionAttachment
        {
            DisplayName = [CreateLocalization("Forsendelsevedlegg")],
            Urls =
            [
                new V1ServiceOwnerDialogsCommandsCreateTransmission_TransmissionAttachmentUrl
                {
                    Url = new Uri("https://example.com/transmission-attachment.pdf"),
                    ConsumerType = Attachments_AttachmentUrlConsumerType.Gui
                }
            ]
        };

        modify?.Invoke(attachment);
        transmission.Attachments.Add(attachment);
        return transmission;
    }

    public static Guid NewUuidV7(DateTimeOffset? timeStamp = null) => IdentifiableExtensions.CreateVersion7(timeStamp);
}
