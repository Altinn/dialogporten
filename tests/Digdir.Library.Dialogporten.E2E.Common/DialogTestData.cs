using Altinn.ApiClients.Dialogporten.Features.V1;

namespace Digdir.Library.Dialogporten.E2E.Common;

public static class DialogTestData
{
    public static V1ServiceOwnerDialogsCommandsCreate_Dialog GetSimpleCreateDialogCommand() =>
        CreateDialog(
            serviceResource: "urn:altinn:resource:ttd-dialogporten-automated-tests",
            party: $"urn:altinn:person:identifier-no:{TestTokenConstants.DefaultEndUserSsn}",
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
}
