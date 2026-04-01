using Altinn.ApiClients.Dialogporten.ServiceOwner.V1;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;

namespace Digdir.Library.Dialogporten.E2E.Common;

public static class ServiceOwnerSdkDialogTestData
{
    public static Guid NewUuidV7(DateTimeOffset? timeStamp = null) =>
        IdentifiableExtensions.CreateVersion7(timeStamp);

    public static CreateDialogRequest CreateSimpleDialog(Action<CreateDialogRequest>? modify = null)
    {
        var dialog = CreateDialog(
            serviceResource: E2EConstants.DefaultServiceResource,
            party: E2EConstants.DefaultParty,
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

    public static CreateDialogRequest CreateComplexDialog(Action<CreateDialogRequest>? modify = null)
    {
        var dialog = CreateDialog(
            serviceResource: E2EConstants.DefaultServiceResource,
            party: E2EConstants.DefaultParty,
            content: CreateContent(
                title: CreateContentValue(
                    value: "Skjema for rapportering av et eller annet",
                    languageCode: "nb"),
                nonSensitiveTitle: CreateContentValue(
                    value: "Ikke-sensitiv tittel",
                    languageCode: "nb"),
                summary: CreateContentValue(
                    value: "Et sammendrag her. Maks 200 tegn, ingen HTML-støtte. Påkrevd. Vises i liste.",
                    languageCode: "nb"),
                nonSensitiveSummary: CreateContentValue(
                    value: "Ikke-sensitiv sammendrag",
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

        dialog.Status = DialogStatusInput.NotApplicable;
        dialog.ExtendedStatus = "urn:any/valid/uri";
        dialog.DueAt = new DateTimeOffset(2033, 11, 25, 6, 37, 54, 292, TimeSpan.Zero);
        dialog.ExpiresAt = new DateTimeOffset(2053, 11, 25, 6, 37, 54, 292, TimeSpan.Zero);
        dialog.Process = "urn:test:process:1";
        dialog.SearchTags =
        [
            new CreateDialogTag { Value = "something searchable" },
            new CreateDialogTag { Value = "something else searchable" }
        ];
        dialog.ServiceOwnerContext = new CreateDialogServiceOwnerContext
        {
            ServiceOwnerLabels =
            [
                new CreateDialogServiceOwnerLabel { Value = "some-label" }
            ]
        };

        dialog
            .AddTransmission(t =>
            {
                t.AuthorizationAttribute = E2EConstants.AvailableExternalResource;
                t.Content = new CreateDialogTransmissionContent
                {
                    Title = CreateContentValue(
                        value: [
                            CreateLocalization("Forsendelsestittel"),
                            CreateLocalization("Transmission title", "en")
                        ]),
                    Summary = CreateContentValue(
                        value: [
                            CreateLocalization("Forsendelse oppsummering"),
                            CreateLocalization("Transmission summary", "en")
                        ]),
                    ContentReference = CreateContentValue(
                        mediaType: "application/vnd.dialogporten.frontchannelembed-url;type=text/markdown",
                        value: [
                            CreateLocalization("https://digdir.no/nb"),
                            CreateLocalization("https://digdir.no/en", "en")
                        ])
                };
                t.Attachments =
                [
                    new CreateDialogTransmissionAttachment
                    {
                        DisplayName =
                        [
                            CreateLocalization("Forsendelse visningsnavn"),
                            CreateLocalization("Transmission attachment display name", "en")
                        ],
                        Urls =
                        [
                            new CreateDialogTransmissionAttachmentUrl
                            {
                                Url = new Uri("https://digdir.apps.tt02.altinn.no/some-other-url"),
                                ConsumerType = AttachmentUrlConsumerType.Gui
                            }
                        ]
                    }
                ];
            })
            .AddTransmission(t =>
            {
                t.AuthorizationAttribute = E2EConstants.UnavailableExternalResource;
                t.Content = new CreateDialogTransmissionContent
                {
                    Title = CreateContentValue(
                        value: [
                            CreateLocalization("Forsendelsesstittel"),
                            CreateLocalization("Transmission title", "en")
                        ]),
                    Summary = CreateContentValue(
                        value: [
                            CreateLocalization("Transmisjon oppsummering"),
                            CreateLocalization("Transmission summary", "en")
                        ])
                };
                t.Attachments =
                [
                    new CreateDialogTransmissionAttachment
                    {
                        DisplayName =
                        [
                            CreateLocalization("Visningsnavn for forsendelsesvedlegg "),
                            CreateLocalization("Transmission attachment display name", "en")
                        ],
                        Urls =
                        [
                            new CreateDialogTransmissionAttachmentUrl
                            {
                                Url = new Uri("https://digdir.apps.tt02.altinn.no/some-other-url"),
                                ConsumerType = AttachmentUrlConsumerType.Gui
                            }
                        ]
                    }
                ];
            })
            .AddTransmission(t =>
            {
                t.AuthorizationAttribute = E2EConstants.UnavailableSubresource;
                t.Content = new CreateDialogTransmissionContent
                {
                    Title = CreateContentValue(
                        value: [
                            CreateLocalization("Forsendelsetittel"),
                            CreateLocalization("Transmission title", "en")
                        ]),
                    Summary = CreateContentValue(
                        value: [
                            CreateLocalization("Forsendelsesoppsummering"),
                            CreateLocalization("Transmission summary", "en")
                        ])
                };
                t.Attachments =
                [
                    new CreateDialogTransmissionAttachment
                    {
                        DisplayName =
                        [
                            CreateLocalization("Visningsnavn for forsendelsesvedlegg"),
                            CreateLocalization("Transmission attachment display name", "en")
                        ],
                        Urls =
                        [
                            new CreateDialogTransmissionAttachmentUrl
                            {
                                Url = new Uri("https://digdir.apps.tt02.altinn.no/some-other-url"),
                                ConsumerType = AttachmentUrlConsumerType.Gui
                            }
                        ]
                    }
                ];
            });

        dialog
            .AddGuiAction(g =>
            {
                g.Action = "read";
                g.Url = new Uri("https://digdir.no");
                g.Priority = DialogGuiActionPriority.Primary;
                g.Title = [CreateLocalization("Gå til dialog")];
            })
            .AddGuiAction(g =>
            {
                g.Action = "read";
                g.Url = new Uri("https://digdir.no");
                g.Priority = DialogGuiActionPriority.Secondary;
                g.HttpMethod = HttpVerb.POST;
                g.Title = [CreateLocalization("Utfør handling uten navigasjon")];
                g.Prompt = [CreateLocalization("Er du sikker?")];
            });

        dialog.AddApiAction();

        dialog
            .AddAttachment(a =>
            {
                a.DisplayName = [CreateLocalization("Et vedlegg")];
                a.Urls = [CreateAttachmentUrl()];
            })
            .AddAttachment(a =>
            {
                a.DisplayName = [CreateLocalization("Et annet vedlegg")];
                a.Urls = [CreateAttachmentUrl()];
            })
            .AddAttachment(a =>
            {
                a.DisplayName = [CreateLocalization("Nok et vedlegg")];
                a.Urls = [CreateAttachmentUrl()];
            });

        dialog
            .AddActivity(a =>
            {
                a.Type = DialogActivityType.DialogCreated;
                a.PerformedBy = new Actor
                {
                    ActorType = ActorType.PartyRepresentative,
                    ActorName = "Some custom name"
                };
            })
            .AddActivity(a =>
            {
                a.Type = DialogActivityType.PaymentMade;
                a.PerformedBy = new Actor
                {
                    ActorType = ActorType.ServiceOwner
                };
            })
            .AddActivity(a =>
            {
                a.Type = DialogActivityType.Information;
                a.PerformedBy = new Actor
                {
                    ActorType = ActorType.PartyRepresentative,
                    ActorId = $"urn:altinn:organization:identifier-no:{E2EConstants.GetDefaultServiceOwnerOrgNr()}"
                };
                a.Description =
                [
                    CreateLocalization("Brukeren har begått skattesvindel"),
                    CreateLocalization("Tax fraud", "en")
                ];
            });

        modify?.Invoke(dialog);
        return dialog;
    }

    private static CreateDialogAttachmentUrl CreateAttachmentUrl() =>
        new()
        {
            Url = new Uri("https://foo.com/foo.pdf"),
            ConsumerType = AttachmentUrlConsumerType.Gui,
            MediaType = "application/pdf"
        };

    public static CreateDialogRequest CreateDialog(
        string serviceResource,
        string party,
        CreateDialogContent content) =>
        new()
        {
            ServiceResource = serviceResource,
            Party = party,
            Content = content
        };

    public static CreateDialogContent CreateContent(
        ContentValue title,
        ContentValue? summary = null,
        ContentValue? senderName = null,
        ContentValue? additionalInfo = null,
        ContentValue? extendedStatus = null,
        ContentValue? mainContentReference = null,
        ContentValue? nonSensitiveTitle = null,
        ContentValue? nonSensitiveSummary = null
    )
    {
        var content = new CreateDialogContent
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

        if (nonSensitiveTitle is not null)
            content.NonSensitiveTitle = nonSensitiveTitle;

        if (nonSensitiveSummary is not null)
            content.NonSensitiveSummary = nonSensitiveSummary;

        return content;
    }

    public static ContentValue CreateContentValue(
        string value,
        string languageCode,
        string? mediaType = null) =>
        CreateContentValue(
            mediaType: mediaType,
            value: [CreateLocalization(value, languageCode)]);

    public static ContentValue CreateContentValue(
        List<Localization> value,
        string? mediaType = null)
    {
        var contentValue = new ContentValue
        {
            Value = value
        };

        if (mediaType is not null)
        {
            contentValue.MediaType = mediaType;
        }

        return contentValue;
    }

    public static Localization CreateLocalization(
        string value,
        string languageCode = "nb") =>
        new()
        {
            Value = value,
            LanguageCode = languageCode
        };

    public static CreateTransmissionRequest CreateSimpleTransmission(
        Action<CreateTransmissionRequest>? modify = null)
    {
        var transmission = new CreateTransmissionRequest
        {
            Type = DialogTransmissionType.Information,
            Sender = new Actor
            {
                ActorType = ActorType.ServiceOwner
            },
            Content = new CreateTransmissionContent
            {
                Title = CreateContentValue(
                    value: "Melding med vedlegg",
                    languageCode: "nb")
            }
        };

        modify?.Invoke(transmission);
        return transmission;
    }

    public static CreateActivityRequest CreateSimpleActivity(
        Action<CreateActivityRequest>? modify = null)
    {
        var activity = new CreateActivityRequest
        {
            Type = DialogActivityType.DialogCreated,
            ExtendedType = new Uri("http://localhost"),
            PerformedBy = new Actor
            {
                ActorType = ActorType.PartyRepresentative,
                ActorId = "urn:altinn:person:legacy-selfidentified:Leif"
            },
            Description = []
        };

        modify?.Invoke(activity);
        return activity;
    }
}
