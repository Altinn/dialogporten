using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Parties;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common;

public static class SnapshotDialog
{
    public const string ServiceResource = "urn:altinn:resource:1337";

    public static CreateDialogDto Create() => new()
    {
        Party = NorwegianPersonIdentifier.PrefixWithSeparator + "06875895484",
        Progress = 22,
        Process = "some-process",
        PrecedingProcess = "some-preceding-process",
        ServiceResource = ServiceResource,
        ExtendedStatus = "ext-status",
        ExternalReference = "external-ref",
        SearchTags =
        [
            new()
            {
                Value = "tag1"
            },
            new()
            {
                Value = "tag2"
            }
        ],
        Attachments =
        [
            new()
            {
                DisplayName =
                [
                    new()
                    {
                        LanguageCode = "nb",
                        Value = "Vedlegg 1"
                    }
                ],
                Urls =
                [
                    new()
                    {
                        ConsumerType = AttachmentUrlConsumerType.Values.Gui,
                        Url = new Uri("https://example.com/attachment1")
                    }
                ],
            }
        ],
        GuiActions =
        [
            new()
            {
                Action = "action",
                Priority = DialogGuiActionPriority.Values.Primary,
                Url = new Uri("https://example.com/action"),
                Title = [new(){LanguageCode = "nb", Value = "Utfør handling"}]
            },
            new()
            {
                Action = "action",
                Priority = DialogGuiActionPriority.Values.Secondary,
                Url = new Uri("https://example.com/action"),
                Title = [new(){LanguageCode = "nb", Value = "Utfør handling"}]
            }
        ],
        Activities =
        [
            new()
            {
                CreatedAt = DateTimeOffset.Now,
                Type = DialogActivityType.Values.DialogCreated,
                PerformedBy = new()
                {
                    ActorType = ActorType.Values.ServiceOwner
                }
            }
        ],
        Transmissions =
        [
            new()
            {
                Type = DialogTransmissionType.Values.Information,
                Sender = new()
                {
                    ActorType = ActorType.Values.ServiceOwner
                },
                Content = new()
                {
                    Title = new()
                    {
                        Value =
                        [
                            new()
                            {
                                LanguageCode = "nb", Value = "Dette er en tittel, transmission"
                            }
                        ]
                    },
                    Summary = new()
                    {
                        Value =
                        [
                            new()
                            {
                                LanguageCode = "nb", Value = "Dette er et sammendrag, transmission"
                            }
                        ]
                    }
                }
            }
        ],
        Content = new()
        {
            Title = new()
            {
                Value =
                [
                    new()
                    {
                        LanguageCode = "nb", Value = "Dette er en tittel, dialog"
                    }
                ]
            },
            Summary = new()
            {
                Value =
                [
                    new()
                    {
                        LanguageCode = "nb", Value = "Dette er et sammendrag, dialog"
                    }
                ]
            }
        }
    };
}
