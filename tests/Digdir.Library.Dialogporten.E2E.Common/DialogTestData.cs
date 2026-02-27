using Altinn.ApiClients.Dialogporten.Features.V1;
using Digdir.Domain.Dialogporten.Domain;

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

    public static V1ServiceOwnerDialogsCommandsCreate_Dialog CreateComplexDialog(
        Action<V1ServiceOwnerDialogsCommandsCreate_Dialog>? modify = null)
    {
        var dialog = new V1ServiceOwnerDialogsCommandsCreate_Dialog
        {
            Id = null,
            IdempotentKey = null!,
            ServiceResource = "urn:altinn:resource:ttd-dialogporten-automated-tests",
            Party = "urn:altinn:person:identifier-no:" + E2EConstants.DefaultEndUserSsn,
            Progress = null,
            ExtendedStatus = "extended status",
            ExternalReference = "externalReference",
            VisibleFrom = null,
            DueAt = null,
            Process = "process",
            PrecedingProcess = "precedingProcess",
            ExpiresAt = null,
            IsApiOnly = false,
            CreatedAt = null,
            UpdatedAt = null,
            Status = null,
            SystemLabel = null,
            ServiceOwnerContext = new V1ServiceOwnerDialogsCommandsCreate_DialogServiceOwnerContext
            {
                ServiceOwnerLabels =
                [
                    new V1ServiceOwnerDialogsCommandsCreate_ServiceOwnerLabel
                    {
                        Value = "label"
                    }
                ]
            },
            Content = CreateContent(
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
                    value:
                    "Utvidet forklaring (enkel HTML-støtte, inntil 1023 tegn). Ikke påkrevd. Vises kun i detaljvisning.",
                    languageCode: "nb",
                    mediaType: "text/plain"),
                extendedStatus: CreateContentValue(
                    value: "Utvidet status",
                    languageCode: "nb",
                    mediaType: "text/plain"),
                mainContentReference: CreateContentValue(
                    value: "https://localhost",
                    languageCode: "nb",
                    mediaType: MediaTypes.EmbeddableMarkdown
                )
            ),
            SearchTags =
            [
                new V1ServiceOwnerDialogsCommandsCreate_Tag
                {
                    Value = "SearchTag"
                }
            ],
            Attachments =
            [
                new V1ServiceOwnerDialogsCommandsCreate_Attachment
                {
                    Id = null,
                    DisplayName =
                    [
                        new V1CommonLocalizations_Localization
                        {
                            Value = "display name",
                            LanguageCode = "nb"
                        }
                    ],
                    Name = "name",
                    Urls =
                    [
                        new V1ServiceOwnerDialogsCommandsCreate_AttachmentUrl
                        {
                            Url = new Uri("https://localhost/attachment-1"),
                            MediaType = MediaTypes.PlainText,
                            ConsumerType = Attachments_AttachmentUrlConsumerType.Gui
                        }
                    ],
                    ExpiresAt = null
                }
            ],
            Transmissions =
            [
                new V1ServiceOwnerDialogsCommandsCreate_Transmission
                {
                    Id = null,
                    IdempotentKey = null!,
                    CreatedAt = default,
                    AuthorizationAttribute = "attribute",
                    ExtendedType = new Uri("http://example.com/transmissions/extended-type"),
                    ExternalReference = "externalReference",
                    RelatedTransmissionId = null,
                    Type = DialogsEntitiesTransmissions_DialogTransmissionType.Information,
                    Sender = new V1ServiceOwnerCommonActors_Actor
                    {
                        ActorType = Actors_ActorType.PartyRepresentative,
                        ActorName = "name",
                        ActorId = null!
                    },
                    Content = new V1ServiceOwnerDialogsCommandsCreate_TransmissionContent
                    {
                        Title = new V1CommonContent_ContentValue
                        {
                            Value =
                            [
                                new V1CommonLocalizations_Localization
                                {
                                    Value = "title",
                                    LanguageCode = "nb"
                                }
                            ],
                            MediaType = MediaTypes.PlainText,
                        },
                        Summary = new V1CommonContent_ContentValue
                        {
                            Value =
                            [
                                new V1CommonLocalizations_Localization
                                {
                                    Value = "Summary",
                                    LanguageCode = "nb"
                                }
                            ],
                            MediaType = MediaTypes.PlainText,
                        },
                        ContentReference = new V1CommonContent_ContentValue
                        {
                            Value =
                            [
                                new V1CommonLocalizations_Localization
                                {
                                    Value = "https://localhost/transmission-content-reference",
                                    LanguageCode = "nb"
                                }
                            ],
                            MediaType = MediaTypes.EmbeddableMarkdown,
                        }
                    },
                    Attachments =
                    [
                        new V1ServiceOwnerDialogsCommandsCreate_TransmissionAttachment
                        {
                            Id = null,
                            DisplayName =
                            [
                                new V1CommonLocalizations_Localization
                                {
                                    Value = "displayName",
                                    LanguageCode = "nb"
                                }
                            ],
                            Name = "Name",
                            Urls =
                            [
                                new V1ServiceOwnerDialogsCommandsCreate_TransmissionAttachmentUrl
                                {
                                    Url = new Uri("https://localhost/transmission-attachment"),
                                    MediaType = MediaTypes.PlainText,
                                    ConsumerType = Attachments_AttachmentUrlConsumerType.Gui
                                }
                            ],
                            ExpiresAt = null
                        }
                    ],
                    NavigationalActions =
                    [
                        new V1ServiceOwnerDialogsCommandsCreate_TransmissionNavigationalAction
                        {
                            Title =
                            [
                                new V1CommonLocalizations_Localization
                                {
                                    Value = "navigational-action",
                                    LanguageCode = "nb"
                                }
                            ],
                            Url = new Uri("https://localhost/transmissions/navigational-action"),
                            ExpiresAt = null
                        }
                    ]
                }
            ],
            GuiActions =
            [
                new V1ServiceOwnerDialogsCommandsCreate_GuiAction
                {
                    Id = null,
                    Action = "open",
                    Url = new Uri("https://localhost/gui-action"),
                    AuthorizationAttribute = "authorization-attribute",
                    IsDeleteDialogAction = false,
                    HttpMethod = Http_HttpVerb.GET,
                    Priority = DialogsEntitiesActions_DialogGuiActionPriority.Primary,
                    Title =
                    [
                        new V1CommonLocalizations_Localization
                        {
                            Value = "open",
                            LanguageCode = "nb"
                        }
                    ],
                    Prompt =
                    [
                        new V1CommonLocalizations_Localization
                        {
                            Value = "prompt",
                            LanguageCode = "nb"
                        }
                    ]
                }
            ],
            ApiActions =
            [
                new V1ServiceOwnerDialogsCommandsCreate_ApiAction
                {
                    Id = null,
                    Action = "goto",
                    AuthorizationAttribute = "authorization-attribute-api-action",
                    Name = "goto",
                    Endpoints =
                    [
                        new V1ServiceOwnerDialogsCommandsCreate_ApiActionEndpoint
                        {
                            Id = null,
                            Version = "1",
                            Url = new Uri("https://localhost/api-action"),
                            HttpMethod = Http_HttpVerb.GET,
                            DocumentationUrl = new Uri("http://localhost/api-action-documentation"),
                            RequestSchema = new Uri("http://localhost/api-action-request-schema"),
                            ResponseSchema = new Uri("http://localhost/api-action-response-schema"),
                            Deprecated = false,
                            SunsetAt = null
                        }
                    ]
                }
            ],
            Activities =
            [
                new V1ServiceOwnerDialogsCommandsCreate_Activity
                {
                    Id = null,
                    CreatedAt = null,
                    ExtendedType = new Uri("http://localhost/activities"),
                    Type = DialogsEntitiesActivities_DialogActivityType.DialogCreated,
                    TransmissionId = null,
                    PerformedBy = new V1ServiceOwnerCommonActors_Actor
                    {
                        ActorType = Actors_ActorType.PartyRepresentative,
                        ActorName = "name",
                        ActorId = null!
                    },
                    Description = []
                }
            ]
        };

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
        V1CommonContent_ContentValue? extendedStatus = null,
        V1CommonContent_ContentValue? mainContentReference = null
    )
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

        if (mainContentReference is not null)
            content.MainContentReference = mainContentReference;

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
}
