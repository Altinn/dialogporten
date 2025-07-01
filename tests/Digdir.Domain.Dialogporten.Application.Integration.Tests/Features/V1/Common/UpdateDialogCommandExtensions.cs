using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common;

public static class UpdateDialogCommandExtensions
{
    public static UpdateDialogCommand AddTransmission(this UpdateDialogCommand command, Action<TransmissionDto>? modify = null)
    {
        var transmission = new TransmissionDto
        {
            Type = DialogTransmissionType.Values.Information,
            Content = new()
            {
                Title = new() { Value = DialogGenerator.GenerateFakeLocalizations(3) },
                Summary = new() { Value = DialogGenerator.GenerateFakeLocalizations(3) }
            },
            Sender = new() { ActorType = ActorType.Values.ServiceOwner }
        };

        modify?.Invoke(transmission);
        command.Dto.Transmissions.Add(transmission);
        return command;
    }

    public static TransmissionDto AddAttachment(this TransmissionDto transmission, Action<TransmissionAttachmentDto>? modify = null)
    {
        var attachment = new TransmissionAttachmentDto
        {
            Id = NewUuidV7(),
            DisplayName = DialogGenerator.GenerateFakeLocalizations(1),
            Urls = [new() { ConsumerType = AttachmentUrlConsumerType.Values.Gui,
                Url = new Uri("https://example.com/file.pdf") }]
        };

        modify?.Invoke(attachment);
        transmission.Attachments.Add(attachment);
        return transmission;
    }
}
