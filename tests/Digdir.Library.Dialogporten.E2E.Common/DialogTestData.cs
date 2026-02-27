using Altinn.ApiClients.Dialogporten.Features.V1;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

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
                    value: "Utvidet Status",
                    languageCode: "nb")));

        dialog.Status = V1ServiceOwnerCommonDialogStatuses_DialogStatusInput.NotApplicable;
        dialog.ExtendedStatus = "urn:any/valid/uri";
        dialog.DueAt = new DateTimeOffset(2033, 11, 25, 6, 37, 54, 292, TimeSpan.Zero);
        dialog.ExpiresAt = new DateTimeOffset(2053, 11, 25, 6, 37, 54, 292, TimeSpan.Zero);
        dialog.Process = "urn:test:process:1";
        dialog.SearchTags =
        [
            new V1ServiceOwnerDialogsCommandsCreate_Tag { Value = "something searchable" },
            new V1ServiceOwnerDialogsCommandsCreate_Tag { Value = "something else searchable" }
        ];
        dialog.ServiceOwnerContext = new V1ServiceOwnerDialogsCommandsCreate_DialogServiceOwnerContext
        {
            ServiceOwnerLabels =
            [
                new V1ServiceOwnerDialogsCommandsCreate_ServiceOwnerLabel { Value = "some-label" }
            ]
        };

        // 3 transmissions, each with content title+summary, sender=serviceOwner, 1 attachment
        dialog
            .AddTransmission(t =>
            {
                t.Content = new V1ServiceOwnerDialogsCommandsCreate_TransmissionContent
                {
                    Title = CreateContentValue(
                        value: [CreateLocalization("Forsendelsestittel", "nb"), CreateLocalization("Transmission title", "en")]),
                    Summary = CreateContentValue(
                        value: [CreateLocalization("Forsendelse oppsummering", "nb"), CreateLocalization("Transmission summary", "en")])
                };
                t.Attachments =
                [
                    new V1ServiceOwnerDialogsCommandsCreate_TransmissionAttachment
                    {
                        DisplayName = [CreateLocalization("Forsendelse visningsnavn", "nb"), CreateLocalization("Transmission attachment display name", "en")],
                        Urls =
                        [
                            new V1ServiceOwnerDialogsCommandsCreate_TransmissionAttachmentUrl
                            {
                                Url = new Uri("https://digdir.apps.tt02.altinn.no/some-other-url"),
                                ConsumerType = Attachments_AttachmentUrlConsumerType.Gui
                            }
                        ]
                    }
                ];
            })
            .AddTransmission(t =>
            {
                t.Content = new V1ServiceOwnerDialogsCommandsCreate_TransmissionContent
                {
                    Title = CreateContentValue(
                        value: [CreateLocalization("Forsendelsesstittel", "nb"), CreateLocalization("Transmission title", "en")]),
                    Summary = CreateContentValue(
                        value: [CreateLocalization("Transmisjon oppsummering", "nb"), CreateLocalization("Transmission summary", "en")])
                };
                t.Attachments =
                [
                    new V1ServiceOwnerDialogsCommandsCreate_TransmissionAttachment
                    {
                        DisplayName = [CreateLocalization("Visningsnavn for forsendelsesvedlegg ", "nb"), CreateLocalization("Transmission attachment display name", "en")],
                        Urls =
                        [
                            new V1ServiceOwnerDialogsCommandsCreate_TransmissionAttachmentUrl
                            {
                                Url = new Uri("https://digdir.apps.tt02.altinn.no/some-other-url"),
                                ConsumerType = Attachments_AttachmentUrlConsumerType.Gui
                            }
                        ]
                    }
                ];
            })
            .AddTransmission(t =>
            {
                t.Content = new V1ServiceOwnerDialogsCommandsCreate_TransmissionContent
                {
                    Title = CreateContentValue(
                        value: [CreateLocalization("Forsendelsetittel", "nb"), CreateLocalization("Transmission title", "en")]),
                    Summary = CreateContentValue(
                        value: [CreateLocalization("Forsendelsesoppsummering", "nb"), CreateLocalization("Transmission summary", "en")])
                };
                t.Attachments =
                [
                    new V1ServiceOwnerDialogsCommandsCreate_TransmissionAttachment
                    {
                        DisplayName = [CreateLocalization("Visningsnavn for forsendelsesvedlegg", "nb"), CreateLocalization("Transmission attachment display name", "en")],
                        Urls =
                        [
                            new V1ServiceOwnerDialogsCommandsCreate_TransmissionAttachmentUrl
                            {
                                Url = new Uri("https://digdir.apps.tt02.altinn.no/some-other-url"),
                                ConsumerType = Attachments_AttachmentUrlConsumerType.Gui
                            }
                        ]
                    }
                ];
            });

        // 2 GUI actions: Primary GET + Secondary POST with prompt
        dialog
            .AddGuiAction(g =>
            {
                g.Action = "read";
                g.Url = new Uri("https://digdir.no");
                g.Priority = DialogsEntitiesActions_DialogGuiActionPriority.Primary;
                g.Title = [CreateLocalization("Gå til dialog", "nb")];
            })
            .AddGuiAction(g =>
            {
                g.Action = "read";
                g.Url = new Uri("https://digdir.no");
                g.Priority = DialogsEntitiesActions_DialogGuiActionPriority.Secondary;
                g.HttpMethod = Http_HttpVerb.POST;
                g.Title = [CreateLocalization("Utfør handling uten navigasjon", "nb")];
                g.Prompt = [CreateLocalization("Er du sikker?", "nb")];
            });

        // 1 API action with 2 endpoints (one deprecated)
        dialog.AddApiAction();

        // 3 attachments
        dialog
            .AddAttachment(a =>
            {
                a.DisplayName = [CreateLocalization("Et vedlegg", "nb")];
                a.Urls =
                [
                    new V1ServiceOwnerDialogsCommandsCreate_AttachmentUrl
                    {
                        Url = new Uri("https://foo.com/foo.pdf"),
                        ConsumerType = Attachments_AttachmentUrlConsumerType.Gui,
                        MediaType = "application/pdf"
                    }
                ];
            })
            .AddAttachment(a =>
            {
                a.DisplayName = [CreateLocalization("Et annet vedlegg", "nb")];
                a.Urls =
                [
                    new V1ServiceOwnerDialogsCommandsCreate_AttachmentUrl
                    {
                        Url = new Uri("https://foo.com/foo.pdf"),
                        ConsumerType = Attachments_AttachmentUrlConsumerType.Gui,
                        MediaType = "application/pdf"
                    }
                ];
            })
            .AddAttachment(a =>
            {
                a.DisplayName = [CreateLocalization("Nok et vedlegg", "nb")];
                a.Urls =
                [
                    new V1ServiceOwnerDialogsCommandsCreate_AttachmentUrl
                    {
                        Url = new Uri("https://foo.com/foo.pdf"),
                        ConsumerType = Attachments_AttachmentUrlConsumerType.Gui,
                        MediaType = "application/pdf"
                    }
                ];
            });

        // 3 activities
        dialog
            .AddActivity(a =>
            {
                a.Type = DialogsEntitiesActivities_DialogActivityType.DialogCreated;
                a.PerformedBy = new V1ServiceOwnerCommonActors_Actor
                {
                    ActorType = Actors_ActorType.PartyRepresentative,
                    ActorName = "Some custom name"
                };
            })
            .AddActivity(a =>
            {
                a.Type = DialogsEntitiesActivities_DialogActivityType.PaymentMade;
                a.PerformedBy = new V1ServiceOwnerCommonActors_Actor
                {
                    ActorType = Actors_ActorType.ServiceOwner
                };
            })
            .AddActivity(a =>
            {
                a.Type = DialogsEntitiesActivities_DialogActivityType.Information;
                a.PerformedBy = new V1ServiceOwnerCommonActors_Actor
                {
                    ActorType = Actors_ActorType.PartyRepresentative,
                    ActorId = $"urn:altinn:organization:identifier-no:{E2EConstants.DefaultServiceOwnerOrgNr}"
                };
                a.Description =
                [
                    CreateLocalization("Brukeren har begått skattesvindel", "nb"),
                    CreateLocalization("Tax fraud", "en")
                ];
            });

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
}
