using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Extensions;

internal static class CreateDialogTransmissionExtensions
{
    public static TransmissionDto WithPartyRepresentativeActor(this TransmissionDto transmission)
    {
        transmission.Sender = new ActorDto
        {
            ActorType = ActorType.Values.PartyRepresentative,
            ActorName = "Fredrik",
        };

        return transmission;
    }

    public static TransmissionDto WithServiceOwnerActor(this TransmissionDto transmission)
    {
        transmission.Sender = new ActorDto
        {
            ActorType = ActorType.Values.ServiceOwner,
        };

        return transmission;
    }

    public static TransmissionDto AddAttachment(this TransmissionDto transmission, Action<TransmissionAttachmentDto>? modify = null)
    {
        var attachment = new TransmissionAttachmentDto
        {
            Id = NewUuidV7(),
            DisplayName = DialogGenerator.GenerateFakeLocalizations(1),
            Urls =
            [
                new()
                {
                    ConsumerType = AttachmentUrlConsumerType.Values.Gui,
                    Url = new Uri("https://example.com/file.pdf")
                }
            ]
        };

        modify?.Invoke(attachment);
        transmission.Attachments.Add(attachment);
        return transmission;
    }

    public static TransmissionDto AddNavigationalAction(this TransmissionDto transmission, Action<TransmissionNavigationalActionDto>? modify = null)
    {
        var action = new TransmissionNavigationalActionDto
        {
            Title = DialogGenerator.GenerateFakeLocalizations(1),
            Url = new Uri("https://example.com/action")
        };

        modify?.Invoke(action);
        transmission.NavigationalActions.Add(action);
        return transmission;
    }
}
