using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.UpdateTransmission;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Extensions;

internal static class UpdateTransmissionCommandExtensions
{
    public static UpdateTransmissionCommand AddAttachment(this UpdateTransmissionCommand command, Action<TransmissionAttachmentDto>? modify = null)
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
        command.Dto.Attachments.Add(attachment);
        return command;
    }

    public static UpdateTransmissionCommand AddNavigationalAction(this UpdateTransmissionCommand command, Action<TransmissionNavigationalActionDto>? modify = null)
    {
        var action = new TransmissionNavigationalActionDto
        {
            Title = DialogGenerator.GenerateFakeLocalizations(1),
            Url = new Uri("https://example.com/action")
        };

        modify?.Invoke(action);
        command.Dto.NavigationalActions.Add(action);
        return command;
    }
}
